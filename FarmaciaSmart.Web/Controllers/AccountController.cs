using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Helpers;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Account;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Controllers;

public class AccountController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext context,
    IAuditLogService auditLogService) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var roles = await userManager.GetRolesAsync(user);
            if (!roles.Any(ApplicationRoles.StaffRoles.Contains))
            {
                await signInManager.SignOutAsync();
                await auditLogService.LogAsync("StaffLoginDeniedNonStaff", user.Id);
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            await auditLogService.LogAsync("StaffLogin", user.Id);
            return RedirectToLocalOrHome(model.ReturnUrl);
        }

        await auditLogService.LogAsync("StaffLoginFailed", user.Id, $"Email: {model.Email}");

        ModelState.AddModelError(string.Empty, result.IsLockedOut
            ? "This account has been locked out. Try again later."
            : "Invalid login attempt.");
        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = ApplicationRoles.AdminOrSuper)]
    public async Task<IActionResult> Register()
    {
        var branches = await context.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
        return View(new StaffRegisterViewModel
        {
            Branches = branches,
            Role = ApplicationRoles.Cashier
        });
    }

    [HttpPost]
    [Authorize(Roles = ApplicationRoles.AdminOrSuper)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(StaffRegisterViewModel model)
    {
        if (!ApplicationRoles.StaffRoles.Contains(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Please select a valid staff role.");
        }

        if (model.BranchId.HasValue)
        {
            var branchValid = await context.Branches.AnyAsync(b => b.Id == model.BranchId.Value && b.IsActive);
            if (!branchValid)
            {
                ModelState.AddModelError(nameof(model.BranchId), "Selected branch does not exist.");
            }
        }

        if (!ModelState.IsValid)
        {
            model.Branches = await context.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
            return View(model);
        }

        var existingUser = await userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email address already exists.");
            model.Branches = await context.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            FirstName = model.FirstName,
            LastName = model.LastName,
            PhoneNumber = model.ContactNumber,
            BranchId = model.BranchId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            model.Branches = await context.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
            return View(model);
        }

        var roleResult = await userManager.AddToRoleAsync(user, model.Role);
        if (!roleResult.Succeeded)
        {
            foreach (var error in roleResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            model.Branches = await context.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
            return View(model);
        }

        await auditLogService.LogAsync("StaffRegistered", user.Id, $"Role: {model.Role}, BranchId: {model.BranchId}");

        TempData["Success"] = $"Staff account for {user.FirstName} {user.LastName} ({model.Role}) was created successfully.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = userManager.GetUserId(User);
        await signInManager.SignOutAsync();
        await auditLogService.LogAsync("StaffLogout", userId);
        return RedirectToAction("Index", "Home");
    }

    private IActionResult RedirectToLocalOrHome(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
