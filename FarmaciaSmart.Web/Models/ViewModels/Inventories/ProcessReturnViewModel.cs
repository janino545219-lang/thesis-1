using System.ComponentModel.DataAnnotations;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Models.ViewModels.Inventories;

public class ProcessReturnViewModel
{
    [Required, Display(Name = "Receipt number")]
    public string ReceiptNumber { get; set; } = string.Empty;

    [Required, Display(Name = "Sale item")]
    public int SaleItemId { get; set; }

    [Required, Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }

    [Required, StringLength(300)]
    public string Reason { get; set; } = string.Empty;

    [Display(Name = "Return to sellable stock")]
    public bool RestockToInventory { get; set; } = true;

    public Sale? Sale { get; set; }
}
