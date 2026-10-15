using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FarmaciaSmart.Web.Helpers;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Controllers;

[Authorize(Roles = $"{ApplicationRoles.Administrator},{ApplicationRoles.BranchManager}")]
public class CustomerController(
    ICustomerService customerService,
    IAuditLogService auditLogService,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        return View(await customerService.GetAllAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var customer = await customerService.GetByIdAsync(id);
        return customer is null ? NotFound() : View(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, CustomerStatus status)
    {
        var (succeeded, error) = await customerService.SetStatusAsync(id, status);
        if (succeeded)
        {
            await auditLogService.LogAsync("CustomerStatusChanged", userManager.GetUserId(User), $"CustomerId: {id}, Status: {status}");
            TempData["Success"] = $"Customer status set to {status}.";
        }
        else
        {
            TempData["Error"] = error;
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
