using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Purchasing;
using FarmaciaSmart.Web.Services;

namespace FarmaciaSmart.Tests.Services;

public class PurchasingServiceTests
{
    private static (Branch branch, Supplier supplier, Product product, Product expiringProduct) Seed(ApplicationDbContext context)
    {
        var branch = new Branch { Name = "Main Branch", Code = "MAIN", Address = "123 St", IsActive = true };
        var supplier = new Supplier { Name = "Metro Drug Distributor", IsActive = true };
        var category = new Category { Name = "Pain Relief", IsActive = true };
        context.Branches.Add(branch);
        context.Suppliers.Add(supplier);
        context.Categories.Add(category);
        context.SaveChanges();

        var product = new Product { Name = "Biogesic 500mg", CategoryId = category.Id, Price = 85.50m, IsActive = true };
        var expiringProduct = new Product
        {
            Name = "Amoxicillin 500mg", CategoryId = category.Id, Price = 50m, IsActive = true,
            RequiresExpirationTracking = true
        };
        context.Products.AddRange(product, expiringProduct);
        context.SaveChanges();

        return (branch, supplier, product, expiringProduct);
    }

    private static async Task<PurchaseOrder> CreateDraftWithOneItemAsync(
        ApplicationDbContext context, PurchasingService service, Branch branch, Supplier supplier, Product product, int quantity = 20, decimal unitCost = 40m)
    {
        var (_, _, purchaseOrderId) = await service.CreateDraftAsync(
            new CreatePurchaseOrderViewModel { SupplierId = supplier.Id, BranchId = branch.Id }, "user-1");

        await service.AddItemAsync(purchaseOrderId!.Value, new PurchaseOrderItemViewModel
        {
            ProductId = product.Id,
            QuantityOrdered = quantity,
            UnitCost = unitCost
        });

        return context.PurchaseOrders.Single(p => p.Id == purchaseOrderId);
    }

    [Fact]
    public async Task CreateDraftAsync_AssignsOrderNumberAndDraftStatus()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, supplier, _, _) = Seed(context);
        var service = new PurchasingService(context);

        var (succeeded, error, purchaseOrderId) = await service.CreateDraftAsync(
            new CreatePurchaseOrderViewModel { SupplierId = supplier.Id, BranchId = branch.Id }, "user-1");

        Assert.True(succeeded);
        Assert.Null(error);
        var order = context.PurchaseOrders.Single(p => p.Id == purchaseOrderId);
        Assert.Equal($"PO-{order.Id:D6}", order.OrderNumber);
        Assert.Equal(PurchaseOrderStatus.Draft, order.Status);
    }

    [Fact]
    public async Task CreateDraftAsync_InactiveSupplier_Fails()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, supplier, _, _) = Seed(context);
        supplier.IsActive = false;
        context.SaveChanges();
        var service = new PurchasingService(context);

        var (succeeded, error, _) = await service.CreateDraftAsync(
            new CreatePurchaseOrderViewModel { SupplierId = supplier.Id, BranchId = branch.Id }, "user-1");

        Assert.False(succeeded);
        Assert.Contains("supplier", error);
    }

    [Fact]
    public async Task AddItemAndRemoveItem_RecalculateTotalAmount()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, supplier, product, _) = Seed(context);
        var service = new PurchasingService(context);
        var order = await CreateDraftWithOneItemAsync(context, service, branch, supplier, product, quantity: 20, unitCost: 40m);

        Assert.Equal(800m, context.PurchaseOrders.Single(p => p.Id == order.Id).TotalAmount);

        var item = context.PurchaseOrderItems.Single(i => i.PurchaseOrderId == order.Id);
        await service.RemoveItemAsync(item.Id);

        Assert.Equal(0m, context.PurchaseOrders.Single(p => p.Id == order.Id).TotalAmount);
        Assert.Empty(context.PurchaseOrderItems);
    }

    [Fact]
    public async Task AddItemAsync_OnNonDraftOrder_Fails()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, supplier, product, _) = Seed(context);
        var service = new PurchasingService(context);
        var order = await CreateDraftWithOneItemAsync(context, service, branch, supplier, product);
        await service.PlaceOrderAsync(order.Id);

        var (succeeded, error) = await service.AddItemAsync(order.Id, new PurchaseOrderItemViewModel
        {
            ProductId = product.Id,
            QuantityOrdered = 5,
            UnitCost = 10m
        });

        Assert.False(succeeded);
        Assert.Contains("draft", error);
    }

    [Fact]
    public async Task PlaceOrderAsync_WithNoItems_Fails()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, supplier, _, _) = Seed(context);
        var service = new PurchasingService(context);
        var (_, _, purchaseOrderId) = await service.CreateDraftAsync(
            new CreatePurchaseOrderViewModel { SupplierId = supplier.Id, BranchId = branch.Id }, "user-1");

        var (succeeded, error) = await service.PlaceOrderAsync(purchaseOrderId!.Value);

        Assert.False(succeeded);
        Assert.Contains("at least one item", error);
    }

    [Fact]
    public async Task ReceiveItemAsync_FullQuantity_UpdatesInventoryAndMarksReceived()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, supplier, product, _) = Seed(context);
        var service = new PurchasingService(context);
        var order = await CreateDraftWithOneItemAsync(context, service, branch, supplier, product, quantity: 20, unitCost: 40m);
        await service.PlaceOrderAsync(order.Id);
        var item = context.PurchaseOrderItems.Single(i => i.PurchaseOrderId == order.Id);

        var (succeeded, error) = await service.ReceiveItemAsync(new ReceivePurchaseOrderItemViewModel
        {
            PurchaseOrderItemId = item.Id,
            Quantity = 20,
            BatchNumber = "BATCH-PO-1"
        }, "receiver-1");

        Assert.True(succeeded);
        Assert.Null(error);
        Assert.Equal(20, context.Inventories.Single().QuantityOnHand);
        Assert.Single(context.InventoryBatches);
        Assert.Equal(InventoryTransactionType.Purchase, context.InventoryTransactions.Single().TransactionType);
        Assert.Equal(order.OrderNumber, context.InventoryTransactions.Single().ReferenceNumber);
        Assert.Equal(PurchaseOrderStatus.Received, context.PurchaseOrders.Single(p => p.Id == order.Id).Status);
    }

    [Fact]
    public async Task ReceiveItemAsync_PartialQuantity_MarksPartiallyReceived()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, supplier, product, _) = Seed(context);
        var service = new PurchasingService(context);
        var order = await CreateDraftWithOneItemAsync(context, service, branch, supplier, product, quantity: 20, unitCost: 40m);
        await service.PlaceOrderAsync(order.Id);
        var item = context.PurchaseOrderItems.Single(i => i.PurchaseOrderId == order.Id);

        await service.ReceiveItemAsync(new ReceivePurchaseOrderItemViewModel
        {
            PurchaseOrderItemId = item.Id,
            Quantity = 5,
            BatchNumber = "BATCH-PO-1"
        }, "receiver-1");

        Assert.Equal(PurchaseOrderStatus.PartiallyReceived, context.PurchaseOrders.Single(p => p.Id == order.Id).Status);
        Assert.Equal(5, context.Inventories.Single().QuantityOnHand);
    }

    [Fact]
    public async Task ReceiveItemAsync_QuantityExceedsRemaining_Fails()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, supplier, product, _) = Seed(context);
        var service = new PurchasingService(context);
        var order = await CreateDraftWithOneItemAsync(context, service, branch, supplier, product, quantity: 20, unitCost: 40m);
        await service.PlaceOrderAsync(order.Id);
        var item = context.PurchaseOrderItems.Single(i => i.PurchaseOrderId == order.Id);

        var (succeeded, error) = await service.ReceiveItemAsync(new ReceivePurchaseOrderItemViewModel
        {
            PurchaseOrderItemId = item.Id,
            Quantity = 999,
            BatchNumber = "BATCH-PO-1"
        }, "receiver-1");

        Assert.False(succeeded);
        Assert.Contains("remain to be received", error);
    }

    [Fact]
    public async Task ReceiveItemAsync_ExpirationRequiredProduct_WithoutDate_Fails()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, supplier, _, expiringProduct) = Seed(context);
        var service = new PurchasingService(context);
        var order = await CreateDraftWithOneItemAsync(context, service, branch, supplier, expiringProduct, quantity: 10, unitCost: 20m);
        await service.PlaceOrderAsync(order.Id);
        var item = context.PurchaseOrderItems.Single(i => i.PurchaseOrderId == order.Id);

        var (succeeded, error) = await service.ReceiveItemAsync(new ReceivePurchaseOrderItemViewModel
        {
            PurchaseOrderItemId = item.Id,
            Quantity = 10,
            BatchNumber = "BATCH-PO-1"
        }, "receiver-1");

        Assert.False(succeeded);
        Assert.Contains("expiration date", error);
    }

    [Fact]
    public async Task CancelAsync_AfterPartiallyReceived_Fails()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, supplier, product, _) = Seed(context);
        var service = new PurchasingService(context);
        var order = await CreateDraftWithOneItemAsync(context, service, branch, supplier, product, quantity: 20, unitCost: 40m);
        await service.PlaceOrderAsync(order.Id);
        var item = context.PurchaseOrderItems.Single(i => i.PurchaseOrderId == order.Id);
        await service.ReceiveItemAsync(new ReceivePurchaseOrderItemViewModel
        {
            PurchaseOrderItemId = item.Id,
            Quantity = 5,
            BatchNumber = "BATCH-PO-1"
        }, "receiver-1");

        var (succeeded, error) = await service.CancelAsync(order.Id);

        Assert.False(succeeded);
        Assert.Contains("draft or ordered", error);
    }

    [Fact]
    public async Task CancelAsync_FromDraft_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, supplier, product, _) = Seed(context);
        var service = new PurchasingService(context);
        var order = await CreateDraftWithOneItemAsync(context, service, branch, supplier, product);

        var (succeeded, error) = await service.CancelAsync(order.Id);

        Assert.True(succeeded);
        Assert.Null(error);
        Assert.Equal(PurchaseOrderStatus.Cancelled, context.PurchaseOrders.Single(p => p.Id == order.Id).Status);
    }
}
