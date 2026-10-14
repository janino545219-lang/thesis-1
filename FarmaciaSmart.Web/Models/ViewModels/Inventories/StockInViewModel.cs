using System.ComponentModel.DataAnnotations;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Models.ViewModels.Inventories;

public class StockInViewModel
{
    [Required, Display(Name = "Product")]
    public int ProductId { get; set; }

    [Required, Display(Name = "Branch")]
    public int BranchId { get; set; }

    [Required, StringLength(50)]
    [Display(Name = "Batch number")]
    public string BatchNumber { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Expiration date")]
    public DateTime? ExpirationDate { get; set; }

    [Required, Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }

    [Display(Name = "Supplier")]
    public int? SupplierId { get; set; }

    [Range(0, 1000000)]
    [Display(Name = "Unit cost")]
    public decimal? UnitCost { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public IEnumerable<Product> Products { get; set; } = [];
    public IEnumerable<Branch> Branches { get; set; } = [];
    public IEnumerable<Supplier> Suppliers { get; set; } = [];
}
