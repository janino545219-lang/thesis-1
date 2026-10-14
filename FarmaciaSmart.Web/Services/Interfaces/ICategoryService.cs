using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Categories;

namespace FarmaciaSmart.Web.Services.Interfaces;

public interface ICategoryService
{
    Task<List<Category>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> CreateAsync(CategoryViewModel model, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> UpdateAsync(CategoryViewModel model, CancellationToken cancellationToken = default);
    Task<bool> SetActiveStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default);
}
