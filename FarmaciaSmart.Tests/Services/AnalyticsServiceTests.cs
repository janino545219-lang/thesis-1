using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Services;

namespace FarmaciaSmart.Tests.Services;

public class AnalyticsServiceTests
{
    private static (Branch branch, Product product) Seed(ApplicationDbContext context)
    {
        var branch = new Branch { Name = "Main Branch", Code = "MAIN", Address = "123 St", IsActive = true };
        var category = new Category { Name = "Pain Relief", IsActive = true };
        context.Branches.Add(branch);
        context.Categories.Add(category);
        context.SaveChanges();

        var product = new Product { Name = "Biogesic 500mg", CategoryId = category.Id, Price = 85.50m, IsActive = true, LowStockThreshold = 20 };
        context.Products.Add(product);

        var cashier = new ApplicationUser { Id = "cashier-1", UserName = "cashier@example.com", Email = "cashier@example.com", FirstName = "C", LastName = "R", IsActive = true };
        context.Users.Add(cashier);
        context.SaveChanges();

        return (branch, product);
    }

    private static Sale AddSale(ApplicationDbContext context, Branch branch, decimal total, DateTime saleDate)
    {
        var sale = new Sale
        {
            ReceiptNumber = $"R-{Guid.NewGuid():N}"[..10],
            BranchId = branch.Id,
            CashierUserId = "cashier-1",
            SubTotal = total,
            TotalAmount = total,
            Status = SaleStatus.Completed,
            SaleDate = saleDate
        };
        context.Sales.Add(sale);
        context.SaveChanges();
        return sale;
    }

    [Fact]
    public async Task GetSummaryAsync_ComputesThisMonthTotalsAndPercentChangeVsLastMonth()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, _) = Seed(context);
        var now = DateTime.UtcNow;
        var thisMonthStart = new DateTime(now.Year, now.Month, 1, 12, 0, 0, DateTimeKind.Utc);
        var lastMonthStart = thisMonthStart.AddMonths(-1);

        AddSale(context, branch, 100m, thisMonthStart);
        AddSale(context, branch, 100m, thisMonthStart);
        AddSale(context, branch, 100m, lastMonthStart);

        var service = new AnalyticsService(context);
        var summary = await service.GetSummaryAsync();

        Assert.Equal(200m, summary.TotalSalesThisMonth);
        Assert.Equal(2, summary.TransactionsThisMonth);
        Assert.Equal(100m, summary.AvgOrderValueThisMonth);
        Assert.Equal(100.0m, summary.SalesPercentChange);
    }

    [Fact]
    public async Task GetSummaryAsync_NoSalesLastMonth_PercentChangeIsNull()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, _) = Seed(context);
        AddSale(context, branch, 50m, DateTime.UtcNow);

        var service = new AnalyticsService(context);
        var summary = await service.GetSummaryAsync();

        Assert.Null(summary.SalesPercentChange);
    }

    [Fact]
    public async Task GetSummaryAsync_CountsLowStockItemsAcrossBranches()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, product) = Seed(context);
        context.Inventories.Add(new Inventory { ProductId = product.Id, BranchId = branch.Id, QuantityOnHand = 5 });
        context.SaveChanges();

        var service = new AnalyticsService(context);
        var summary = await service.GetSummaryAsync();

        Assert.Equal(1, summary.LowStockItemCount);
    }

    [Fact]
    public async Task GetBusyPeriodsAsync_GroupsSalesByDayOfWeekAndHour()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, _) = Seed(context);
        var monday10am = new DateTime(2026, 8, 24, 10, 15, 0, DateTimeKind.Utc); // a Monday
        var monday10amLater = new DateTime(2026, 8, 24, 10, 45, 0, DateTimeKind.Utc);
        var tuesday3pm = new DateTime(2026, 8, 25, 15, 0, 0, DateTimeKind.Utc);

        AddSale(context, branch, 50m, monday10am);
        AddSale(context, branch, 50m, monday10amLater);
        AddSale(context, branch, 50m, tuesday3pm);

        var service = new AnalyticsService(context);
        var result = await service.GetBusyPeriodsAsync(lookbackDays: 365);

        var mondayTen = result.Single(p => p.DayOfWeek == DayOfWeek.Monday && p.Hour == 10);
        Assert.Equal(2, mondayTen.SaleCount);
        var tuesdayThree = result.Single(p => p.DayOfWeek == DayOfWeek.Tuesday && p.Hour == 15);
        Assert.Equal(1, tuesdayThree.SaleCount);
    }

    [Fact]
    public async Task GetBusyPeriodsAsync_ExcludesSalesOutsideLookbackWindow()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, _) = Seed(context);
        AddSale(context, branch, 50m, DateTime.UtcNow.AddDays(-200));

        var service = new AnalyticsService(context);
        var result = await service.GetBusyPeriodsAsync(lookbackDays: 90);

        Assert.Empty(result);
    }
}
