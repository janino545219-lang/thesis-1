using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Products;

namespace FarmaciaSmart.Web.Services.Interfaces;

public interface IProductService
{
    Task<List<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> CreateAsync(ProductViewModel model, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> UpdateAsync(ProductViewModel model, CancellationToken cancellationToken = default);
    Task<bool> SetActiveStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default);
}
