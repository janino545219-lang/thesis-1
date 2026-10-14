using System.ComponentModel.DataAnnotations;

namespace FarmaciaSmart.Web.Models.ViewModels.Purchasing;

public class PurchaseOrderItemViewModel
{
    [Required, Display(Name = "Product")]
    public int ProductId { get; set; }

    [Required, Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    [Display(Name = "Quantity ordered")]
    public int QuantityOrdered { get; set; }

    [Required, Range(0.01, double.MaxValue, ErrorMessage = "Unit cost must be greater than zero.")]
    [Display(Name = "Unit cost")]
    public decimal UnitCost { get; set; }
}
