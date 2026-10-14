using FarmaciaSmart.Web.Models.ViewModels.Recommendations;

namespace FarmaciaSmart.Web.Services.Interfaces;

public interface IRankingService
{
    Task<List<(CandidatePair Candidate, decimal Score)>> RankAsync(
        int customerId, List<CandidatePair> candidates, CancellationToken cancellationToken = default);
}
