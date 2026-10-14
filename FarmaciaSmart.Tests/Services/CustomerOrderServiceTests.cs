using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Sales;
using FarmaciaSmart.Web.Services;

namespace FarmaciaSmart.Tests.Services;

public class CustomerOrderServiceTests
{
    private static (Branch branch, Customer customer, Product product) Seed(ApplicationDbContext context)
    {
        var branch = new Branch { Name = "Main Branch", Code = "MAIN", Address = "123 St", IsActive = true };
        var category = new Category { Name = "Pain Relief", IsActive = true };
        context.Branches.Add(branch);
        context.Categories.Add(category);
        context.SaveChanges();

        var user = new ApplicationUser { Id = "user-1", UserName = "jane@example.com", Email = "jane@example.com", FirstName = "Jane", LastName = "Doe", IsActive = true };
        context.Users.Add(user);
        var customer = new Customer { UserId = user.Id, Status = CustomerStatus.Active };
        context.Customers.Add(customer);

        var product = new Product { Name = "Biogesic 500mg", CategoryId = category.Id, Price = 85.50m, IsActive = true };
        context.Products.Add(product);
        context.SaveChanges();

        context.Inventories.Add(new Inventory { ProductId = product.Id, BranchId = branch.Id, QuantityOnHand = 100 });
        context.SaveChanges();

        return (branch, customer, product);
    }

    [Fact]
    public async Task SubmitOrderAsync_CreatesOrderWithSnapshotPriceAndOrderNumber()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, customer, product) = Seed(context);
        var service = new CustomerOrderService(context);
        var cart = new List<CartItemDto> { new() { ProductId = product.Id, ProductName = product.Name, UnitPrice = product.Price, Quantity = 2 } };

        var (succeeded, error, orderId) = await service.SubmitOrderAsync(customer.Id, branch.Id, cart, "Please pack separately");

        Assert.True(succeeded);
        Assert.Null(error);
        var order = context.CustomerOrders.Single(o => o.Id == orderId);
        Assert.Equal($"CO-{order.Id:D6}", order.OrderNumber);
        Assert.Equal(CustomerOrderStatus.Pending, order.Status);
        Assert.Equal(171.00m, order.TotalAmount);

        var item = context.CustomerOrderItems.Single(i => i.CustomerOrderId == order.Id);
        Assert.Equal(85.50m, item.UnitPriceAtOrderTime);
        Assert.Equal(2, item.Quantity);
    }

    [Fact]
    public async Task SubmitOrderAsync_EmptyCart_Fails()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, customer, _) = Seed(context);
        var service = new CustomerOrderService(context);

        var (succeeded, error, orderId) = await service.SubmitOrderAsync(customer.Id, branch.Id, [], null);

        Assert.False(succeeded);
        Assert.Null(orderId);
        Assert.Contains("empty", error);
    }

    [Fact]
    public async Task SubmitOrderAsync_ProductNoLongerActive_Fails()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, customer, product) = Seed(context);
        product.IsActive = false;
        context.SaveChanges();
        var service = new CustomerOrderService(context);
        var cart = new List<CartItemDto> { new() { ProductId = product.Id, ProductName = product.Name, UnitPrice = product.Price, Quantity = 1 } };

        var (succeeded, error, _) = await service.SubmitOrderAsync(customer.Id, branch.Id, cart, null);

        Assert.False(succeeded);
        Assert.Contains("no longer available", error);
    }

    [Fact]
    public async Task FulfillAsync_PendingOrder_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, customer, product) = Seed(context);
        var service = new CustomerOrderService(context);
        var cart = new List<CartItemDto> { new() { ProductId = product.Id, ProductName = product.Name, UnitPrice = product.Price, Quantity = 1 } };
        var (_, _, orderId) = await service.SubmitOrderAsync(customer.Id, branch.Id, cart, null);

        var (succeeded, error) = await service.FulfillAsync(orderId!.Value, "staff-1");

        Assert.True(succeeded);
        Assert.Null(error);
        var order = context.CustomerOrders.Single(o => o.Id == orderId);
        Assert.Equal(CustomerOrderStatus.Fulfilled, order.Status);
        Assert.Equal("staff-1", order.FulfilledByUserId);
        Assert.NotNull(order.FulfilledAt);
    }

    [Fact]
    public async Task FulfillAsync_AlreadyFulfilled_Fails()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, customer, product) = Seed(context);
        var service = new CustomerOrderService(context);
        var cart = new List<CartItemDto> { new() { ProductId = product.Id, ProductName = product.Name, UnitPrice = product.Price, Quantity = 1 } };
        var (_, _, orderId) = await service.SubmitOrderAsync(customer.Id, branch.Id, cart, null);
        await service.FulfillAsync(orderId!.Value, "staff-1");

        var (succeeded, error) = await service.FulfillAsync(orderId.Value, "staff-1");

        Assert.False(succeeded);
        Assert.Contains("pending", error);
    }

    [Fact]
    public async Task CancelAsync_PendingOrder_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, customer, product) = Seed(context);
        var service = new CustomerOrderService(context);
        var cart = new List<CartItemDto> { new() { ProductId = product.Id, ProductName = product.Name, UnitPrice = product.Price, Quantity = 1 } };
        var (_, _, orderId) = await service.SubmitOrderAsync(customer.Id, branch.Id, cart, null);

        var (succeeded, error) = await service.CancelAsync(orderId!.Value);

        Assert.True(succeeded);
        Assert.Null(error);
        Assert.Equal(CustomerOrderStatus.Cancelled, context.CustomerOrders.Single(o => o.Id == orderId).Status);
    }

    [Fact]
    public async Task SubmitOrderAsync_QuantityExceedsStock_Fails()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, customer, product) = Seed(context);
        var service = new CustomerOrderService(context);
        var cart = new List<CartItemDto> { new() { ProductId = product.Id, ProductName = product.Name, UnitPrice = product.Price, Quantity = 101 } };

        var (succeeded, error, orderId) = await service.SubmitOrderAsync(customer.Id, branch.Id, cart, null);

        Assert.False(succeeded);
        Assert.Null(orderId);
        Assert.Contains("Only 100 unit(s)", error);
    }

    [Fact]
    public async Task SubmitOrderAsync_SecondCustomer_CannotJointlyExceedStock()
    {
        // The exact shortage scenario: 100 units in stock, customer A pre-orders 70,
        // customer B then tries to pre-order 50 -- should be capped to the 30 remaining,
        // not allowed to also succeed at 50 (which would jointly claim 120 of 100 units).
        using var context = TestDbContextFactory.Create();
        var (branch, customerA, product) = Seed(context);
        var userB = new ApplicationUser { Id = "user-2", UserName = "bob@example.com", Email = "bob@example.com", FirstName = "Bob", LastName = "Roe", IsActive = true };
        context.Users.Add(userB);
        var customerB = new Customer { UserId = userB.Id, Status = CustomerStatus.Active };
        context.Customers.Add(customerB);
        context.SaveChanges();

        var service = new CustomerOrderService(context);
        var cartA = new List<CartItemDto> { new() { ProductId = product.Id, ProductName = product.Name, UnitPrice = product.Price, Quantity = 70 } };
        var (succeededA, _, _) = await service.SubmitOrderAsync(customerA.Id, branch.Id, cartA, null);
        Assert.True(succeededA);

        var cartB = new List<CartItemDto> { new() { ProductId = product.Id, ProductName = product.Name, UnitPrice = product.Price, Quantity = 50 } };
        var (succeededB, errorB, orderIdB) = await service.SubmitOrderAsync(customerB.Id, branch.Id, cartB, null);

        Assert.False(succeededB);
        Assert.Null(orderIdB);
        Assert.Contains("Only 30 unit(s)", errorB);
    }

    [Fact]
    public async Task SubmitOrderAsync_CancelledOrdersDoNotCountAgainstAvailability()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, customer, product) = Seed(context);
        var service = new CustomerOrderService(context);
        var firstCart = new List<CartItemDto> { new() { ProductId = product.Id, ProductName = product.Name, UnitPrice = product.Price, Quantity = 90 } };
        var (_, _, firstOrderId) = await service.SubmitOrderAsync(customer.Id, branch.Id, firstCart, null);
        await service.CancelAsync(firstOrderId!.Value);

        var secondCart = new List<CartItemDto> { new() { ProductId = product.Id, ProductName = product.Name, UnitPrice = product.Price, Quantity = 90 } };
        var (succeeded, error, _) = await service.SubmitOrderAsync(customer.Id, branch.Id, secondCart, null);

        Assert.True(succeeded);
        Assert.Null(error);
    }

    [Fact]
    public async Task CancelAsync_AlreadyFulfilled_Fails()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, customer, product) = Seed(context);
        var service = new CustomerOrderService(context);
        var cart = new List<CartItemDto> { new() { ProductId = product.Id, ProductName = product.Name, UnitPrice = product.Price, Quantity = 1 } };
        var (_, _, orderId) = await service.SubmitOrderAsync(customer.Id, branch.Id, cart, null);
        await service.FulfillAsync(orderId!.Value, "staff-1");

        var (succeeded, error) = await service.CancelAsync(orderId.Value);

        Assert.False(succeeded);
        Assert.Contains("pending", error);
    }
}
