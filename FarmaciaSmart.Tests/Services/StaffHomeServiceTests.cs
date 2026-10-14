using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Services;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Tests.Services;

public class StaffHomeServiceTests
{
    private class FakeRecommendationService(List<Recommendation> pendingReviews) : IRecommendationService
    {
        public Task<(bool Succeeded, string? Error, int? RecommendationId)> GenerateRecommendationAsync(
            int customerId, int categoryId, int? branchId, decimal? maxPrice = null, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Recommendation?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<List<Recommendation>> GetPendingReviewAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(pendingReviews);

        public Task<(bool Succeeded, string? Error)> ReviewAsync(
            int recommendationId, RecommendationReviewStatus status, string reviewedByUserId, string? notes, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private static StaffHomeService CreateService(ApplicationDbContext context, int pendingReviewCount = 0)
    {
        var pending = Enumerable.Range(0, pendingReviewCount).Select(_ => new Recommendation()).ToList();
        return new StaffHomeService(context, new AnalyticsService(context), new FakeRecommendationService(pending));
    }

    private static (Branch branch1, Branch branch2, Product product, Employee employee) Seed(ApplicationDbContext context)
    {
        var branch1 = new Branch { Name = "Main Branch", Code = "MAIN", Address = "123 St", IsActive = true };
        var branch2 = new Branch { Name = "Second Branch", Code = "BR2", Address = "456 St", IsActive = true };
        var category = new Category { Name = "Pain Relief", IsActive = true };
        context.Branches.AddRange(branch1, branch2);
        context.Categories.Add(category);
        context.SaveChanges();

        var cashier = new ApplicationUser { Id = "cashier-1", UserName = "cashier@example.com", Email = "cashier@example.com", FirstName = "C", LastName = "R", IsActive = true };
        context.Users.Add(cashier);

        var product = new Product { Name = "Biogesic 500mg", CategoryId = category.Id, Price = 85.50m, IsActive = true, LowStockThreshold = 20 };
        context.Products.Add(product);
        context.SaveChanges();

        var employee = new Employee
        {
            FirstName = "Ana", LastName = "Reyes", Position = "Cashier", BranchId = branch1.Id,
            HireDate = new DateTime(2026, 1, 1), MonthlySalary = 20000m
        };
        context.Employees.Add(employee);
        context.SaveChanges();

        return (branch1, branch2, product, employee);
    }

    private static void AddSale(ApplicationDbContext context, Branch branch, decimal total, DateTime saleDate)
    {
        context.Sales.Add(new Sale
        {
            ReceiptNumber = $"R-{Guid.NewGuid():N}"[..10],
            BranchId = branch.Id,
            CashierUserId = "cashier-1",
            SubTotal = total,
            TotalAmount = total,
            Status = SaleStatus.Completed,
            SaleDate = saleDate
        });
        context.SaveChanges();
    }

    [Fact]
    public async Task GetSummaryAsync_NoRolesGranted_ReturnsAllNulls()
    {
        using var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var result = await service.GetSummaryAsync(false, false, false, false, false, null);

        Assert.Null(result.BusinessOverview);
        Assert.Null(result.TodaysSalesCount);
        Assert.Null(result.LowStockCount);
        Assert.Null(result.PendingReviewsCount);
        Assert.Null(result.ClockedInTodayCount);
    }

    [Fact]
    public async Task GetSummaryAsync_Cashier_ComputesTodaysSalesOnly()
    {
        using var context = TestDbContextFactory.Create();
        var (branch1, _, _, _) = Seed(context);
        AddSale(context, branch1, 100m, DateTime.UtcNow);
        AddSale(context, branch1, 50m, DateTime.UtcNow.AddDays(-1));
        var service = CreateService(context);

        var result = await service.GetSummaryAsync(false, true, false, false, false, null);

        Assert.Equal(1, result.TodaysSalesCount);
        Assert.Equal(100m, result.TodaysSalesTotal);
        Assert.Null(result.BusinessOverview);
        Assert.Null(result.LowStockCount);
    }

    [Fact]
    public async Task GetSummaryAsync_Cashier_ScopedToBranchWhenBranchIdProvided()
    {
        using var context = TestDbContextFactory.Create();
        var (branch1, branch2, _, _) = Seed(context);
        AddSale(context, branch1, 100m, DateTime.UtcNow);
        AddSale(context, branch2, 200m, DateTime.UtcNow);
        var service = CreateService(context);

        var result = await service.GetSummaryAsync(false, true, false, false, false, branch1.Id);

        Assert.Equal(1, result.TodaysSalesCount);
        Assert.Equal(100m, result.TodaysSalesTotal);
    }

    [Fact]
    public async Task GetSummaryAsync_InventoryStaff_CountsLowStockAndExpiringSoon()
    {
        using var context = TestDbContextFactory.Create();
        var (branch1, _, product, _) = Seed(context);
        context.Inventories.Add(new Inventory { ProductId = product.Id, BranchId = branch1.Id, QuantityOnHand = 5 });
        context.InventoryBatches.Add(new InventoryBatch
        {
            ProductId = product.Id, BranchId = branch1.Id, BatchNumber = "B1",
            ExpirationDate = DateTime.UtcNow.AddDays(10), QuantityReceived = 5, QuantityRemaining = 5
        });
        context.SaveChanges();
        var service = CreateService(context);

        var result = await service.GetSummaryAsync(false, false, true, false, false, null);

        Assert.Equal(1, result.LowStockCount);
        Assert.Equal(1, result.ExpiringSoonCount);
    }

    [Fact]
    public async Task GetSummaryAsync_Pharmacist_CountsPendingReviews()
    {
        using var context = TestDbContextFactory.Create();
        Seed(context);
        var service = CreateService(context, pendingReviewCount: 3);

        var result = await service.GetSummaryAsync(false, false, false, true, false, null);

        Assert.Equal(3, result.PendingReviewsCount);
        Assert.Null(result.BusinessOverview);
    }

    [Fact]
    public async Task GetSummaryAsync_HrPayroll_CountsClockedInAndDraftPayrolls()
    {
        using var context = TestDbContextFactory.Create();
        var (_, _, _, employee) = Seed(context);
        context.Attendances.Add(new Attendance { EmployeeId = employee.Id, AttendanceDate = DateOnly.FromDateTime(DateTime.UtcNow), TimeIn = DateTime.UtcNow });
        context.Payrolls.Add(new Payroll
        {
            EmployeeId = employee.Id, PeriodStart = new DateOnly(2026, 8, 1), PeriodEnd = new DateOnly(2026, 8, 15),
            Status = PayrollStatus.Draft, ProcessedByUserId = "cashier-1"
        });
        context.SaveChanges();
        var service = CreateService(context);

        var result = await service.GetSummaryAsync(false, false, false, false, true, null);

        Assert.Equal(1, result.ClockedInTodayCount);
        Assert.Equal(1, result.DraftPayrollCount);
    }

    [Fact]
    public async Task GetSummaryAsync_Administrator_SeesEverySection()
    {
        using var context = TestDbContextFactory.Create();
        Seed(context);
        var service = CreateService(context);

        var result = await service.GetSummaryAsync(true, false, false, false, false, null);

        Assert.NotNull(result.BusinessOverview);
        Assert.NotNull(result.TodaysSalesCount);
        Assert.NotNull(result.LowStockCount);
        Assert.NotNull(result.PendingReviewsCount);
        Assert.NotNull(result.ClockedInTodayCount);
    }
}
