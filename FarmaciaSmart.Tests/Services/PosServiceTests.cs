using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Sales;
using FarmaciaSmart.Web.Services;

namespace FarmaciaSmart.Tests.Services;

public class PosServiceTests
{
    private static (Branch branch, Product product) Seed(ApplicationDbContext context, int quantityOnHand = 50)
    {
        var branch = new Branch { Name = "Main Branch", Code = "MAIN", Address = "123 St", IsActive = true };
        var category = new Category { Name = "Pain Relief", IsActive = true };
        context.Branches.Add(branch);
        context.Categories.Add(category);
        context.SaveChanges();

        var product = new Product { Name = "Biogesic 500mg", CategoryId = category.Id, Price = 85.50m, IsActive = true };
        context.Products.Add(product);
        context.SaveChanges();

        context.Inventories.Add(new Inventory { ProductId = product.Id, BranchId = branch.Id, QuantityOnHand = quantityOnHand });
        context.SaveChanges();

        return (branch, product);
    }

    private static List<CartItemDto> Cart(Product product, int quantity) =>
        [new CartItemDto { ProductId = product.Id, ProductName = product.Name, UnitPrice = product.Price, Quantity = quantity }];

    [Fact]
    public async Task CheckoutAsync_SufficientStock_SucceedsAndDeductsInventory()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, product) = Seed(context, quantityOnHand: 50);
        var service = new PosService(context);

        var (succeeded, error, saleId) = await service.CheckoutAsync(
            Cart(product, 7),
            new CheckoutViewModel { PaymentMethod = PaymentMethod.Cash, AmountTendered = 1000 },
            branch.Id,
            "cashier-1");

        Assert.True(succeeded);
        Assert.Null(error);
        Assert.NotNull(saleId);
        Assert.Equal(43, context.Inventories.Single().QuantityOnHand);
        Assert.Single(context.Sales);
        Assert.Single(context.SaleItems);
        Assert.Single(context.Payments);
    }

    [Fact]
    public async Task CheckoutAsync_InsufficientStock_FailsWithoutChangingAnything()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, product) = Seed(context, quantityOnHand: 5);
        var service = new PosService(context);

        var (succeeded, error, saleId) = await service.CheckoutAsync(
            Cart(product, 999),
            new CheckoutViewModel { PaymentMethod = PaymentMethod.Cash, AmountTendered = 100000 },
            branch.Id,
            "cashier-1");

        Assert.False(succeeded);
        Assert.Contains("Insufficient stock", error);
        Assert.Null(saleId);
        Assert.Equal(5, context.Inventories.Single().QuantityOnHand);
        Assert.Empty(context.Sales);
    }

    [Fact]
    public async Task CheckoutAsync_AmountTenderedBelowTotal_Fails()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, product) = Seed(context);
        var service = new PosService(context);

        var (succeeded, error, _) = await service.CheckoutAsync(
            Cart(product, 7),
            new CheckoutViewModel { PaymentMethod = PaymentMethod.Cash, AmountTendered = 1 },
            branch.Id,
            "cashier-1");

        Assert.False(succeeded);
        Assert.Contains("less than the total", error);
    }

    [Fact]
    public async Task CheckoutAsync_WithSeniorPwdDiscount_ComputesCorrectTotals()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, product) = Seed(context);
        var service = new PosService(context);

        var model = new CheckoutViewModel { PaymentMethod = PaymentMethod.Cash, AmountTendered = 500, ApplySeniorPwdDiscount = true };
        var (succeeded, _, saleId) = await service.CheckoutAsync(Cart(product, 7), model, branch.Id, "cashier-1");

        Assert.True(succeeded);
        var sale = context.Sales.Single(s => s.Id == saleId);

        // 7 * 85.50 = 598.50 subtotal; 20% discount = 119.70; total = 478.80
        Assert.Equal(598.50m, sale.SubTotal);
        Assert.Equal(119.70m, sale.DiscountAmount);
        Assert.Equal(478.80m, sale.TotalAmount);
        Assert.Equal(21.20m, context.Payments.Single().ChangeAmount);
    }

    [Fact]
    public async Task CheckoutAsync_EmptyCart_Fails()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, _) = Seed(context);
        var service = new PosService(context);

        var (succeeded, error, _) = await service.CheckoutAsync(
            [], new CheckoutViewModel { PaymentMethod = PaymentMethod.Cash, AmountTendered = 100 }, branch.Id, "cashier-1");

        Assert.False(succeeded);
        Assert.Contains("empty", error);
    }
}
