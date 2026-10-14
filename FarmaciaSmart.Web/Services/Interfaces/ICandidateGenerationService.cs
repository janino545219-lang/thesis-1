using FarmaciaSmart.Web.Models.ViewModels.Recommendations;

namespace FarmaciaSmart.Web.Services.Interfaces;

public interface ICandidateGenerationService
{
    Task<List<CandidatePair>> GenerateAsync(RecommendationContext context, CancellationToken cancellationToken = default);
}
