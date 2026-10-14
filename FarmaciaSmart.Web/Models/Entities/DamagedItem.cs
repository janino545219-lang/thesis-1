namespace FarmaciaSmart.Web.Models.Entities;

public class DamagedItem
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public int? InventoryBatchId { get; set; }
    public InventoryBatch? InventoryBatch { get; set; }

    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;

    public string ReportedByUserId { get; set; } = string.Empty;
    public ApplicationUser ReportedByUser { get; set; } = null!;

    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
}
