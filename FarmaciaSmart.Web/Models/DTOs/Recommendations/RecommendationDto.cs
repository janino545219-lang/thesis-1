namespace FarmaciaSmart.Web.Models.DTOs.Recommendations;

public class RecommendationDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string RequestText { get; set; } = string.Empty;
    public string ReviewStatus { get; set; } = string.Empty;
    public string? ReviewNotes { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<RecommendationItemDto> Items { get; set; } = [];
}
