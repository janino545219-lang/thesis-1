using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Payrolls;

namespace FarmaciaSmart.Web.Services.Interfaces;

public interface IPayrollService
{
    Task<List<Payroll>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Payroll?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error, int? PayrollId)> CreateDraftAsync(CreatePayrollViewModel model, string processedByUserId, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> AddItemAsync(int payrollId, PayrollItemViewModel model, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> RemoveItemAsync(int payrollItemId, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> FinalizeAsync(int payrollId, string processedByUserId, CancellationToken cancellationToken = default);
}
