using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Attendances;

namespace FarmaciaSmart.Web.Services.Interfaces;

public interface IAttendanceService
{
    Task<List<Attendance>> GetForDateAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<List<Attendance>> GetHistoryAsync(int? employeeId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> ClockInAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> ClockOutAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<Attendance?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> CorrectAsync(AttendanceCorrectionViewModel model, string correctedByUserId, CancellationToken cancellationToken = default);
}
