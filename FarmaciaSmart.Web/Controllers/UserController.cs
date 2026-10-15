using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Helpers;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Users;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Controllers;

[Authorize(Roles = ApplicationRoles.AdminOrSuper)]
public class UserController(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    ApplicationDbContext context,
    IAuditLogService auditLogService) : Controller
{
    public async Task<IActionResult> Index(string? search, string? roleFilter)
    {
        var usersQuery = context.Users.Include(u => u.Branch).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            usersQuery = usersQuery.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(s)) ||
                u.FirstName.ToLower().Contains(s) ||
                u.LastName.ToLower().Contains(s));
        }

        var usersList = await usersQuery.OrderByDescending(u => u.CreatedAt).ToListAsync();
        var userItemModels = new List<UserItemViewModel>();

        foreach (var user in usersList)
        {
            var roles = await userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? "No Role";

            if (!string.IsNullOrWhiteSpace(roleFilter) && !string.Equals(primaryRole, roleFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            userItemModels.Add(new UserItemViewModel
            {
                Id = user.Id,
                Email = user.Email ?? user.UserName ?? "N/A",
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                Role = primaryRole,
                BranchId = user.BranchId,
                BranchName = user.Branch?.Name,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            });
        }

        var viewModel = new UserIndexViewModel
        {
            Users = userItemModels,
            SearchTerm = search,
            SelectedRole = roleFilter
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var branches = await context.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
        return View(new CreateUserViewModel
        {
            Branches = branches,
            Role = ApplicationRoles.Cashier
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (!ApplicationRoles.StaffRoles.Contains(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Please select a valid role.");
        }

        var existingUser = await userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email address already exists.");
        }

        if (!ModelState.IsValid)
        {
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
            PhoneNumber = model.PhoneNumber,
            BranchId = model.BranchId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
            {
                ModelState.AddModelError(string.Empty, err.Description);
            }
            model.Branches = await context.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
            return View(model);
        }

        await userManager.AddToRoleAsync(user, model.Role);
        await auditLogService.LogAsync("UserCreatedByAdmin", userManager.GetUserId(User), $"Created {user.Email} with role {model.Role}");

        TempData["Success"] = $"User {user.Email} was created successfully with role {model.Role}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await context.Users.Include(u => u.Branch).FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
        {
            return NotFound();
        }

        var roles = await userManager.GetRolesAsync(user);
        var currentRole = roles.FirstOrDefault() ?? ApplicationRoles.Cashier;
        var branches = await context.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();

        return View(new EditUserViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? user.UserName ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Role = currentRole,
            BranchId = user.BranchId,
            IsActive = user.IsActive,
            Branches = branches
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        var user = await userManager.FindByIdAsync(model.Id);
        if (user is null)
        {
            return NotFound();
        }

        if (!ApplicationRoles.StaffRoles.Contains(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Please select a valid role.");
        }

        if (!ModelState.IsValid)
        {
            model.Branches = await context.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
            return View(model);
        }

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.PhoneNumber = model.PhoneNumber;
        user.BranchId = model.BranchId;
        user.IsActive = model.IsActive;

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await userManager.ResetPasswordAsync(user, token, model.NewPassword);
            if (!resetResult.Succeeded)
            {
                foreach (var err in resetResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                model.Branches = await context.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
                return View(model);
            }
        }

        await userManager.UpdateAsync(user);

        // Update role if changed
        var currentRoles = await userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(model.Role))
        {
            await userManager.RemoveFromRolesAsync(user, currentRoles);
            await userManager.AddToRoleAsync(user, model.Role);
        }

        await auditLogService.LogAsync("UserUpdatedByAdmin", userManager.GetUserId(User), $"Updated {user.Email} (Role: {model.Role}, Active: {model.IsActive})");
        TempData["Success"] = $"User {user.Email} updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        // Prevent self-lockout
        if (user.Id == userManager.GetUserId(User))
        {
            TempData["Success"] = "You cannot deactivate your own active account.";
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = !user.IsActive;
        await userManager.UpdateAsync(user);

        var statusText = user.IsActive ? "activated" : "deactivated";
        await auditLogService.LogAsync("UserStatusToggled", userManager.GetUserId(User), $"Account for {user.Email} was {statusText}");

        TempData["Success"] = $"Account for {user.Email} has been {statusText}.";
        return RedirectToAction(nameof(Index));
    }
}
