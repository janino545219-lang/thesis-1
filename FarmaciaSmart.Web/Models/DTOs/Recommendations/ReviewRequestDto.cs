using System.ComponentModel.DataAnnotations;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Models.DTOs.Recommendations;

/// <summary>POST /api/recommendations/{id}/review body. Section 38.</summary>
public class ReviewRequestDto
{
    [Required]
    public RecommendationReviewStatus Status { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}
