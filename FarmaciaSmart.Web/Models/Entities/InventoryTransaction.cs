namespace FarmaciaSmart.Web.Models.Entities;

public enum InventoryTransactionType
{
    Purchase = 0,
    Sale = 1,
    Return = 2,
    Damaged = 3,
    Adjustment = 4,
    TransferIn = 5,
    TransferOut = 6
}

public class InventoryTransaction
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public int? InventoryBatchId { get; set; }
    public InventoryBatch? InventoryBatch { get; set; }

    public InventoryTransactionType TransactionType { get; set; }

    /// <summary>Signed: positive for stock-in movements, negative for stock-out movements.</summary>
    public int Quantity { get; set; }

    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }

    public string PerformedByUserId { get; set; } = string.Empty;
    public ApplicationUser PerformedByUser { get; set; } = null!;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
