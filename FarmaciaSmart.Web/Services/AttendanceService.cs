using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Attendances;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

public class AttendanceService(ApplicationDbContext context) : IAttendanceService
{
    public async Task<List<Attendance>> GetForDateAsync(DateOnly date, CancellationToken cancellationToken = default) =>
        await context.Attendances
            .Include(a => a.Employee)
            .Where(a => a.AttendanceDate == date)
            .OrderBy(a => a.Employee.LastName)
            .ToListAsync(cancellationToken);

    public async Task<List<Attendance>> GetHistoryAsync(int? employeeId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var query = context.Attendances.Include(a => a.Employee).AsQueryable();

        if (employeeId.HasValue)
        {
            query = query.Where(a => a.EmployeeId == employeeId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(a => a.AttendanceDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(a => a.AttendanceDate <= to.Value);
        }

        return await query
            .OrderByDescending(a => a.AttendanceDate)
            .ThenBy(a => a.Employee.LastName)
            .Take(200)
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Succeeded, string? Error)> ClockInAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await context.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId && e.EmploymentStatus == EmploymentStatus.Active, cancellationToken);

        if (employee is null)
        {
            return (false, "Employee not found or not active.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var record = await context.Attendances
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.AttendanceDate == today, cancellationToken);

        if (record is not null && record.TimeIn.HasValue)
        {
            return (false, $"{employee.FirstName} {employee.LastName} already clocked in today.");
        }

        if (record is null)
        {
            record = new Attendance { EmployeeId = employeeId, AttendanceDate = today };
            context.Attendances.Add(record);
        }

        record.TimeIn = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> ClockOutAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var record = await context.Attendances
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.AttendanceDate == today, cancellationToken);

        if (record is null || !record.TimeIn.HasValue)
        {
            return (false, "Employee has not clocked in today.");
        }

        if (record.TimeOut.HasValue)
        {
            return (false, $"{record.Employee.FirstName} {record.Employee.LastName} already clocked out today.");
        }

        record.TimeOut = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<Attendance?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Attendances.Include(a => a.Employee).FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<(bool Succeeded, string? Error)> CorrectAsync(AttendanceCorrectionViewModel model, string correctedByUserId, CancellationToken cancellationToken = default)
    {
        var record = await context.Attendances.FirstOrDefaultAsync(a => a.Id == model.Id, cancellationToken);
        if (record is null)
        {
            return (false, "Attendance record not found.");
        }

        if (model.TimeOut.HasValue && model.TimeIn.HasValue && model.TimeOut < model.TimeIn)
        {
            return (false, "Time out cannot be earlier than time in.");
        }

        record.TimeIn = model.TimeIn;
        record.TimeOut = model.TimeOut;
        record.IsCorrected = true;
        record.CorrectionReason = model.CorrectionReason;
        record.CorrectedByUserId = correctedByUserId;

        await context.SaveChangesAsync(cancellationToken);
        return (true, null);
    }
}
