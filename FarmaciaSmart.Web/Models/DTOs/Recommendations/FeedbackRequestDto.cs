using System.ComponentModel.DataAnnotations;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Models.DTOs.Recommendations;

/// <summary>POST /api/recommendations/{id}/feedback body. Section 38.</summary>
public class FeedbackRequestDto
{
    [Required]
    public int RecommendationItemId { get; set; }

    [Required]
    public FeedbackRating Rating { get; set; }

    [StringLength(500)]
    public string? Comment { get; set; }
}
