namespace FarmaciaSmart.Web.Models.Entities;

public enum SaleStatus
{
    Completed = 0,
    Voided = 1
}

public class Sale
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;

    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string CashierUserId { get; set; } = string.Empty;
    public ApplicationUser CashierUser { get; set; } = null!;

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public SaleStatus Status { get; set; } = SaleStatus.Completed;

    public ICollection<SaleItem> Items { get; set; } = [];
    public Payment? Payment { get; set; }

    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
}
