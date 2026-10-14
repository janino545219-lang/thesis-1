using System.ComponentModel.DataAnnotations;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Models.ViewModels.Recommendations;

public class RecommendationRuleViewModel
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Trigger { get; set; } = string.Empty;

    [Required, Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Display(Name = "Specific product (optional)")]
    public int? ProductId { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Recommendation type")]
    public string RecommendationType { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Warning { get; set; }

    [StringLength(500)]
    public string? Restriction { get; set; }

    [Required, StringLength(1000)]
    public string Explanation { get; set; } = string.Empty;

    [Display(Name = "Pharmacist approved")]
    public bool PharmacistApproved { get; set; }

    public IEnumerable<Category> Categories { get; set; } = [];
    public IEnumerable<Product> Products { get; set; } = [];
}
