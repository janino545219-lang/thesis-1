using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Helpers;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Purchasing;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Controllers;

[Authorize(Roles = $"{ApplicationRoles.Administrator},{ApplicationRoles.InventoryStaff}")]
public class PurchaseController(
    IPurchasingService purchasingService,
    ApplicationDbContext context,
    IAuditLogService auditLogService,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        return View(await purchasingService.GetAllAsync());
    }

    public async Task<IActionResult> Create()
    {
        return View(await BuildCreateViewModelAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePurchaseOrderViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(await BuildCreateViewModelAsync(model));
        }

        var userId = userManager.GetUserId(User)!;
        var (succeeded, error, purchaseOrderId) = await purchasingService.CreateDraftAsync(model, userId);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error!);
            return View(await BuildCreateViewModelAsync(model));
        }

        await auditLogService.LogAsync(
            "PurchaseOrderCreated",
            userId,
            $"SupplierId: {model.SupplierId}, BranchId: {model.BranchId}");

        return RedirectToAction(nameof(Details), new { id = purchaseOrderId });
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await purchasingService.GetByIdAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        ViewBag.Products = await context.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(int purchaseOrderId, PurchaseOrderItemViewModel model)
    {
        var (succeeded, error) = await purchasingService.AddItemAsync(purchaseOrderId, model);
        if (succeeded)
        {
            await auditLogService.LogAsync(
                "PurchaseOrderItemAdded",
                userManager.GetUserId(User),
                $"PurchaseOrderId: {purchaseOrderId}, ProductId: {model.ProductId}, Qty: {model.QuantityOrdered}");
        }
        else
        {
            TempData["Error"] = error;
        }

        return RedirectToAction(nameof(Details), new { id = purchaseOrderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveItem(int purchaseOrderItemId, int purchaseOrderId)
    {
        var (succeeded, error) = await purchasingService.RemoveItemAsync(purchaseOrderItemId);
        if (!succeeded)
        {
            TempData["Error"] = error;
        }

        return RedirectToAction(nameof(Details), new { id = purchaseOrderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(int id)
    {
        var userId = userManager.GetUserId(User)!;
        var (succeeded, error) = await purchasingService.PlaceOrderAsync(id);
        if (succeeded)
        {
            await auditLogService.LogAsync("PurchaseOrderPlaced", userId, $"PurchaseOrderId: {id}");
            TempData["Success"] = "Purchase order placed.";
        }
        else
        {
            TempData["Error"] = error;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Receive(int purchaseOrderId, ReceivePurchaseOrderItemViewModel model)
    {
        var userId = userManager.GetUserId(User)!;
        var (succeeded, error) = await purchasingService.ReceiveItemAsync(model, userId);
        if (succeeded)
        {
            await auditLogService.LogAsync(
                "PurchaseOrderItemReceived",
                userId,
                $"PurchaseOrderItemId: {model.PurchaseOrderItemId}, Qty: {model.Quantity}, Batch: {model.BatchNumber}");
            TempData["Success"] = "Stock received against purchase order.";
        }
        else
        {
            TempData["Error"] = error;
        }

        return RedirectToAction(nameof(Details), new { id = purchaseOrderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = userManager.GetUserId(User)!;
        var (succeeded, error) = await purchasingService.CancelAsync(id);
        if (succeeded)
        {
            await auditLogService.LogAsync("PurchaseOrderCancelled", userId, $"PurchaseOrderId: {id}");
            TempData["Success"] = "Purchase order cancelled.";
        }
        else
        {
            TempData["Error"] = error;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<CreatePurchaseOrderViewModel> BuildCreateViewModelAsync(CreatePurchaseOrderViewModel? model = null)
    {
        model ??= new CreatePurchaseOrderViewModel();
        model.Suppliers = await context.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
        model.Branches = await context.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
        return model;
    }
}
