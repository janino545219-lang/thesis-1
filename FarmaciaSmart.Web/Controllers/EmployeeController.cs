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

[Authorize(Roles = $"{ApplicationRoles.Administrator},{ApplicationRoles.HrPayroll}")]
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
        if (!ModelState.IsValid)
        {
            model.Branches = await GetActiveBranchesAsync();
            return View(model);
        }

        var (succeeded, error) = await employeeService.CreateAsync(model);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error!);
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

        TempData["Success"] = "Employee created successfully.";
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

    private async Task<List<Branch>> GetActiveBranchesAsync() =>
        await context.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
}
