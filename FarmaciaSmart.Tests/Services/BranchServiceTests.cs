using FarmaciaSmart.Web.Models.ViewModels.Branches;
using FarmaciaSmart.Web.Services;

namespace FarmaciaSmart.Tests.Services;

public class BranchServiceTests
{
    [Fact]
    public async Task CreateAsync_WithNewName_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var service = new BranchService(context);

        var (succeeded, error) = await service.CreateAsync(new BranchViewModel
        {
            Name = "Main Branch",
            Code = "MAIN",
            Address = "123 Rizal St"
        });

        Assert.True(succeeded);
        Assert.Null(error);
        Assert.Single(context.Branches);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateCode_Fails()
    {
        using var context = TestDbContextFactory.Create();
        var service = new BranchService(context);

        await service.CreateAsync(new BranchViewModel { Name = "Main Branch", Code = "MAIN", Address = "123 Rizal St" });
        var (succeeded, error) = await service.CreateAsync(new BranchViewModel { Name = "Other Branch", Code = "MAIN", Address = "456 Other St" });

        Assert.False(succeeded);
        Assert.Contains("already exists", error);
        Assert.Single(context.Branches);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_Fails()
    {
        using var context = TestDbContextFactory.Create();
        var service = new BranchService(context);

        await service.CreateAsync(new BranchViewModel { Name = "Main Branch", Code = "MAIN", Address = "123 Rizal St" });
        var (succeeded, error) = await service.CreateAsync(new BranchViewModel { Name = "Main Branch", Code = "BR2", Address = "456 Other St" });

        Assert.False(succeeded);
        Assert.Contains("already exists", error);
    }

    [Fact]
    public async Task UpdateAsync_AllowsKeepingOwnNameAndCode()
    {
        using var context = TestDbContextFactory.Create();
        var service = new BranchService(context);

        await service.CreateAsync(new BranchViewModel { Name = "Main Branch", Code = "MAIN", Address = "123 Rizal St" });
        var branch = context.Branches.Single();

        var (succeeded, error) = await service.UpdateAsync(new BranchViewModel
        {
            Id = branch.Id,
            Name = "Main Branch",
            Code = "MAIN",
            Address = "New Address"
        });

        Assert.True(succeeded);
        Assert.Null(error);
    }

    [Fact]
    public async Task SetActiveStatusAsync_TogglesIsActive()
    {
        using var context = TestDbContextFactory.Create();
        var service = new BranchService(context);

        await service.CreateAsync(new BranchViewModel { Name = "Main Branch", Code = "MAIN", Address = "123 Rizal St" });
        var branch = context.Branches.Single();
        Assert.True(branch.IsActive);

        var succeeded = await service.SetActiveStatusAsync(branch.Id, false);

        Assert.True(succeeded);
        Assert.False(context.Branches.Single().IsActive);
    }
}
