using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FarmaciaSmart.Web.Helpers;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Administrator)]
public class AuditLogController(IAuditLogService auditLogService) : Controller
{
    public async Task<IActionResult> Index(int page = 1)
    {
        return View(await auditLogService.GetRecentAsync(page));
    }
}
