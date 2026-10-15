using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Helpers;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Controllers;

[Authorize(Roles = $"{ApplicationRoles.AdminOrSuper},{ApplicationRoles.BranchManager},{ApplicationRoles.InventoryStaff},{ApplicationRoles.Pharmacist}")]
public class ReportController(IReportService reportService, ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> SalesSummary(DateOnly? from, DateOnly? to, int? branchId)
    {
        ViewBag.Branches = await context.Branches.OrderBy(b => b.Name).ToListAsync();
        ViewBag.From = from;
        ViewBag.To = to;
        ViewBag.BranchId = branchId;

        var sales = await reportService.GetSalesSummaryAsync(from, to, branchId);
        ViewBag.TotalSales = sales.Sum(s => s.TotalAmount);
        return View(sales);
    }

    public async Task<IActionResult> LowStock(int? branchId)
    {
        ViewBag.Branches = await context.Branches.OrderBy(b => b.Name).ToListAsync();
        ViewBag.BranchId = branchId;
        return View(await reportService.GetLowStockReportAsync(branchId));
    }

    public async Task<IActionResult> Expiration(int? daysAhead, int? branchId)
    {
        var days = daysAhead ?? 30;
        ViewBag.Branches = await context.Branches.OrderBy(b => b.Name).ToListAsync();
        ViewBag.BranchId = branchId;
        ViewBag.DaysAhead = days;
        return View(await reportService.GetExpirationReportAsync(days, branchId));
    }

    public async Task<IActionResult> Purchases(DateOnly? from, DateOnly? to, int? branchId, int? supplierId)
    {
        ViewBag.Branches = await context.Branches.OrderBy(b => b.Name).ToListAsync();
        ViewBag.Suppliers = await context.Suppliers.OrderBy(s => s.Name).ToListAsync();
        ViewBag.From = from;
        ViewBag.To = to;
        ViewBag.BranchId = branchId;
        ViewBag.SupplierId = supplierId;

        var orders = await reportService.GetPurchasesReportAsync(from, to, branchId, supplierId);
        ViewBag.TotalAmount = orders.Sum(o => o.TotalAmount);
        return View(orders);
    }
}
