using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Services.Interfaces;

public interface IRecommendationService
{
    Task<(bool Succeeded, string? Error, int? RecommendationId)> GenerateRecommendationAsync(
        int customerId, int categoryId, int? branchId, decimal? maxPrice = null, CancellationToken cancellationToken = default);

    Task<Recommendation?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Recommendation>> GetPendingReviewAsync(CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> ReviewAsync(
        int recommendationId, RecommendationReviewStatus status, string reviewedByUserId, string? notes, CancellationToken cancellationToken = default);
}
