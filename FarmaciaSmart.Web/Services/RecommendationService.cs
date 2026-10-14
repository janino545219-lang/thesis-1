using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

/// <summary>
/// Orchestrates the RETRIEVE -> FILTER -> RANK -> EXPLAIN pipeline (spec Section 49).
/// REVIEW happens separately via ReviewAsync once a pharmacist acts on the result.
/// FEEDBACK/IMPROVE (Stage 11) live in IFeedbackService and feed RankAsync's deterministic
/// personalization adjustment.
/// </summary>
public class RecommendationService(
    ApplicationDbContext context,
    IContextService contextService,
    ICandidateGenerationService candidateGenerationService,
    IRankingService rankingService,
    IExplanationService explanationService) : IRecommendationService
{
    public async Task<(bool Succeeded, string? Error, int? RecommendationId)> GenerateRecommendationAsync(
        int customerId, int categoryId, int? branchId, decimal? maxPrice = null, CancellationToken cancellationToken = default)
    {
        var (ctx, ctxError) = await contextService.BuildContextAsync(customerId, categoryId, branchId, maxPrice, cancellationToken);
        if (ctx is null)
        {
            return (false, ctxError, null);
        }

        var candidates = await candidateGenerationService.GenerateAsync(ctx, cancellationToken);
        if (candidates.Count == 0)
        {
            return (false, "No recommendations are available for this concern right now. Please consult the pharmacist directly.", null);
        }

        var ranked = await rankingService.RankAsync(customerId, candidates, cancellationToken);
        var category = await context.Categories.FirstAsync(c => c.Id == categoryId, cancellationToken);

        var requestText = maxPrice.HasValue
            ? $"Looking for help with: {category.Name} (budget: up to {maxPrice.Value:N2})"
            : $"Looking for help with: {category.Name}";

        var recommendation = new Recommendation
        {
            CustomerId = customerId,
            CategoryId = categoryId,
            BranchId = ctx.BranchId,
            RequestText = requestText
        };

        context.Recommendations.Add(recommendation);
        await context.SaveChangesAsync(cancellationToken);

        var rank = 1;
        foreach (var (candidate, score) in ranked.Take(5))
        {
            var (explanation, warning) = explanationService.Explain(candidate);

            context.RecommendationItems.Add(new RecommendationItem
            {
                RecommendationId = recommendation.Id,
                ProductId = candidate.Product.Id,
                RecommendationRuleId = candidate.Rule?.Id,
                Rank = rank++,
                Score = score,
                Explanation = explanation,
                Warning = warning
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        return (true, null, recommendation.Id);
    }

    public async Task<Recommendation?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Recommendations
            .Include(r => r.Category)
            .Include(r => r.Branch)
            .Include(r => r.Customer).ThenInclude(c => c.User)
            .Include(r => r.ReviewedByUser)
            .Include(r => r.Items).ThenInclude(i => i.Product)
            .Include(r => r.Items).ThenInclude(i => i.RecommendationRule)
            .Include(r => r.Items).ThenInclude(i => i.Feedback)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<List<Recommendation>> GetPendingReviewAsync(CancellationToken cancellationToken = default) =>
        await context.Recommendations
            .Include(r => r.Category)
            .Include(r => r.Customer).ThenInclude(c => c.User)
            .Where(r => r.ReviewStatus == RecommendationReviewStatus.PendingReview)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<(bool Succeeded, string? Error)> ReviewAsync(
        int recommendationId, RecommendationReviewStatus status, string reviewedByUserId, string? notes, CancellationToken cancellationToken = default)
    {
        var recommendation = await context.Recommendations.FirstOrDefaultAsync(r => r.Id == recommendationId, cancellationToken);
        if (recommendation is null)
        {
            return (false, "Recommendation not found.");
        }

        if (status == RecommendationReviewStatus.PendingReview)
        {
            return (false, "Invalid review status.");
        }

        recommendation.ReviewStatus = status;
        recommendation.ReviewedByUserId = reviewedByUserId;
        recommendation.ReviewedAt = DateTime.UtcNow;
        recommendation.ReviewNotes = notes;

        await context.SaveChangesAsync(cancellationToken);
        return (true, null);
    }
}
