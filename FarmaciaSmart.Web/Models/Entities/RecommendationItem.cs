namespace FarmaciaSmart.Web.Models.Entities;

public class RecommendationItem
{
    public int Id { get; set; }

    public int RecommendationId { get; set; }
    public Recommendation Recommendation { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int? RecommendationRuleId { get; set; }
    public RecommendationRule? RecommendationRule { get; set; }

    public int Rank { get; set; }
    public decimal Score { get; set; }

    /// <summary>Retrieved from the matching RecommendationRule, never fabricated.</summary>
    public string Explanation { get; set; } = string.Empty;
    public string? Warning { get; set; }

    public RecommendationFeedback? Feedback { get; set; }
}
