using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Helpers;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Attendances;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Controllers;

[Authorize(Roles = $"{ApplicationRoles.Administrator},{ApplicationRoles.BranchManager},{ApplicationRoles.HrPayroll}")]
public class AttendanceController(
    IAttendanceService attendanceService,
    ApplicationDbContext context,
    IAuditLogService auditLogService,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        ViewBag.Employees = await context.Employees
            .Where(e => e.EmploymentStatus == EmploymentStatus.Active)
            .OrderBy(e => e.LastName)
            .ToListAsync();

        return View(await attendanceService.GetForDateAsync(today));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClockIn(int employeeId)
    {
        var (succeeded, error) = await attendanceService.ClockInAsync(employeeId);
        if (succeeded)
        {
            await auditLogService.LogAsync("AttendanceClockIn", userManager.GetUserId(User), $"EmployeeId: {employeeId}");
        }
        else
        {
            TempData["Error"] = error;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClockOut(int employeeId)
    {
        var (succeeded, error) = await attendanceService.ClockOutAsync(employeeId);
        if (succeeded)
        {
            await auditLogService.LogAsync("AttendanceClockOut", userManager.GetUserId(User), $"EmployeeId: {employeeId}");
        }
        else
        {
            TempData["Error"] = error;
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> History(int? employeeId, DateOnly? from, DateOnly? to)
    {
        ViewBag.Employees = await context.Employees.OrderBy(e => e.LastName).ToListAsync();
        ViewBag.EmployeeId = employeeId;
        ViewBag.From = from;
        ViewBag.To = to;

        return View(await attendanceService.GetHistoryAsync(employeeId, from, to));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var record = await attendanceService.GetByIdAsync(id);
        if (record is null)
        {
            return NotFound();
        }

        return View(new AttendanceCorrectionViewModel
        {
            Id = record.Id,
            EmployeeId = record.EmployeeId,
            EmployeeName = $"{record.Employee.FirstName} {record.Employee.LastName}",
            AttendanceDate = record.AttendanceDate,
            TimeIn = record.TimeIn,
            TimeOut = record.TimeOut
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AttendanceCorrectionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = userManager.GetUserId(User)!;
        var (succeeded, error) = await attendanceService.CorrectAsync(model, userId);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error!);
            return View(model);
        }

        await auditLogService.LogAsync("AttendanceCorrected", userId, $"AttendanceId: {model.Id}, Reason: {model.CorrectionReason}");

        TempData["Success"] = "Attendance record corrected successfully.";
        return RedirectToAction(nameof(History));
    }
}
