using FarmaciaSmart.Web.Models.ViewModels.Categories;
using FarmaciaSmart.Web.Services;

namespace FarmaciaSmart.Tests.Services;

public class CategoryServiceTests
{
    [Fact]
    public async Task CreateAsync_WithNewName_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var service = new CategoryService(context);

        var (succeeded, error) = await service.CreateAsync(new CategoryViewModel { Name = "Pain Relief" });

        Assert.True(succeeded);
        Assert.Null(error);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_Fails()
    {
        using var context = TestDbContextFactory.Create();
        var service = new CategoryService(context);

        await service.CreateAsync(new CategoryViewModel { Name = "Pain Relief" });
        var (succeeded, error) = await service.CreateAsync(new CategoryViewModel { Name = "Pain Relief" });

        Assert.False(succeeded);
        Assert.Contains("already exists", error);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentId_Fails()
    {
        using var context = TestDbContextFactory.Create();
        var service = new CategoryService(context);

        var (succeeded, error) = await service.UpdateAsync(new CategoryViewModel { Id = 999, Name = "Ghost Category" });

        Assert.False(succeeded);
        Assert.Equal("Category not found.", error);
    }
}
