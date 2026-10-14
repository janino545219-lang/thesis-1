using Microsoft.AspNetCore.Http;
using FarmaciaSmart.Web.Services;

namespace FarmaciaSmart.Tests.Services;

public class AuditLogServiceTests
{
    [Fact]
    public async Task LogAsync_ThenGetRecentAsync_ReturnsNewestFirst()
    {
        using var context = TestDbContextFactory.Create();
        var service = new AuditLogService(context, new HttpContextAccessor());

        await service.LogAsync("FirstAction", "user-1");
        await service.LogAsync("SecondAction", "user-1");

        var result = await service.GetRecentAsync();

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("SecondAction", result.Items[0].Action);
        Assert.Equal("FirstAction", result.Items[1].Action);
    }

    [Fact]
    public async Task GetRecentAsync_RespectsPageSize()
    {
        using var context = TestDbContextFactory.Create();
        var service = new AuditLogService(context, new HttpContextAccessor());

        for (var i = 0; i < 5; i++)
        {
            await service.LogAsync($"Action{i}", "user-1");
        }

        var result = await service.GetRecentAsync(page: 1, pageSize: 3);

        Assert.Equal(3, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetRecentAsync_SecondPage_ReturnsRemainingItems()
    {
        using var context = TestDbContextFactory.Create();
        var service = new AuditLogService(context, new HttpContextAccessor());

        for (var i = 0; i < 5; i++)
        {
            await service.LogAsync($"Action{i}", "user-1");
        }

        var result = await service.GetRecentAsync(page: 2, pageSize: 3);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.Page);
    }
}
