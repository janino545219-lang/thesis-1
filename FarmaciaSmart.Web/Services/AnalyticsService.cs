using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Analytics;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

public class AnalyticsService(ApplicationDbContext context) : IAnalyticsService
{
    public async Task<List<DailySalesPoint>> GetSalesTrendAsync(int days, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.Date.AddDays(-(days - 1));

        var raw = await context.Sales
            .Where(s => s.Status == SaleStatus.Completed && s.SaleDate >= since)
            .GroupBy(s => s.SaleDate.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(s => s.TotalAmount) })
            .ToListAsync(cancellationToken);

        var lookup = raw.ToDictionary(x => DateOnly.FromDateTime(x.Date), x => x.Total);
        var sinceDate = DateOnly.FromDateTime(since);

        var result = new List<DailySalesPoint>();
        for (var i = 0; i < days; i++)
        {
            var date = sinceDate.AddDays(i);
            result.Add(new DailySalesPoint
            {
                Date = date,
                TotalAmount = lookup.TryGetValue(date, out var amt) ? amt : 0
            });
        }

        return result;
    }

    public async Task<List<TopProductPoint>> GetTopProductsAsync(int take, CancellationToken cancellationToken = default) =>
        await context.SaleItems
            .Where(si => si.Sale.Status == SaleStatus.Completed)
            .GroupBy(si => si.Product.Name)
            .Select(g => new TopProductPoint { ProductName = g.Key, QuantitySold = g.Sum(si => si.Quantity) })
            .OrderByDescending(p => p.QuantitySold)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<List<BranchSalesPoint>> GetSalesByBranchAsync(CancellationToken cancellationToken = default) =>
        await context.Sales
            .Where(s => s.Status == SaleStatus.Completed)
            .GroupBy(s => s.Branch.Name)
            .Select(g => new BranchSalesPoint { BranchName = g.Key, TotalAmount = g.Sum(s => s.TotalAmount) })
            .ToListAsync(cancellationToken);

    public async Task<List<BranchLowStockPoint>> GetLowStockByBranchAsync(CancellationToken cancellationToken = default) =>
        await context.Inventories
            .Where(i => i.QuantityOnHand <= i.Product.LowStockThreshold)
            .GroupBy(i => i.Branch.Name)
            .Select(g => new BranchLowStockPoint { BranchName = g.Key, LowStockCount = g.Count() })
            .ToListAsync(cancellationToken);

    public async Task<AnalyticsSummaryViewModel> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonthStart = thisMonthStart.AddMonths(-1);

        var thisMonthSales = await context.Sales
            .Where(s => s.Status == SaleStatus.Completed && s.SaleDate >= thisMonthStart)
            .Select(s => s.TotalAmount)
            .ToListAsync(cancellationToken);

        var lastMonthSales = await context.Sales
            .Where(s => s.Status == SaleStatus.Completed && s.SaleDate >= lastMonthStart && s.SaleDate < thisMonthStart)
            .Select(s => s.TotalAmount)
            .ToListAsync(cancellationToken);

        var thisMonthTotal = thisMonthSales.Sum();
        var lastMonthTotal = lastMonthSales.Sum();
        var thisMonthCount = thisMonthSales.Count;
        var lastMonthCount = lastMonthSales.Count;
        var thisMonthAvg = thisMonthCount == 0 ? 0 : thisMonthTotal / thisMonthCount;
        var lastMonthAvg = lastMonthCount == 0 ? 0 : lastMonthTotal / lastMonthCount;

        var lowStockCount = await context.Inventories.CountAsync(i => i.QuantityOnHand <= i.Product.LowStockThreshold, cancellationToken);

        var newCustomersThisMonth = await context.Customers.CountAsync(c => c.CreatedAt >= thisMonthStart, cancellationToken);
        var newCustomersLastMonth = await context.Customers.CountAsync(
            c => c.CreatedAt >= lastMonthStart && c.CreatedAt < thisMonthStart, cancellationToken);

        return new AnalyticsSummaryViewModel
        {
            TotalSalesThisMonth = thisMonthTotal,
            SalesPercentChange = PercentChange(thisMonthTotal, lastMonthTotal),
            TransactionsThisMonth = thisMonthCount,
            TransactionsPercentChange = PercentChange(thisMonthCount, lastMonthCount),
            AvgOrderValueThisMonth = thisMonthAvg,
            AvgOrderValuePercentChange = PercentChange(thisMonthAvg, lastMonthAvg),
            LowStockItemCount = lowStockCount,
            NewCustomersThisMonth = newCustomersThisMonth,
            NewCustomersPercentChange = PercentChange(newCustomersThisMonth, newCustomersLastMonth)
        };
    }

    public async Task<List<BusyPeriodPoint>> GetBusyPeriodsAsync(int lookbackDays = 90, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-lookbackDays);

        var saleDates = await context.Sales
            .Where(s => s.Status == SaleStatus.Completed && s.SaleDate >= since)
            .Select(s => s.SaleDate)
            .ToListAsync(cancellationToken);

        return saleDates
            .GroupBy(d => (d.DayOfWeek, d.Hour))
            .Select(g => new BusyPeriodPoint { DayOfWeek = g.Key.DayOfWeek, Hour = g.Key.Hour, SaleCount = g.Count() })
            .ToList();
    }

    /// <summary>Null when there's no prior-period baseline to compare against (avoids a divide-by-zero "infinite" swing).</summary>
    private static decimal? PercentChange(decimal current, decimal previous) =>
        previous == 0 ? null : Math.Round((current - previous) / previous * 100, 1);
}
