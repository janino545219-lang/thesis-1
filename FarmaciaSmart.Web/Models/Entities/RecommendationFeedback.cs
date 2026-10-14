namespace FarmaciaSmart.Web.Models.Entities;

public enum FeedbackRating
{
    Helpful = 0,
    NotHelpful = 1
}

public class RecommendationFeedback
{
    public int Id { get; set; }

    public int RecommendationItemId { get; set; }
    public RecommendationItem RecommendationItem { get; set; } = null!;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public FeedbackRating Rating { get; set; }
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
