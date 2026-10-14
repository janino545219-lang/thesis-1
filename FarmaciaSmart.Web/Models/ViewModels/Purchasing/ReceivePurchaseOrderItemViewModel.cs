using System.ComponentModel.DataAnnotations;

namespace FarmaciaSmart.Web.Models.ViewModels.Purchasing;

public class ReceivePurchaseOrderItemViewModel
{
    [Required]
    public int PurchaseOrderItemId { get; set; }

    [Required, Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }

    [Required, StringLength(50)]
    [Display(Name = "Batch number")]
    public string BatchNumber { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Expiration date")]
    public DateTime? ExpirationDate { get; set; }
}
