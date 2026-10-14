using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Services;

namespace FarmaciaSmart.Tests.Services;

public class CustomerDashboardServiceTests
{
    private static (Branch branch, Customer customer, Category category) Seed(ApplicationDbContext context)
    {
        var branch = new Branch { Name = "Main Branch", Code = "MAIN", Address = "123 St", IsActive = true };
        var category = new Category { Name = "Pain Relief", IsActive = true };
        context.Branches.Add(branch);
        context.Categories.Add(category);
        context.SaveChanges();

        var user = new ApplicationUser { Id = "user-1", UserName = "jane@example.com", Email = "jane@example.com", FirstName = "Jane", LastName = "Doe", IsActive = true };
        context.Users.Add(user);
        var cashier = new ApplicationUser { Id = "cashier-1", UserName = "cashier@example.com", Email = "cashier@example.com", FirstName = "C", LastName = "R", IsActive = true };
        context.Users.Add(cashier);
        var customer = new Customer { UserId = user.Id, Status = CustomerStatus.Active };
        context.Customers.Add(customer);
        context.SaveChanges();

        return (branch, customer, category);
    }

    private static void AddSale(ApplicationDbContext context, Branch branch, Customer customer, decimal total, DateTime saleDate, SaleStatus status = SaleStatus.Completed)
    {
        context.Sales.Add(new Sale
        {
            ReceiptNumber = $"R-{Guid.NewGuid():N}"[..10],
            BranchId = branch.Id,
            CustomerId = customer.Id,
            CashierUserId = "cashier-1",
            SubTotal = total,
            TotalAmount = total,
            Status = status,
            SaleDate = saleDate
        });
        context.SaveChanges();
    }

    [Fact]
    public async Task GetDashboardAsync_UnknownCustomer_ReturnsNull()
    {
        using var context = TestDbContextFactory.Create();
        var service = new CustomerDashboardService(context);

        var result = await service.GetDashboardAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDashboardAsync_ComputesTotalSpentFromCompletedSalesOnly()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, customer, _) = Seed(context);
        AddSale(context, branch, customer, 100m, DateTime.UtcNow);
        AddSale(context, branch, customer, 50m, DateTime.UtcNow, SaleStatus.Voided);

        var service = new CustomerDashboardService(context);
        var result = await service.GetDashboardAsync(customer.Id);

        Assert.Equal(100m, result!.TotalSpent);
        Assert.Single(result.RecentPurchases);
    }

    [Fact]
    public async Task GetDashboardAsync_SpendingTrend_HasSixMonthsNewestLast()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, customer, _) = Seed(context);
        AddSale(context, branch, customer, 75m, DateTime.UtcNow);

        var service = new CustomerDashboardService(context);
        var result = await service.GetDashboardAsync(customer.Id);

        Assert.Equal(6, result!.SpendingTrend.Count);
        Assert.Equal(DateTime.UtcNow.ToString("MMM"), result.SpendingTrend[^1].MonthLabel);
        Assert.Equal(75m, result.SpendingTrend[^1].TotalAmount);
    }

    [Fact]
    public async Task GetDashboardAsync_CountsOrdersByStatus()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, customer, _) = Seed(context);
        context.CustomerOrders.AddRange(
            new CustomerOrder { OrderNumber = "CO-000001", CustomerId = customer.Id, BranchId = branch.Id, Status = CustomerOrderStatus.Pending },
            new CustomerOrder { OrderNumber = "CO-000002", CustomerId = customer.Id, BranchId = branch.Id, Status = CustomerOrderStatus.Fulfilled },
            new CustomerOrder { OrderNumber = "CO-000003", CustomerId = customer.Id, BranchId = branch.Id, Status = CustomerOrderStatus.Cancelled });
        context.SaveChanges();

        var service = new CustomerDashboardService(context);
        var result = await service.GetDashboardAsync(customer.Id);

        Assert.Equal(3, result!.TotalOrders);
        Assert.Equal(1, result.PendingOrders);
        Assert.Equal(1, result.PendingOrderCount);
        Assert.Equal(1, result.FulfilledOrderCount);
        Assert.Equal(1, result.CancelledOrderCount);
    }

    [Fact]
    public async Task GetDashboardAsync_RecentRecommendations_OrderedNewestFirstAndLimited()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, customer, category) = Seed(context);
        for (var i = 0; i < 7; i++)
        {
            context.Recommendations.Add(new Recommendation
            {
                CustomerId = customer.Id,
                CategoryId = category.Id,
                BranchId = branch.Id,
                RequestText = $"Request {i}",
                CreatedAt = DateTime.UtcNow.AddDays(-i)
            });
        }
        context.SaveChanges();

        var service = new CustomerDashboardService(context);
        var result = await service.GetDashboardAsync(customer.Id);

        Assert.Equal(7, result!.RecommendationsReceived);
        Assert.Equal(5, result.RecentRecommendations.Count);
        Assert.Equal("Request 0", result.RecentRecommendations[0].RequestText);
    }
}
