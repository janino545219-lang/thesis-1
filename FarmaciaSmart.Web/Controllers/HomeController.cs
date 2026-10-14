using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using FarmaciaSmart.Web.Helpers;
using FarmaciaSmart.Web.Models.ViewModels;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Controllers;

public class HomeController(IStaffHomeService staffHomeService) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (!(User.Identity?.IsAuthenticated ?? false))
        {
            return View("Landing");
        }

        if (User.IsInRole(ApplicationRoles.Customer))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "CustomerPortal" });
        }

        var branchIdClaim = User.FindFirstValue(ApplicationUserClaimsPrincipalFactory.BranchIdClaimType);
        int? branchId = int.TryParse(branchIdClaim, out var parsedBranchId) ? parsedBranchId : null;

        var summary = await staffHomeService.GetSummaryAsync(
            isAdministrator: User.IsInRole(ApplicationRoles.Administrator),
            isCashier: User.IsInRole(ApplicationRoles.Cashier),
            isInventoryStaff: User.IsInRole(ApplicationRoles.InventoryStaff),
            isPharmacist: User.IsInRole(ApplicationRoles.Pharmacist),
            isHrPayroll: User.IsInRole(ApplicationRoles.HrPayroll),
            branchId: branchId);

        summary.DisplayName = User.FindFirstValue(ClaimTypes.GivenName) ?? User.Identity?.Name ?? "there";

        return View(summary);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult AccessDenied()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
