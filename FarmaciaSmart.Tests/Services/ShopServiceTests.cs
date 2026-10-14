using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Services;

namespace FarmaciaSmart.Tests.Services;

public class ShopServiceTests
{
    private static Branch Seed(ApplicationDbContext context)
    {
        var branch = new Branch { Name = "Main Branch", Code = "MAIN", Address = "123 St", IsActive = true };
        var otherBranch = new Branch { Name = "Second Branch", Code = "BR2", Address = "456 St", IsActive = true };
        var category = new Category { Name = "Pain Relief", IsActive = true };
        context.Branches.AddRange(branch, otherBranch);
        context.Categories.Add(category);
        context.SaveChanges();

        var inStock = new Product { Name = "Biogesic 500mg", Brand = "Unilab", CategoryId = category.Id, Price = 85.50m, IsActive = true };
        var outOfStock = new Product { Name = "Alaxan FR", Brand = "Unilab", CategoryId = category.Id, Price = 12.00m, IsActive = true };
        var inactive = new Product { Name = "Discontinued Item", Brand = "Other", CategoryId = category.Id, Price = 5.00m, IsActive = false };
        context.Products.AddRange(inStock, outOfStock, inactive);
        context.SaveChanges();

        context.Inventories.AddRange(
            new Inventory { ProductId = inStock.Id, BranchId = branch.Id, QuantityOnHand = 30 },
            new Inventory { ProductId = outOfStock.Id, BranchId = branch.Id, QuantityOnHand = 0 },
            new Inventory { ProductId = inactive.Id, BranchId = branch.Id, QuantityOnHand = 10 },
            new Inventory { ProductId = inStock.Id, BranchId = otherBranch.Id, QuantityOnHand = 5 });
        context.SaveChanges();

        return branch;
    }

    [Fact]
    public async Task BrowseAsync_OnlyReturnsActiveInStockProductsAtBranch()
    {
        using var context = TestDbContextFactory.Create();
        var branch = Seed(context);
        var service = new ShopService(context);

        var result = await service.BrowseAsync(branch.Id, null, null, null, null, null);

        Assert.Single(result.Products);
        Assert.Equal("Biogesic 500mg", result.Products[0].Name);
    }

    [Fact]
    public async Task BrowseAsync_FiltersByPriceRange()
    {
        using var context = TestDbContextFactory.Create();
        var branch = Seed(context);
        var service = new ShopService(context);

        var result = await service.BrowseAsync(branch.Id, null, null, minPrice: 100m, maxPrice: null, brands: null);

        Assert.Empty(result.Products);
    }

    [Fact]
    public async Task BrowseAsync_FiltersBySearchTerm()
    {
        using var context = TestDbContextFactory.Create();
        var branch = Seed(context);
        var service = new ShopService(context);

        var result = await service.BrowseAsync(branch.Id, null, "biogesic", null, null, null);

        Assert.Single(result.Products);
    }

    [Fact]
    public async Task BrowseAsync_DifferentBranch_SeesDifferentStock()
    {
        using var context = TestDbContextFactory.Create();
        Seed(context);
        var otherBranch = context.Branches.Single(b => b.Code == "BR2");
        var service = new ShopService(context);

        var result = await service.BrowseAsync(otherBranch.Id, null, null, null, null, null);

        Assert.Single(result.Products);
        Assert.Equal(5, result.Products[0].QuantityAvailable);
    }
}
