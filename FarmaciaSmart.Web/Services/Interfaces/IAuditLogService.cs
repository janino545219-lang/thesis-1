using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels;

namespace FarmaciaSmart.Web.Services.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(string action, string? userId, string? details = null, CancellationToken cancellationToken = default);
    Task<PagedResult<AuditLog>> GetRecentAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
}
