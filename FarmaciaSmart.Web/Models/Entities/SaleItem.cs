namespace FarmaciaSmart.Web.Models.Entities;

public class SaleItem
{
    public int Id { get; set; }

    public int SaleId { get; set; }
    public Sale Sale { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>Captured at time of sale so later price changes don't retroactively alter historical receipts.</summary>
    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}
