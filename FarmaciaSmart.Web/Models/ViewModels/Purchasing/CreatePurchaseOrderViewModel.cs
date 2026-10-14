using System.ComponentModel.DataAnnotations;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Models.ViewModels.Purchasing;

public class CreatePurchaseOrderViewModel
{
    [Required, Display(Name = "Supplier")]
    public int SupplierId { get; set; }

    [Required, Display(Name = "Branch")]
    public int BranchId { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Expected delivery date")]
    public DateOnly? ExpectedDeliveryDate { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public IEnumerable<Supplier> Suppliers { get; set; } = [];
    public IEnumerable<Branch> Branches { get; set; } = [];
}
