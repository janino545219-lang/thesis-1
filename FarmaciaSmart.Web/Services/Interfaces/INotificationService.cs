using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels;

namespace FarmaciaSmart.Web.Services.Interfaces;

public interface INotificationService
{
    Task<PagedResult<Notification>> GetRecentAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Scans current system state (stock, expiring/expired batches) and creates
    /// notifications for newly-detected conditions. Deduplicated against the last 24h so
    /// repeated scans of an unresolved condition don't spam. Returns the count created.</summary>
    Task<int> GenerateSystemNotificationsAsync(CancellationToken cancellationToken = default);

    /// <summary>One-shot event notification (damaged item reported, return processed, payroll
    /// finalized, administrative event) -- unlike GenerateSystemNotificationsAsync's state scan,
    /// this fires once per real event with no dedup, called by a controller right after the
    /// triggering action succeeds (same convention as IAuditLogService.LogAsync).</summary>
    Task NotifyAsync(NotificationType type, string title, string message, int? branchId = null, CancellationToken cancellationToken = default);
}
