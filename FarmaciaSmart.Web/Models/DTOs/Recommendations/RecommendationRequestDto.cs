using System.ComponentModel.DataAnnotations;

namespace FarmaciaSmart.Web.Models.DTOs.Recommendations;

/// <summary>POST /api/recommendations body. Section 38.</summary>
public class RecommendationRequestDto
{
    [Required]
    public int CategoryId { get; set; }

    public int? BranchId { get; set; }

    public decimal? MaxPrice { get; set; }
}
