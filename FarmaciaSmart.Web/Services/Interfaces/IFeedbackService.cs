using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Services.Interfaces;

public interface IFeedbackService
{
    Task<(bool Succeeded, string? Error)> SubmitFeedbackAsync(
        int customerId, int recommendationItemId, FeedbackRating rating, string? comment, CancellationToken cancellationToken = default);

    Task LogInteractionAsync(
        int customerId, int recommendationId, InteractionType type, int? productId = null, CancellationToken cancellationToken = default);
}
