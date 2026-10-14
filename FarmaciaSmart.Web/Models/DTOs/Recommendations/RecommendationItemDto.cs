namespace FarmaciaSmart.Web.Models.DTOs.Recommendations;

public class RecommendationItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Rank { get; set; }
    public decimal Score { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public string? Warning { get; set; }

    /// <summary>False when this item was added by semantic broadening rather than a pharmacist-authored rule (Section 10/20).</summary>
    public bool PharmacistRuleBased { get; set; }

    public string? FeedbackRating { get; set; }
}
