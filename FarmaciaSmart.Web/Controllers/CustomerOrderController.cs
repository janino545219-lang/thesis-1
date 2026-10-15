using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FarmaciaSmart.Web.Helpers;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Controllers;

/// <summary>
/// Staff-side queue for customer pre-orders (Shop feature). Fulfilling one is bookkeeping only --
/// it marks the order as picked up. The actual sale/payment/inventory deduction happens through
/// the existing POS screen exactly as it would for any other walk-in customer.
/// </summary>
[Authorize(Roles = $"{ApplicationRoles.AdminOrSuper},{ApplicationRoles.BranchManager},{ApplicationRoles.Cashier},{ApplicationRoles.InventoryStaff},{ApplicationRoles.Pharmacist}")]
public class CustomerOrderController(
    ICustomerOrderService customerOrderService,
    IAuditLogService auditLogService,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        return View(await customerOrderService.GetPendingAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await customerOrderService.GetByIdAsync(id);
        return order is null ? NotFound() : View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Fulfill(int id)
    {
        var userId = userManager.GetUserId(User)!;
        var (succeeded, error) = await customerOrderService.FulfillAsync(id, userId);
        if (succeeded)
        {
            await auditLogService.LogAsync("CustomerOrderFulfilled", userId, $"OrderId: {id}");
            TempData["Success"] = "Order marked as fulfilled.";
        }
        else
        {
            TempData["Error"] = error;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = userManager.GetUserId(User)!;
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

        return RedirectToAction(nameof(Index));
    }
}
