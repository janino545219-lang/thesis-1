using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FarmaciaSmart.Web.Helpers;
using FarmaciaSmart.Web.Models.ViewModels.Analytics;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Administrator)]
public class AnalyticsController(IAnalyticsService analyticsService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var model = new AnalyticsDashboardViewModel
        {
            Summary = await analyticsService.GetSummaryAsync(),
            SalesTrend = await analyticsService.GetSalesTrendAsync(30),
            TopProducts = await analyticsService.GetTopProductsAsync(5),
            SalesByBranch = await analyticsService.GetSalesByBranchAsync(),
            LowStockByBranch = await analyticsService.GetLowStockByBranchAsync(),
            BusyPeriods = await analyticsService.GetBusyPeriodsAsync()
        };

        return View(model);
    }
}
