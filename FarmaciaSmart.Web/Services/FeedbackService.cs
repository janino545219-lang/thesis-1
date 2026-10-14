using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

public class FeedbackService(ApplicationDbContext context) : IFeedbackService
{
    public async Task<(bool Succeeded, string? Error)> SubmitFeedbackAsync(
        int customerId, int recommendationItemId, FeedbackRating rating, string? comment, CancellationToken cancellationToken = default)
    {
        var item = await context.RecommendationItems
            .Include(i => i.Recommendation)
            .FirstOrDefaultAsync(i => i.Id == recommendationItemId, cancellationToken);

        if (item is null || item.Recommendation.CustomerId != customerId)
        {
            return (false, "Recommendation item not found.");
        }

        var existing = await context.RecommendationFeedbacks
            .FirstOrDefaultAsync(f => f.RecommendationItemId == recommendationItemId, cancellationToken);

        if (existing is null)
        {
            context.RecommendationFeedbacks.Add(new RecommendationFeedback
            {
                RecommendationItemId = recommendationItemId,
                CustomerId = customerId,
                Rating = rating,
                Comment = comment
            });
        }
        else
        {
            existing.Rating = rating;
            existing.Comment = comment;
        }

        await context.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task LogInteractionAsync(
        int customerId, int recommendationId, InteractionType type, int? productId = null, CancellationToken cancellationToken = default)
    {
        var alreadyLogged = await context.RecommendationInteractions.AnyAsync(
            i => i.RecommendationId == recommendationId && i.InteractionType == type && i.ProductId == productId,
            cancellationToken);

        if (alreadyLogged)
        {
            return;
        }

        context.RecommendationInteractions.Add(new RecommendationInteraction
        {
            CustomerId = customerId,
            RecommendationId = recommendationId,
            InteractionType = type,
            ProductId = productId
        });

        await context.SaveChangesAsync(cancellationToken);
    }
}
