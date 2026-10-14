using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FarmaciaSmart.Web.Helpers;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Controllers;

[Authorize(Roles = $"{ApplicationRoles.Administrator},{ApplicationRoles.InventoryStaff},{ApplicationRoles.HrPayroll},{ApplicationRoles.Pharmacist}")]
public class NotificationController(
    INotificationService notificationService,
    IAuditLogService auditLogService,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index(int page = 1)
    {
        return View(await notificationService.GetRecentAsync(page));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        await notificationService.MarkAsReadAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateNow()
    {
        var count = await notificationService.GenerateSystemNotificationsAsync();
        TempData["Success"] = $"Generated {count} new notification(s).";
        await auditLogService.LogAsync("NotificationsGenerated", userManager.GetUserId(User), $"Count: {count}");
        return RedirectToAction(nameof(Index));
    }
}
