using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Services.Interfaces;

public interface ICustomerService
{
    Task<List<Customer>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> SetStatusAsync(int id, CustomerStatus status, CancellationToken cancellationToken = default);
}
