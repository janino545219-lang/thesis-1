using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Areas.CustomerPortal.Models.ViewModels;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Areas.CustomerPortal.Controllers;

public class ShopController(
    IShopService shopService,
    ICustomerCartService cartService,
    ICustomerOrderService customerOrderService,
    ApplicationDbContext context,
    IAuditLogService auditLogService,
    UserManager<ApplicationUser> userManager) : CustomerPortalControllerBase
{
    public async Task<IActionResult> Index(int? categoryId, string? searchTerm, decimal? minPrice, decimal? maxPrice, List<string>? brands)
    {
        var customer = await GetCallingCustomerAsync();
        if (customer is null)
        {
            return NotFound();
        }

        var branchId = await ResolveBranchIdAsync(customer);
        if (branchId is null)
        {
            return Problem("No branch is available to shop from.");
        }

        var model = await shopService.BrowseAsync(branchId.Value, categoryId, searchTerm, minPrice, maxPrice, brands);
        model.CartItemCount = cartService.GetCart().Sum(i => i.Quantity);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddToCart(int productId, string productName, decimal unitPrice, int quantity = 1)
    {
        if (quantity > 0)
        {
            cartService.AddItem(productId, productName, unitPrice, quantity);
        }

        TempData["Success"] = $"Added {productName} to your cart.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Cart()
    {
        return View(cartService.GetCart());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateCartQuantity(int productId, int quantity)
    {
        cartService.UpdateQuantity(productId, quantity);
        return RedirectToAction(nameof(Cart));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveFromCart(int productId)
    {
        cartService.RemoveItem(productId);
        return RedirectToAction(nameof(Cart));
    }

    public async Task<IActionResult> Checkout()
    {
        var cart = cartService.GetCart();
        if (cart.Count == 0)
        {
            TempData["Error"] = "Your cart is empty.";
            return RedirectToAction(nameof(Cart));
        }

        var customer = await GetCallingCustomerAsync();
        if (customer is null)
        {
            return NotFound();
        }

        ViewBag.BranchName = await ResolveBranchNameAsync(customer);
        return View(cart);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(string? notes)
    {
        var customer = await GetCallingCustomerAsync();
        if (customer is null)
        {
            return NotFound();
        }

        var branchId = await ResolveBranchIdAsync(customer);
        if (branchId is null)
        {
            return Problem("No branch is available to shop from.");
        }

        var cart = cartService.GetCart();
        var (succeeded, error, orderId) = await customerOrderService.SubmitOrderAsync(customer.Id, branchId.Value, cart, notes);

        if (!succeeded)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Cart));
        }

        cartService.ClearCart();
        await auditLogService.LogAsync("CustomerOrderSubmitted", userManager.GetUserId(User), $"OrderId: {orderId}");

        TempData["Success"] = "Order submitted! You'll be notified when it's ready for pickup.";
        return RedirectToAction(nameof(OrderDetails), new { id = orderId });
    }

    public async Task<IActionResult> Orders()
    {
        var customer = await GetCallingCustomerAsync();
        if (customer is null)
        {
            return NotFound();
        }

        return View(await customerOrderService.GetByCustomerAsync(customer.Id));
    }

    public async Task<IActionResult> OrderDetails(int id)
    {
        var order = await customerOrderService.GetByIdAsync(id);
        var userId = userManager.GetUserId(User)!;

        if (order is null || order.Customer.UserId != userId)
        {
            return NotFound();
        }

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var order = await customerOrderService.GetByIdAsync(id);
        var userId = userManager.GetUserId(User)!;

        if (order is null || order.Customer.UserId != userId)
        {
            return NotFound();
        }

        var (succeeded, error) = await customerOrderService.CancelAsync(id);
        if (succeeded)
        {
            await auditLogService.LogAsync("CustomerOrderCancelled", userId, $"OrderId: {id}");
            TempData["Success"] = "Order cancelled.";
        }
        else
        {
            TempData["Error"] = error;
        }

        return RedirectToAction(nameof(OrderDetails), new { id });
    }

    private async Task<Customer?> GetCallingCustomerAsync()
    {
        var userId = userManager.GetUserId(User);
        return await context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
    }

    private async Task<int?> ResolveBranchIdAsync(Customer customer)
    {
        if (customer.PreferredBranchId.HasValue)
        {
            return customer.PreferredBranchId;
        }

        return await context.Branches.Where(b => b.IsActive).Select(b => (int?)b.Id).FirstOrDefaultAsync();
    }

    private async Task<string> ResolveBranchNameAsync(Customer customer)
    {
        var branchId = await ResolveBranchIdAsync(customer);
        return await context.Branches.Where(b => b.Id == branchId).Select(b => b.Name).FirstOrDefaultAsync() ?? "your branch";
    }
}
