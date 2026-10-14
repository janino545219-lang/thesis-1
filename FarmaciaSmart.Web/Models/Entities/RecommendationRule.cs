namespace FarmaciaSmart.Web.Models.Entities;

public class RecommendationRule
{
    public int Id { get; set; }
    public string Trigger { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    /// <summary>Optional: a specific product this rule targets. Null = any active, recommendation-eligible product in the category.</summary>
    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public string RecommendationType { get; set; } = string.Empty;
    public string? Warning { get; set; }
    public string? Restriction { get; set; }
    public string Explanation { get; set; } = string.Empty;

    public bool PharmacistApproved { get; set; }
    public bool IsActive { get; set; } = true;

    public string CreatedByUserId { get; set; } = string.Empty;
    public ApplicationUser CreatedByUser { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
