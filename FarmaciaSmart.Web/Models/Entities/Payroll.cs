namespace FarmaciaSmart.Web.Models.Entities;

public enum PayrollStatus
{
    Draft = 0,
    Finalized = 1
}

public class Payroll
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }

    public decimal BasicPay { get; set; }
    public decimal GrossPay { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }

    public PayrollStatus Status { get; set; } = PayrollStatus.Draft;

    public string ProcessedByUserId { get; set; } = string.Empty;
    public ApplicationUser ProcessedByUser { get; set; } = null!;

    public ICollection<PayrollItem> Items { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
