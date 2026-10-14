using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Employees;

namespace FarmaciaSmart.Web.Services.Interfaces;

public interface IEmployeeService
{
    Task<List<Employee>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> CreateAsync(EmployeeViewModel model, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> UpdateAsync(EmployeeViewModel model, CancellationToken cancellationToken = default);
}
