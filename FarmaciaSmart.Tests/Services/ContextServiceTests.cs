using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Services;

namespace FarmaciaSmart.Tests.Services;

public class ContextServiceTests
{
    private static (Branch branch, Category category, Customer customer) Seed(ApplicationDbContext context)
    {
        var branch = new Branch { Name = "Main Branch", Code = "MAIN", Address = "123 St", IsActive = true };
        var category = new Category { Name = "Pain Relief", IsActive = true };
        context.Branches.Add(branch);
        context.Categories.Add(category);
        context.SaveChanges();

        var customer = new Customer { UserId = "user-1", PreferredBranchId = branch.Id, Status = CustomerStatus.Active };
        context.Customers.Add(customer);
        context.SaveChanges();

        return (branch, category, customer);
    }

    [Fact]
    public async Task BuildContextAsync_ExplicitMaxPrice_IsRememberedAsPreference()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, category, customer) = Seed(context);
        var service = new ContextService(context);

        var (ctx, error) = await service.BuildContextAsync(customer.Id, category.Id, branch.Id, maxPrice: 85.49m);

        Assert.NotNull(ctx);
        Assert.Null(error);
        Assert.Equal(85.49m, ctx!.MaxPrice);

        var preference = context.CustomerPreferences.Single(p => p.CustomerId == customer.Id);
        Assert.Equal(85.49m, preference.MaxBudget);
    }

    [Fact]
    public async Task BuildContextAsync_NoExplicitMaxPrice_RecallsRememberedPreference()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, category, customer) = Seed(context);
        var service = new ContextService(context);

        await service.BuildContextAsync(customer.Id, category.Id, branch.Id, maxPrice: 85.49m);
        var (secondCtx, _) = await service.BuildContextAsync(customer.Id, category.Id, branch.Id);

        Assert.Equal(85.49m, secondCtx!.MaxPrice);
    }

    [Fact]
    public async Task BuildContextAsync_NewExplicitMaxPrice_OverwritesPreviousPreference()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, category, customer) = Seed(context);
        var service = new ContextService(context);

        await service.BuildContextAsync(customer.Id, category.Id, branch.Id, maxPrice: 85.49m);
        await service.BuildContextAsync(customer.Id, category.Id, branch.Id, maxPrice: 40m);

        Assert.Single(context.CustomerPreferences);
        Assert.Equal(40m, context.CustomerPreferences.Single().MaxBudget);
    }

    [Fact]
    public async Task BuildContextAsync_NoPreferenceEverStated_MaxPriceStaysNull()
    {
        using var context = TestDbContextFactory.Create();
        var (branch, category, customer) = Seed(context);
        var service = new ContextService(context);

        var (ctx, _) = await service.BuildContextAsync(customer.Id, category.Id, branch.Id);

        Assert.Null(ctx!.MaxPrice);
    }
}
