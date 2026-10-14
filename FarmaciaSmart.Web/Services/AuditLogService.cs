using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

public class AuditLogService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor) : IAuditLogService
{
    public async Task LogAsync(string action, string? userId, string? details = null, CancellationToken cancellationToken = default)
    {
        var ipAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        context.AuditLogs.Add(new AuditLog
        {
            Action = action,
            UserId = userId,
            Details = details,
            IpAddress = ipAddress
        });

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<AuditLog>> GetRecentAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        var query = context.AuditLogs.Include(a => a.User).OrderByDescending(a => a.Timestamp);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<AuditLog> { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
    }
}
