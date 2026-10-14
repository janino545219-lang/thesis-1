namespace FarmaciaSmart.Web.Models.Entities;

public enum RecommendationReviewStatus
{
    PendingReview = 0,
    Approved = 1,
    Modified = 2,
    Rejected = 3
}

public class Recommendation
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public string RequestText { get; set; } = string.Empty;

    public RecommendationReviewStatus ReviewStatus { get; set; } = RecommendationReviewStatus.PendingReview;
    public string? ReviewedByUserId { get; set; }
    public ApplicationUser? ReviewedByUser { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }

    public ICollection<RecommendationItem> Items { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
