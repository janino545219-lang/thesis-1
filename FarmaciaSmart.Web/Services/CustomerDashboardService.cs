using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Areas.CustomerPortal.Models.ViewModels;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

public class CustomerDashboardService(ApplicationDbContext context) : ICustomerDashboardService
{
    public async Task<CustomerDashboardViewModel?> GetDashboardAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var customer = await context.Customers
            .Include(c => c.User)
            .Include(c => c.PreferredBranch)
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);

        if (customer is null)
        {
            return null;
        }

        var completedSales = await context.Sales
            .Include(s => s.Branch)
            .Include(s => s.Items)
            .Where(s => s.CustomerId == customerId && s.Status == SaleStatus.Completed)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync(cancellationToken);

        var orders = await context.CustomerOrders
            .Where(o => o.CustomerId == customerId)
            .ToListAsync(cancellationToken);

        var recommendations = await context.Recommendations
            .Include(r => r.Category)
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return new CustomerDashboardViewModel
        {
            Customer = customer,
            TotalSpent = completedSales.Sum(s => s.TotalAmount),
            TotalOrders = orders.Count,
            PendingOrders = orders.Count(o => o.Status == CustomerOrderStatus.Pending),
            RecommendationsReceived = recommendations.Count,
            SpendingTrend = BuildSpendingTrend(completedSales),
            PendingOrderCount = orders.Count(o => o.Status == CustomerOrderStatus.Pending),
            FulfilledOrderCount = orders.Count(o => o.Status == CustomerOrderStatus.Fulfilled),
            CancelledOrderCount = orders.Count(o => o.Status == CustomerOrderStatus.Cancelled),
            RecentPurchases = [.. completedSales.Take(5)],
            RecentRecommendations = [.. recommendations.Take(5)]
        };
    }

    private static List<MonthlySpendPoint> BuildSpendingTrend(List<Sale> completedSales)
    {
        var now = DateTime.UtcNow;
        var trend = new List<MonthlySpendPoint>();

        for (var i = 5; i >= 0; i--)
        {
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);
            var total = completedSales.Where(s => s.SaleDate >= monthStart && s.SaleDate < monthEnd).Sum(s => s.TotalAmount);
            trend.Add(new MonthlySpendPoint { MonthLabel = monthStart.ToString("MMM"), TotalAmount = total });
        }

        return trend;
    }
}
