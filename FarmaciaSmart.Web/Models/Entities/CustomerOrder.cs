namespace FarmaciaSmart.Web.Models.Entities;

public enum CustomerOrderStatus
{
    Pending = 0,
    Fulfilled = 1,
    Cancelled = 2
}

/// <summary>
/// A customer-submitted pre-order for in-store pickup (Shop feature). Deliberately separate from
/// Sale/SaleItem: submitting one never touches Inventory -- staff fulfill it through the existing
/// POS flow when the customer arrives, so the real sale/stock-deduction code path is untouched.
/// </summary>
public class CustomerOrder
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public CustomerOrderStatus Status { get; set; } = CustomerOrderStatus.Pending;
    public string? Notes { get; set; }
    public decimal TotalAmount { get; set; }

    public string? FulfilledByUserId { get; set; }
    public ApplicationUser? FulfilledByUser { get; set; }
    public DateTime? FulfilledAt { get; set; }

    public ICollection<CustomerOrderItem> Items { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
