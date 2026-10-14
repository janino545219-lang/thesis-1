namespace FarmaciaSmart.Web.Models.Entities;

public class ReturnItem
{
    public int Id { get; set; }

    public int ReturnId { get; set; }
    public Return Return { get; set; } = null!;

    public int SaleItemId { get; set; }
    public SaleItem SaleItem { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal RefundAmount { get; set; }

    /// <summary>Whether the returned stock went back to sellable Inventory, or was written off (e.g. opened/damaged item).</summary>
    public bool RestockedToInventory { get; set; }
}
