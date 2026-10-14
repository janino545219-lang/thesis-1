using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Recommendations;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

/// <summary>
/// Deterministic, transparent scoring (spec Section 16). Personalization (Stage 11, spec
/// Section 23) is an explicit deterministic adjustment from the customer's own past feedback --
/// not machine learning, per the spec's own instruction not to claim the system "learns"
/// unless an actual trained model exists.
/// </summary>
public class RankingService(ApplicationDbContext context) : IRankingService
{
    public async Task<List<(CandidatePair Candidate, decimal Score)>> RankAsync(
        int customerId, List<CandidatePair> candidates, CancellationToken cancellationToken = default)
    {
        var productIds = candidates.Select(c => c.Product.Id).ToList();

        var pastFeedback = await context.RecommendationFeedbacks
            .Where(f => f.CustomerId == customerId && productIds.Contains(f.RecommendationItem.ProductId))
            .Select(f => new { f.RecommendationItem.ProductId, f.Rating })
            .ToListAsync(cancellationToken);

        var adjustmentByProduct = pastFeedback
            .GroupBy(f => f.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(f => f.Rating == FeedbackRating.Helpful ? 3m : -3m));

        return candidates
            .Select(c => (Candidate: c, Score: ComputeScore(c) + adjustmentByProduct.GetValueOrDefault(c.Product.Id, 0m)))
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Candidate.Product.Name)
            .ToList();
    }

    private static decimal ComputeScore(CandidatePair candidate)
    {
        decimal score = 0;

        if (candidate.Rule is not null)
        {
            // A rule that names a specific product reflects more deliberate pharmacist
            // curation than a category-wide rule matching any eligible product.
            score += candidate.Rule.ProductId.HasValue ? 10 : 5;
        }

        // Semantic similarity contributes on a 0-5 scale, letting text similarity nudge
        // ranking without ever outweighing a pharmacist-curated rule match.
        score += (decimal)(candidate.SimilarityScore * 5);

        return score;
    }
}
