using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Services;

namespace FarmaciaSmart.Tests.Services;

public class NotificationServiceTests
{
    [Fact]
    public async Task NotifyAsync_CreatesUnreadNotificationOfGivenType()
    {
        using var context = TestDbContextFactory.Create();
        var branch = new Branch { Name = "Main Branch", Code = "MAIN", Address = "123 St", IsActive = true };
        context.Branches.Add(branch);
        context.SaveChanges();

        var service = new NotificationService(context);

        await service.NotifyAsync(NotificationType.DamagedItem, "Damaged item: Biogesic", "5 units damaged", branch.Id);

        var notification = context.Notifications.Single();
        Assert.Equal(NotificationType.DamagedItem, notification.Type);
        Assert.False(notification.IsRead);
        Assert.Equal(branch.Id, notification.BranchId);
    }

    [Fact]
    public async Task NotifyAsync_CalledTwice_DoesNotDeduplicate()
    {
        using var context = TestDbContextFactory.Create();
        var service = new NotificationService(context);

        await service.NotifyAsync(NotificationType.Payroll, "Payroll finalized", "Same message");
        await service.NotifyAsync(NotificationType.Payroll, "Payroll finalized", "Same message");

        Assert.Equal(2, context.Notifications.Count());
    }
}
