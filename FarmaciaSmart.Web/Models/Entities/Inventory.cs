namespace FarmaciaSmart.Web.Models.Entities;

public class Inventory
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public int QuantityOnHand { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
