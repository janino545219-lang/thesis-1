using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Areas.CustomerPortal.Models.ViewModels;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Helpers;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Account;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Areas.CustomerPortal.Controllers;

[Area("CustomerPortal")]
[AllowAnonymous]
public class AccountController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext context,
    IAuditLogService auditLogService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Register()
    {
        return View(new CustomerRegisterViewModel
        {
            Branches = await context.Branches.Where(b => b.IsActive).ToListAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(CustomerRegisterViewModel model)
    {
        if (!model.AgreeToTerms)
        {
            ModelState.AddModelError(nameof(model.AgreeToTerms), "You must agree to the Terms and Privacy Policy.");
        }

        if (!ModelState.IsValid)
        {
            model.Branches = await context.Branches.Where(b => b.IsActive).ToListAsync();
            return View(model);
        }

        await using var transaction = await context.Database.BeginTransactionAsync();

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            PhoneNumber = model.ContactNumber,
            IsActive = true
        };

        var createResult = await userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.Branches = await context.Branches.Where(b => b.IsActive).ToListAsync();
            return View(model);
        }

        await userManager.AddToRoleAsync(user, ApplicationRoles.Customer);

        context.Customers.Add(new Customer
        {
            UserId = user.Id,
            PreferredBranchId = model.PreferredBranchId,
            ContactNumber = model.ContactNumber
        });

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        await auditLogService.LogAsync("CustomerRegistered", user.Id);

        await signInManager.SignInAsync(user, isPersistent: false);

        TempData["Success"] = "Your account has been registered successfully! Welcome to Farmacia San Antonio.";
        return RedirectToLocalOrHome(null);
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
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
            if (!roles.Contains(ApplicationRoles.Customer))
            {
                await signInManager.SignOutAsync();
                await auditLogService.LogAsync("CustomerLoginDeniedNonCustomer", user.Id);
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            await auditLogService.LogAsync("CustomerLogin", user.Id);
            return RedirectToLocalOrHome(model.ReturnUrl);
        }

        await auditLogService.LogAsync("CustomerLoginFailed", user.Id, $"Email: {model.Email}");

        ModelState.AddModelError(string.Empty, result.IsLockedOut
            ? "This account has been locked out. Try again later."
            : "Invalid login attempt.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = userManager.GetUserId(User);
        await signInManager.SignOutAsync();
        await auditLogService.LogAsync("CustomerLogout", userId);
        return RedirectToAction("Index", "Home", new { area = "" });
    }

    private IActionResult RedirectToLocalOrHome(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard", new { area = "CustomerPortal" });
    }
}
