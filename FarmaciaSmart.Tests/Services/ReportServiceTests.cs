using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Services;

namespace FarmaciaSmart.Tests.Services;

public class ReportServiceTests
{
    private static (Branch branch1, Branch branch2, Supplier supplier1, Supplier supplier2) SeedPurchaseOrders(ApplicationDbContext context)
    {
        var branch1 = new Branch { Name = "Main Branch", Code = "MAIN", Address = "123 St", IsActive = true };
        var branch2 = new Branch { Name = "Second Branch", Code = "BR2", Address = "456 St", IsActive = true };
        var supplier1 = new Supplier { Name = "Metro Drug Distributor", IsActive = true };
        var supplier2 = new Supplier { Name = "Zuellig Pharma", IsActive = true };
        context.Branches.AddRange(branch1, branch2);
        context.Suppliers.AddRange(supplier1, supplier2);
        context.SaveChanges();

        context.PurchaseOrders.AddRange(
            new PurchaseOrder
            {
                OrderNumber = "PO-000001", SupplierId = supplier1.Id, BranchId = branch1.Id,
                TotalAmount = 500m, CreatedByUserId = "user-1", CreatedAt = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new PurchaseOrder
            {
                OrderNumber = "PO-000002", SupplierId = supplier2.Id, BranchId = branch2.Id,
                TotalAmount = 300m, CreatedByUserId = "user-1", CreatedAt = new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc)
            });
        context.SaveChanges();

        return (branch1, branch2, supplier1, supplier2);
    }

    [Fact]
    public async Task GetPurchasesReportAsync_NoFilters_ReturnsAllOrders()
    {
        using var context = TestDbContextFactory.Create();
        SeedPurchaseOrders(context);
        var service = new ReportService(context, new InventoryService(context));

        var results = await service.GetPurchasesReportAsync(null, null, null, null);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task GetPurchasesReportAsync_FilteredByBranch_ReturnsOnlyThatBranch()
    {
        using var context = TestDbContextFactory.Create();
        var (branch1, _, _, _) = SeedPurchaseOrders(context);
        var service = new ReportService(context, new InventoryService(context));

        var results = await service.GetPurchasesReportAsync(null, null, branch1.Id, null);

        Assert.Single(results);
        Assert.Equal("PO-000001", results[0].OrderNumber);
    }

    [Fact]
    public async Task GetPurchasesReportAsync_FilteredByDateRange_ExcludesOutOfRangeOrders()
    {
        using var context = TestDbContextFactory.Create();
        SeedPurchaseOrders(context);
        var service = new ReportService(context, new InventoryService(context));

        var results = await service.GetPurchasesReportAsync(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 10), null, null);

        Assert.Single(results);
        Assert.Equal("PO-000001", results[0].OrderNumber);
    }

    [Fact]
    public async Task GetPurchasesReportAsync_FilteredBySupplier_ReturnsOnlyThatSupplier()
    {
        using var context = TestDbContextFactory.Create();
        var (_, _, _, supplier2) = SeedPurchaseOrders(context);
        var service = new ReportService(context, new InventoryService(context));

        var results = await service.GetPurchasesReportAsync(null, null, null, supplier2.Id);

        Assert.Single(results);
        Assert.Equal("PO-000002", results[0].OrderNumber);
    }
}
