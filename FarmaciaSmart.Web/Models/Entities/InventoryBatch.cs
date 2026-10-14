namespace FarmaciaSmart.Web.Models.Entities;

public class InventoryBatch
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public string BatchNumber { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
    public int QuantityReceived { get; set; }
    public int QuantityRemaining { get; set; }

    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public decimal? UnitCost { get; set; }
    public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
