using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Areas.CustomerPortal.Controllers;

public class DashboardController(
    ICustomerDashboardService dashboardService,
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager) : CustomerPortalControllerBase
{
    public async Task<IActionResult> Index()
    {
        var userId = userManager.GetUserId(User)!;
        var customer = await context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
        if (customer is null)
        {
            return NotFound();
        }

        var model = await dashboardService.GetDashboardAsync(customer.Id);
        return model is null ? NotFound() : View(model);
    }
}
