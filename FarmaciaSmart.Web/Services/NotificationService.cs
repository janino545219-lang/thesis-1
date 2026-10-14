using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

public class NotificationService(ApplicationDbContext context) : INotificationService
{
    public async Task<PagedResult<Notification>> GetRecentAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        var query = context.Notifications.Include(n => n.Branch).OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<Notification> { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default) =>
        await context.Notifications.CountAsync(n => !n.IsRead, cancellationToken);

    public async Task MarkAsReadAsync(int id, CancellationToken cancellationToken = default)
    {
        var notification = await context.Notifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
        if (notification is not null)
        {
            notification.IsRead = true;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GenerateSystemNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var created = 0;
        var dedupeCutoff = DateTime.UtcNow.AddHours(-24);

        var lowStockRows = await context.Inventories
            .Include(i => i.Product)
            .Include(i => i.Branch)
            .Where(i => i.QuantityOnHand <= i.Product.LowStockThreshold)
            .ToListAsync(cancellationToken);

        foreach (var row in lowStockRows)
        {
            var title = $"Low stock: {row.Product.Name} at {row.Branch.Name}";

            var recentlyNotified = await context.Notifications.AnyAsync(
                n => n.Type == NotificationType.LowStock && n.BranchId == row.BranchId && n.Title == title && n.CreatedAt >= dedupeCutoff,
                cancellationToken);

            if (recentlyNotified)
            {
                continue;
            }

            context.Notifications.Add(new Notification
            {
                Type = NotificationType.LowStock,
                Title = title,
                Message = $"{row.Product.Name} at {row.Branch.Name} is at {row.QuantityOnHand} unit(s), " +
                          $"at or below the threshold of {row.Product.LowStockThreshold}.",
                BranchId = row.BranchId
            });
            created++;
        }

        var batches = await context.InventoryBatches
            .Include(b => b.Product)
            .Include(b => b.Branch)
            .Where(b => b.QuantityRemaining > 0 && b.ExpirationDate != null && b.ExpirationDate <= DateTime.UtcNow.Date.AddDays(30))
            .ToListAsync(cancellationToken);

        foreach (var batch in batches)
        {
            var isExpired = batch.ExpirationDate!.Value.Date < DateTime.UtcNow.Date;
            var type = isExpired ? NotificationType.Expired : NotificationType.ExpiringSoon;
            var title = $"{(isExpired ? "Expired" : "Expiring soon")}: {batch.Product.Name} ({batch.BatchNumber}) at {batch.Branch.Name}";

            var recentlyNotified = await context.Notifications.AnyAsync(
                n => n.Type == type && n.BranchId == batch.BranchId && n.Title == title && n.CreatedAt >= dedupeCutoff,
                cancellationToken);

            if (recentlyNotified)
            {
                continue;
            }

            context.Notifications.Add(new Notification
            {
                Type = type,
                Title = title,
                Message = isExpired
                    ? $"Batch {batch.BatchNumber} of {batch.Product.Name} at {batch.Branch.Name} expired on " +
                      $"{batch.ExpirationDate:d} with {batch.QuantityRemaining} unit(s) remaining."
                    : $"Batch {batch.BatchNumber} of {batch.Product.Name} at {batch.Branch.Name} expires on " +
                      $"{batch.ExpirationDate:d} with {batch.QuantityRemaining} unit(s) remaining.",
                BranchId = batch.BranchId
            });
            created++;
        }

        if (created > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return created;
    }

    public async Task NotifyAsync(NotificationType type, string title, string message, int? branchId = null, CancellationToken cancellationToken = default)
    {
        context.Notifications.Add(new Notification
        {
            Type = type,
            Title = title,
            Message = message,
            BranchId = branchId
        });

        await context.SaveChangesAsync(cancellationToken);
    }
}
