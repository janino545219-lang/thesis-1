using System.ComponentModel.DataAnnotations;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Models.ViewModels.Products;

public class ProductViewModel
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, Display(Name = "Category")]
    public int CategoryId { get; set; }

    [StringLength(100)]
    public string? Brand { get; set; }

    [StringLength(30)]
    public string? Unit { get; set; }

    [StringLength(50)]
    public string? Barcode { get; set; }

    [Required, Range(0.01, 1000000, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }

    [Required, Display(Name = "Product type")]
    public ProductType ProductType { get; set; }

    [Display(Name = "Requires expiration tracking")]
    public bool RequiresExpirationTracking { get; set; }

    [Range(0, 100000)]
    [Display(Name = "Low stock threshold")]
    public int LowStockThreshold { get; set; } = 10;

    [Display(Name = "Eligible for AI recommendation")]
    public bool IsRecommendationEligible { get; set; }

    public IEnumerable<Category> Categories { get; set; } = [];
}
