using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Suppliers;

namespace FarmaciaSmart.Web.Services.Interfaces;

public interface ISupplierService
{
    Task<List<Supplier>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Supplier?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> CreateAsync(SupplierViewModel model, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> UpdateAsync(SupplierViewModel model, CancellationToken cancellationToken = default);
    Task<bool> SetActiveStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default);
}
