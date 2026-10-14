using System.ComponentModel.DataAnnotations;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Models.ViewModels.Inventories;

public class DamagedItemViewModel
{
    [Required, Display(Name = "Batch")]
    public int InventoryBatchId { get; set; }

    [Required, Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }

    [Required, StringLength(300)]
    public string Reason { get; set; } = string.Empty;

    public IEnumerable<InventoryBatch> AvailableBatches { get; set; } = [];
}
