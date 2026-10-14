using System.ComponentModel.DataAnnotations;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Models.ViewModels.Inventories;

public class TransferStockViewModel
{
    [Required, Display(Name = "Source batch")]
    public int SourceInventoryBatchId { get; set; }

    [Required, Display(Name = "Destination branch")]
    public int DestinationBranchId { get; set; }

    [Required, Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public IEnumerable<InventoryBatch> AvailableBatches { get; set; } = [];
    public IEnumerable<Branch> Branches { get; set; } = [];
}
