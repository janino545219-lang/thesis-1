using System.ComponentModel.DataAnnotations;

namespace FarmaciaSmart.Web.Models.DTOs.Recommendations;

/// <summary>POST/PUT /api/recommendation-rules body. Section 38.</summary>
public class RecommendationRuleWriteDto
{
    [Required, StringLength(200)]
    public string Trigger { get; set; } = string.Empty;

    [Required]
    public int CategoryId { get; set; }

    public int? ProductId { get; set; }

    [Required, StringLength(100)]
    public string RecommendationType { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Warning { get; set; }

    [StringLength(500)]
    public string? Restriction { get; set; }

    [Required, StringLength(1000)]
    public string Explanation { get; set; } = string.Empty;

    public bool PharmacistApproved { get; set; }
}
