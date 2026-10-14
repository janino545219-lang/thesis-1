namespace FarmaciaSmart.Web.Models.Entities;

public class CustomerOrderItem
{
    public int Id { get; set; }

    public int CustomerOrderId { get; set; }
    public CustomerOrder CustomerOrder { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }

    /// <summary>Snapshot of Product.Price at order time -- the live price may change before pickup/fulfillment.</summary>
    public decimal UnitPriceAtOrderTime { get; set; }
}
