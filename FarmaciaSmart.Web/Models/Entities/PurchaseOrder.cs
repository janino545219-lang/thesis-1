namespace FarmaciaSmart.Web.Models.Entities;

public enum PurchaseOrderStatus
{
    Draft = 0,
    Ordered = 1,
    PartiallyReceived = 2,
    Received = 3,
    Cancelled = 4
}

public class PurchaseOrder
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    public DateOnly? ExpectedDeliveryDate { get; set; }
    public string? Notes { get; set; }

    public decimal TotalAmount { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;
    public ApplicationUser CreatedByUser { get; set; } = null!;

    public DateTime? OrderedAt { get; set; }

    public ICollection<PurchaseOrderItem> Items { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
