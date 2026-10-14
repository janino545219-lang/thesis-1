namespace FarmaciaSmart.Web.Models.Entities;

public enum ProductType
{
    Pharmacy = 0,
    MiniMart = 1
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string? Brand { get; set; }
    public string? Unit { get; set; }
    public string? Barcode { get; set; }

    public decimal Price { get; set; }
    public ProductType ProductType { get; set; }

    /// <summary>Whether batches of this product must track expiration dates (Stage 4 Inventory).</summary>
    public bool RequiresExpirationTracking { get; set; }

    public int LowStockThreshold { get; set; } = 10;

    /// <summary>Coarse eligibility gate for AI candidate generation; individual recommendations still require a pharmacist-approved rule (Stage 9+).</summary>
    public bool IsRecommendationEligible { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
