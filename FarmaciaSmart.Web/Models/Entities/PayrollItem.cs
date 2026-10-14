namespace FarmaciaSmart.Web.Models.Entities;

public enum PayrollItemType
{
    Earning = 0,
    Deduction = 1
}

public class PayrollItem
{
    public int Id { get; set; }

    public int PayrollId { get; set; }
    public Payroll Payroll { get; set; } = null!;

    public PayrollItemType ItemType { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
