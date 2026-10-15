using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Helpers;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Sales;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Controllers;

[Authorize(Roles = $"{ApplicationRoles.AdminOrSuper},{ApplicationRoles.BranchManager},{ApplicationRoles.Cashier},{ApplicationRoles.Pharmacist}")]
public class POSController(
    ICartService cartService,
    IPosService posService,
    ApplicationDbContext context,
    IAuditLogService auditLogService,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index(string? search)
    {
        var branchId = GetOperatingBranchId();
        if (branchId is null)
        {
            return RedirectToAction(nameof(SelectBranch));
        }

        var products = await context.Products
            .Where(p => p.IsActive)
            .Where(p => string.IsNullOrEmpty(search) || p.Name.Contains(search) || (p.Barcode != null && p.Barcode.Contains(search)))
            .OrderBy(p => p.Name)
            .Take(50)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.BranchName = (await context.Branches.FindAsync(branchId.Value))?.Name;
        ViewBag.Cart = cartService.GetCart();

        return View(products);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
    {
        if (GetOperatingBranchId() is null)
        {
            return RedirectToAction(nameof(SelectBranch));
        }

        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);
        if (product is null)
        {
            TempData["Error"] = "Product not found.";
            return RedirectToAction(nameof(Index));
        }

        cartService.AddItem(product.Id, product.Name, product.Price, quantity < 1 ? 1 : quantity);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddByBarcode(string barcode, int quantity = 1)
    {
        if (GetOperatingBranchId() is null)
        {
            return RedirectToAction(nameof(SelectBranch));
        }

        var product = await context.Products.FirstOrDefaultAsync(p => p.Barcode == barcode && p.IsActive);
        if (product is null)
        {
            TempData["Error"] = $"No active product found with barcode {barcode}.";
            return RedirectToAction(nameof(Index));
        }

        cartService.AddItem(product.Id, product.Name, product.Price, quantity < 1 ? 1 : quantity);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveFromCart(int productId)
    {
        cartService.RemoveItem(productId);
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Checkout()
    {
        var cart = cartService.GetCart();
        if (cart.Count == 0)
        {
            TempData["Error"] = "Cart is empty.";
            return RedirectToAction(nameof(Index));
        }

        return View(new CheckoutViewModel { CartItems = cart });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel model)
    {
        var branchId = GetOperatingBranchId();
        if (branchId is null)
        {
            return RedirectToAction(nameof(SelectBranch));
        }

        model.CartItems = cartService.GetCart();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = userManager.GetUserId(User)!;
        var (succeeded, error, saleId) = await posService.CheckoutAsync(model.CartItems, model, branchId.Value, userId);

        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error!);
            return View(model);
        }

        cartService.ClearCart();

        await auditLogService.LogAsync("SaleCompleted", userId, $"SaleId: {saleId}, Total: {model.TotalAmount:F2}");

        return RedirectToAction(nameof(Receipt), new { id = saleId });
    }

    public async Task<IActionResult> Receipt(int id)
    {
        var sale = await posService.GetSaleAsync(id);
        return sale is null ? NotFound() : View(sale);
    }

    public async Task<IActionResult> History()
    {
        var branchId = User.IsInRole(ApplicationRoles.Administrator) ? null : GetOperatingBranchId();
        return View(await posService.GetSalesHistoryAsync(branchId));
    }

    public async Task<IActionResult> SelectBranch()
    {
        ViewBag.Branches = await context.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectBranch(int branchId)
    {
        if (!await context.Branches.AnyAsync(b => b.Id == branchId && b.IsActive))
        {
            ModelState.AddModelError(string.Empty, "Selected branch is not valid.");
            ViewBag.Branches = await context.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
            return View();
        }

        cartService.SetOperatingBranchId(branchId);
        return RedirectToAction(nameof(Index));
    }

    private int? GetOperatingBranchId()
    {
        var claim = User.FindFirst(ApplicationUserClaimsPrincipalFactory.BranchIdClaimType)?.Value;
        if (claim is not null && int.TryParse(claim, out var claimBranchId))
        {
            return claimBranchId;
        }

        return cartService.GetOperatingBranchId();
    }
}
