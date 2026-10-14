using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Inventories;
using FarmaciaSmart.Web.Services;

namespace FarmaciaSmart.Tests.Services;

public class InventoryServiceTests
{
    private static (Branch branch1, Branch branch2, Category category, Product product, Product expiringProduct) Seed(ApplicationDbContext context)
    {
        var branch1 = new Branch { Name = "Main Branch", Code = "MAIN", Address = "123 St", IsActive = true };
        var branch2 = new Branch { Name = "Second Branch", Code = "BR2", Address = "456 St", IsActive = true };
        var category = new Category { Name = "Pain Relief", IsActive = true };
        context.Branches.AddRange(branch1, branch2);
        context.Categories.Add(category);
        context.SaveChanges();

        var product = new Product { Name = "Biogesic 500mg", CategoryId = category.Id, Price = 85.50m, IsActive = true, LowStockThreshold = 20 };
        var expiringProduct = new Product
        {
            Name = "Amoxicillin 500mg", CategoryId = category.Id, Price = 50m, IsActive = true,
            LowStockThreshold = 10, RequiresExpirationTracking = true
        };
        context.Products.AddRange(product, expiringProduct);
        context.SaveChanges();

        return (branch1, branch2, category, product, expiringProduct);
    }

    [Fact]
    public async Task StockInAsync_CreatesInventoryBatchAndTransaction()
    {
        using var context = TestDbContextFactory.Create();
        var (branch1, _, _, product, _) = Seed(context);
        var service = new InventoryService(context);

        var (succeeded, error) = await service.StockInAsync(new StockInViewModel
        {
            ProductId = product.Id,
            BranchId = branch1.Id,
            BatchNumber = "BATCH-001",
            Quantity = 30
        }, "user-1");

        Assert.True(succeeded);
        Assert.Null(error);
        Assert.Equal(30, context.Inventories.Single().QuantityOnHand);
        Assert.Single(context.InventoryBatches);
        Assert.Single(context.InventoryTransactions);
        Assert.Equal(InventoryTransactionType.Purchase, context.InventoryTransactions.Single().TransactionType);
    }

    [Fact]
    public async Task StockInAsync_SecondCall_AccumulatesRatherThanDuplicates()
    {
        using var context = TestDbContextFactory.Create();
        var (branch1, _, _, product, _) = Seed(context);
        var service = new InventoryService(context);

        await service.StockInAsync(new StockInViewModel { ProductId = product.Id, BranchId = branch1.Id, BatchNumber = "B1", Quantity = 15 }, "user-1");
        await service.StockInAsync(new StockInViewModel { ProductId = product.Id, BranchId = branch1.Id, BatchNumber = "B2", Quantity = 30 }, "user-1");

        Assert.Single(context.Inventories);
        Assert.Equal(45, context.Inventories.Single().QuantityOnHand);
        Assert.Equal(2, context.InventoryBatches.Count());
    }

    [Fact]
    public async Task StockInAsync_ExpirationRequiredProduct_WithoutDate_Fails()
    {
        using var context = TestDbContextFactory.Create();
        var (branch1, _, _, _, expiringProduct) = Seed(context);
        var service = new InventoryService(context);

        var (succeeded, error) = await service.StockInAsync(new StockInViewModel
        {
            ProductId = expiringProduct.Id,
            BranchId = branch1.Id,
            BatchNumber = "B1",
            Quantity = 10
        }, "user-1");

        Assert.False(succeeded);
        Assert.Contains("requires an expiration date", error);
        Assert.Empty(context.Inventories);
    }

    [Fact]
    public async Task AdjustStockAsync_PositiveDelta_IncreasesQuantity()
    {
        using var context = TestDbContextFactory.Create();
        var (branch1, _, _, product, _) = Seed(context);
        var service = new InventoryService(context);
        await service.StockInAsync(new StockInViewModel { ProductId = product.Id, BranchId = branch1.Id, BatchNumber = "B1", Quantity = 10 }, "user-1");

        var (succeeded, _) = await service.AdjustStockAsync(new StockAdjustmentViewModel
        {
            ProductId = product.Id,
            BranchId = branch1.Id,
            AdjustmentQuantity = 5,
            Reason = "Recount"
        }, "user-1");

        Assert.True(succeeded);
        Assert.Equal(15, context.Inventories.Single().QuantityOnHand);
    }

    [Fact]
    public async Task AdjustStockAsync_NegativeDeltaExceedingStock_Fails()
    {
        using var context = TestDbContextFactory.Create();
        var (branch1, _, _, product, _) = Seed(context);
        var service = new InventoryService(context);
        await service.StockInAsync(new StockInViewModel { ProductId = product.Id, BranchId = branch1.Id, BatchNumber = "B1", Quantity = 10 }, "user-1");

        var (succeeded, error) = await service.AdjustStockAsync(new StockAdjustmentViewModel
        {
            ProductId = product.Id,
            BranchId = branch1.Id,
            AdjustmentQuantity = -20,
            Reason = "Recount"
        }, "user-1");

        Assert.False(succeeded);
        Assert.Contains("negative stock", error);
        Assert.Equal(10, context.Inventories.Single().QuantityOnHand);
    }

    [Fact]
    public async Task AdjustStockAsync_ZeroDelta_Fails()
    {
        using var context = TestDbContextFactory.Create();
        var (branch1, _, _, product, _) = Seed(context);
        var service = new InventoryService(context);

        var (succeeded, error) = await service.AdjustStockAsync(new StockAdjustmentViewModel
        {
            ProductId = product.Id,
            BranchId = branch1.Id,
            AdjustmentQuantity = 0,
            Reason = "No-op"
        }, "user-1");

        Assert.False(succeeded);
        Assert.Contains("cannot be zero", error);
    }

    [Fact]
    public async Task TransferStockAsync_SameBranch_Fails()
    {
        using var context = TestDbContextFactory.Create();
        var (branch1, _, _, product, _) = Seed(context);
        var service = new InventoryService(context);
        await service.StockInAsync(new StockInViewModel { ProductId = product.Id, BranchId = branch1.Id, BatchNumber = "B1", Quantity = 20 }, "user-1");
        var batch = context.InventoryBatches.Single();

        var (succeeded, error) = await service.TransferStockAsync(new TransferStockViewModel
        {
            SourceInventoryBatchId = batch.Id,
            DestinationBranchId = branch1.Id,
            Quantity = 5
        }, "user-1");

        Assert.False(succeeded);
        Assert.Contains("cannot be the same", error);
    }

    [Fact]
    public async Task TransferStockAsync_MovesStockAndCreatesDestinationBatch()
    {
        using var context = TestDbContextFactory.Create();
        var (branch1, branch2, _, product, _) = Seed(context);
        var service = new InventoryService(context);
        await service.StockInAsync(new StockInViewModel { ProductId = product.Id, BranchId = branch1.Id, BatchNumber = "B1", Quantity = 20 }, "user-1");
        var batch = context.InventoryBatches.Single();

        var (succeeded, _) = await service.TransferStockAsync(new TransferStockViewModel
        {
            SourceInventoryBatchId = batch.Id,
            DestinationBranchId = branch2.Id,
            Quantity = 8
        }, "user-1");

        Assert.True(succeeded);
        Assert.Equal(12, context.Inventories.Single(i => i.BranchId == branch1.Id).QuantityOnHand);
        Assert.Equal(8, context.Inventories.Single(i => i.BranchId == branch2.Id).QuantityOnHand);
        Assert.Equal(2, context.InventoryBatches.Count());
    }

    [Fact]
    public async Task ReportDamagedAsync_ExceedingBatchRemaining_Fails()
    {
        using var context = TestDbContextFactory.Create();
        var (branch1, _, _, product, _) = Seed(context);
        var service = new InventoryService(context);
        await service.StockInAsync(new StockInViewModel { ProductId = product.Id, BranchId = branch1.Id, BatchNumber = "B1", Quantity = 5 }, "user-1");
        var batch = context.InventoryBatches.Single();

        var (succeeded, error) = await service.ReportDamagedAsync(new DamagedItemViewModel
        {
            InventoryBatchId = batch.Id,
            Quantity = 999,
            Reason = "Testing"
        }, "user-1");

        Assert.False(succeeded);
        Assert.Contains("unit(s) remain", error);
    }

    [Fact]
    public async Task ReportDamagedAsync_ValidQuantity_DecrementsBatchAndInventory()
    {
        using var context = TestDbContextFactory.Create();
        var (branch1, _, _, product, _) = Seed(context);
        var service = new InventoryService(context);
        await service.StockInAsync(new StockInViewModel { ProductId = product.Id, BranchId = branch1.Id, BatchNumber = "B1", Quantity = 10 }, "user-1");
        var batch = context.InventoryBatches.Single();

        var (succeeded, _) = await service.ReportDamagedAsync(new DamagedItemViewModel
        {
            InventoryBatchId = batch.Id,
            Quantity = 3,
            Reason = "Crushed in transit"
        }, "user-1");

        Assert.True(succeeded);
        Assert.Equal(7, context.InventoryBatches.Single().QuantityRemaining);
        Assert.Equal(7, context.Inventories.Single().QuantityOnHand);
    }
}
