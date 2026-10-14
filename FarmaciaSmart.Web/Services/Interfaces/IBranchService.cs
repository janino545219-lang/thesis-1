using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Branches;

namespace FarmaciaSmart.Web.Services.Interfaces;

public interface IBranchService
{
    Task<List<Branch>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Branch?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> CreateAsync(BranchViewModel model, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> UpdateAsync(BranchViewModel model, CancellationToken cancellationToken = default);
    Task<bool> SetActiveStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default);
}
