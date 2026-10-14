namespace FarmaciaSmart.Web.Models.DTOs.Recommendations;

public class RecommendationRuleDto
{
    public int Id { get; set; }
    public string Trigger { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public string RecommendationType { get; set; } = string.Empty;
    public string? Warning { get; set; }
    public string? Restriction { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public bool PharmacistApproved { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
