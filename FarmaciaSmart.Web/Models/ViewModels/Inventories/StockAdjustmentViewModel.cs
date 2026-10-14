using System.ComponentModel.DataAnnotations;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Models.ViewModels.Inventories;

public class StockAdjustmentViewModel
{
    [Required, Display(Name = "Product")]
    public int ProductId { get; set; }

    [Required, Display(Name = "Branch")]
    public int BranchId { get; set; }

    [Required]
    [Display(Name = "Adjustment quantity (negative to decrease)")]
    public int AdjustmentQuantity { get; set; }

    [Required, StringLength(300)]
    public string Reason { get; set; } = string.Empty;

    public IEnumerable<Product> Products { get; set; } = [];
    public IEnumerable<Branch> Branches { get; set; } = [];
}
