using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Helpers;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Payrolls;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Controllers;

[Authorize(Roles = $"{ApplicationRoles.Administrator},{ApplicationRoles.HrPayroll}")]
public class PayrollController(
    IPayrollService payrollService,
    ApplicationDbContext context,
    IAuditLogService auditLogService,
    INotificationService notificationService,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        return View(await payrollService.GetAllAsync());
    }

    public async Task<IActionResult> Create()
    {
        return View(new CreatePayrollViewModel { Employees = await GetActiveEmployeesAsync() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePayrollViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Employees = await GetActiveEmployeesAsync();
            return View(model);
        }

        var userId = userManager.GetUserId(User)!;
        var (succeeded, error, payrollId) = await payrollService.CreateDraftAsync(model, userId);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error!);
            model.Employees = await GetActiveEmployeesAsync();
            return View(model);
        }

        await auditLogService.LogAsync(
            "PayrollCreated",
            userId,
            $"EmployeeId: {model.EmployeeId}, Period: {model.PeriodStart}-{model.PeriodEnd}");

        return RedirectToAction(nameof(Details), new { id = payrollId });
    }

    public async Task<IActionResult> Details(int id)
    {
        var payroll = await payrollService.GetByIdAsync(id);
        return payroll is null ? NotFound() : View(payroll);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(int payrollId, PayrollItemViewModel model)
    {
        var (succeeded, error) = await payrollService.AddItemAsync(payrollId, model);
        if (succeeded)
        {
            await auditLogService.LogAsync(
                "PayrollItemAdded",
                userManager.GetUserId(User),
                $"PayrollId: {payrollId}, {model.ItemType}: {model.Description} {model.Amount:F2}");
        }
        else
        {
            TempData["Error"] = error;
        }

        return RedirectToAction(nameof(Details), new { id = payrollId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveItem(int payrollItemId, int payrollId)
    {
        var (succeeded, error) = await payrollService.RemoveItemAsync(payrollItemId);
        if (!succeeded)
        {
            TempData["Error"] = error;
        }

        return RedirectToAction(nameof(Details), new { id = payrollId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Finalize(int id)
    {
        var userId = userManager.GetUserId(User)!;
        var (succeeded, error) = await payrollService.FinalizeAsync(id, userId);
        if (succeeded)
        {
            await auditLogService.LogAsync("PayrollFinalized", userId, $"PayrollId: {id}");

            var payroll = await payrollService.GetByIdAsync(id);
            if (payroll is not null)
            {
                await notificationService.NotifyAsync(
                    NotificationType.Payroll,
                    $"Payroll finalized: {payroll.Employee.FirstName} {payroll.Employee.LastName}",
                    $"Payroll for {payroll.Employee.FirstName} {payroll.Employee.LastName} " +
                    $"({payroll.PeriodStart:d}-{payroll.PeriodEnd:d}) finalized. Net pay: {payroll.NetPay:N2}.",
                    payroll.Employee.BranchId);
            }

            TempData["Success"] = "Payroll finalized.";
        }
        else
        {
            TempData["Error"] = error;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<List<Employee>> GetActiveEmployeesAsync() =>
        await context.Employees.Where(e => e.EmploymentStatus == EmploymentStatus.Active).OrderBy(e => e.LastName).ToListAsync();
}
