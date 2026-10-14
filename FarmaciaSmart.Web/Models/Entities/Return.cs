namespace FarmaciaSmart.Web.Models.Entities;

public class Return
{
    public int Id { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;

    public int SaleId { get; set; }
    public Sale Sale { get; set; } = null!;

    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public string ProcessedByUserId { get; set; } = string.Empty;
    public ApplicationUser ProcessedByUser { get; set; } = null!;

    public string Reason { get; set; } = string.Empty;
    public decimal TotalRefundAmount { get; set; }

    public ICollection<ReturnItem> Items { get; set; } = [];

    public DateTime ReturnDate { get; set; } = DateTime.UtcNow;
}
