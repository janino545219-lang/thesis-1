using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Areas.CustomerPortal.Models.ViewModels;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Areas.CustomerPortal.Controllers;

public class ProfileController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IAuditLogService auditLogService) : CustomerPortalControllerBase
{
    public async Task<IActionResult> Index()
    {
        var userId = userManager.GetUserId(User)!;
        var customer = await context.Customers
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (customer is null)
        {
            return NotFound();
        }

        return View(new CustomerProfileViewModel
        {
            FirstName = customer.User.FirstName,
            LastName = customer.User.LastName,
            Email = customer.User.Email!,
            ContactNumber = customer.ContactNumber,
            PreferredBranchId = customer.PreferredBranchId,
            Branches = await context.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CustomerProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Branches = await context.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
            return View(model);
        }

        var userId = userManager.GetUserId(User)!;
        var user = await userManager.FindByIdAsync(userId);
        var customer = await context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

        if (user is null || customer is null)
        {
            return NotFound();
        }

        if (model.PreferredBranchId.HasValue &&
            !await context.Branches.AnyAsync(b => b.Id == model.PreferredBranchId && b.IsActive))
        {
            ModelState.AddModelError(string.Empty, "Selected branch is not valid.");
            model.Branches = await context.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
            return View(model);
        }

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        await userManager.UpdateAsync(user);

        customer.ContactNumber = model.ContactNumber;
        customer.PreferredBranchId = model.PreferredBranchId;
        customer.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        await auditLogService.LogAsync("CustomerProfileUpdated", userId);

        TempData["Success"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = userManager.GetUserId(User)!;
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await signInManager.RefreshSignInAsync(user);
        await auditLogService.LogAsync("CustomerPasswordChanged", userId);

        TempData["Success"] = "Password changed successfully.";
        return RedirectToAction(nameof(Index));
    }
}
