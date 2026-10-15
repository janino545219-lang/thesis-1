using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Helpers;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Employees;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Controllers;

[Authorize(Roles = $"{ApplicationRoles.AdminOrSuper},{ApplicationRoles.HrPayroll}")]
public class EmployeeController(
    IEmployeeService employeeService,
    ApplicationDbContext context,
    IAuditLogService auditLogService,
    INotificationService notificationService,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        return View(await employeeService.GetAllAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var employee = await employeeService.GetByIdAsync(id);
        return employee is null ? NotFound() : View(employee);
    }

    public async Task<IActionResult> Create()
    {
        return View(new EmployeeViewModel { Branches = await GetActiveBranchesAsync() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeViewModel model)
    {
        if (model.CreateLoginAccount)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "Work email address is required to create a system login account.");
            }
            else
            {
                var existingUser = await userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError(nameof(model.Email), "An account with this email address already exists.");
                }
            }

            if (string.IsNullOrWhiteSpace(model.LoginPassword))
            {
                ModelState.AddModelError(nameof(model.LoginPassword), "Password is required to create a system login account.");
            }
            else if (model.LoginPassword.Length < 6)
            {
                ModelState.AddModelError(nameof(model.LoginPassword), "Password must be at least 6 characters long.");
            }
            else if (model.LoginPassword != model.ConfirmLoginPassword)
            {
                ModelState.AddModelError(nameof(model.ConfirmLoginPassword), "Passwords do not match.");
            }

            var role = model.SystemRole ?? ApplicationRoles.Cashier;
            if (!ApplicationRoles.StaffRoles.Contains(role))
            {
                ModelState.AddModelError(nameof(model.SystemRole), "Please select a valid staff role.");
            }
        }

        if (!ModelState.IsValid)
        {
            model.Branches = await GetActiveBranchesAsync();
            return View(model);
        }

        if (model.CreateLoginAccount && !string.IsNullOrWhiteSpace(model.Email) && !string.IsNullOrWhiteSpace(model.LoginPassword))
        {
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

            var createResult = await userManager.CreateAsync(user, model.LoginPassword);
            if (!createResult.Succeeded)
            {
                foreach (var err in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                model.Branches = await GetActiveBranchesAsync();
                return View(model);
            }

            var role = model.SystemRole ?? ApplicationRoles.Cashier;
            var roleResult = await userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                foreach (var err in roleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                model.Branches = await GetActiveBranchesAsync();
                return View(model);
            }

            model.UserId = user.Id;
            await auditLogService.LogAsync("StaffUserProvisioned", userManager.GetUserId(User), $"Provisioned {user.Email} as {role}");
        }

        var (succeeded, createEmpError) = await employeeService.CreateAsync(model);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, createEmpError!);
            model.Branches = await GetActiveBranchesAsync();
            return View(model);
        }

        await auditLogService.LogAsync("EmployeeCreated", userManager.GetUserId(User), $"Name: {model.FirstName} {model.LastName}");

        var branchName = await context.Branches.Where(b => b.Id == model.BranchId).Select(b => b.Name).FirstOrDefaultAsync();
        await notificationService.NotifyAsync(
            NotificationType.Administrative,
            $"New employee: {model.FirstName} {model.LastName}",
            $"{model.FirstName} {model.LastName} ({model.Position}) was added at {branchName}.",
            model.BranchId);

        TempData["Success"] = model.CreateLoginAccount
            ? $"Employee {model.FirstName} {model.LastName} and their login account ({model.Email}) were created successfully!"
            : $"Employee {model.FirstName} {model.LastName} created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var employee = await employeeService.GetByIdAsync(id);
        if (employee is null)
        {
            return NotFound();
        }

        return View(new EmployeeViewModel
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Position = employee.Position,
            BranchId = employee.BranchId,
            EmploymentStatus = employee.EmploymentStatus,
            ContactNumber = employee.ContactNumber,
            Email = employee.Email,
            HireDate = employee.HireDate,
            MonthlySalary = employee.MonthlySalary,
            UserId = employee.UserId,
            CreateLoginAccount = false,
            Branches = await GetActiveBranchesAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EmployeeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Branches = await GetActiveBranchesAsync();
            return View(model);
        }

        var (succeeded, error) = await employeeService.UpdateAsync(model);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error!);
            model.Branches = await GetActiveBranchesAsync();
            return View(model);
        }

        await auditLogService.LogAsync("EmployeeUpdated", userManager.GetUserId(User), $"Id: {model.Id}");
        TempData["Success"] = "Employee updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ProvisionLogin(int id)
    {
        var employee = await employeeService.GetByIdAsync(id);
        if (employee is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrEmpty(employee.UserId))
        {
            TempData["Success"] = $"{employee.FullName} already has a linked login account.";
            return RedirectToAction(nameof(Index));
        }

        return View(new ProvisionLoginViewModel
        {
            EmployeeId = employee.Id,
            EmployeeName = employee.FullName,
            Email = employee.Email ?? string.Empty,
            Position = employee.Position,
            BranchId = employee.BranchId,
            BranchName = employee.Branch?.Name ?? "Main Branch",
            Role = InferRoleFromPosition(employee.Position)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProvisionLogin(ProvisionLoginViewModel model)
    {
        var employee = await context.Employees.Include(e => e.Branch).FirstOrDefaultAsync(e => e.Id == model.EmployeeId);
        if (employee is null)
        {
            return NotFound();
        }

        if (!ApplicationRoles.StaffRoles.Contains(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Please select a valid staff role.");
        }

        var existingUser = await userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email address already exists.");
        }

        if (!ModelState.IsValid)
        {
            model.EmployeeName = employee.FullName;
            model.BranchName = employee.Branch?.Name ?? "Main Branch";
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            PhoneNumber = employee.ContactNumber,
            BranchId = employee.BranchId,
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
            model.EmployeeName = employee.FullName;
            model.BranchName = employee.Branch?.Name ?? "Main Branch";
            return View(model);
        }

        await userManager.AddToRoleAsync(user, model.Role);
        employee.UserId = user.Id;
        if (string.IsNullOrEmpty(employee.Email))
        {
            employee.Email = model.Email;
        }
        await context.SaveChangesAsync();

        await auditLogService.LogAsync("StaffUserProvisioned", userManager.GetUserId(User), $"Provisioned {user.Email} ({model.Role}) for employee {employee.FullName}");
        TempData["Success"] = $"Login credentials for {employee.FullName} ({model.Email}) were created successfully!";
        return RedirectToAction(nameof(Index));
    }

    private static string InferRoleFromPosition(string? position)
    {
        if (string.IsNullOrWhiteSpace(position)) return ApplicationRoles.Cashier;
        var p = position.ToLowerInvariant();
        if (p.Contains("pharmacist")) return ApplicationRoles.Pharmacist;
        if (p.Contains("manager")) return ApplicationRoles.BranchManager;
        if (p.Contains("admin") || p.Contains("owner")) return ApplicationRoles.Administrator;
        if (p.Contains("inventory") || p.Contains("stock") || p.Contains("warehouse")) return ApplicationRoles.InventoryStaff;
        if (p.Contains("hr") || p.Contains("payroll")) return ApplicationRoles.HrPayroll;
        return ApplicationRoles.Cashier;
    }

    private async Task<List<Branch>> GetActiveBranchesAsync() =>
        await context.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
}
