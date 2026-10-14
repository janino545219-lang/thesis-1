namespace FarmaciaSmart.Web.Models.Entities;

public enum PaymentMethod
{
    Cash = 0,
    Card = 1,
    GCash = 2
}

public class Payment
{
    public int Id { get; set; }

    public int SaleId { get; set; }
    public Sale Sale { get; set; } = null!;

    public PaymentMethod Method { get; set; }
    public decimal AmountTendered { get; set; }
    public decimal ChangeAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
