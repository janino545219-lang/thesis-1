using FarmaciaSmart.Web.Models.ViewModels.Recommendations;

namespace FarmaciaSmart.Web.Services.Interfaces;

public interface IContextService
{
    Task<(RecommendationContext? Context, string? Error)> BuildContextAsync(
        int customerId, int categoryId, int? branchId, decimal? maxPrice = null, CancellationToken cancellationToken = default);
}
