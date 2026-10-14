namespace FarmaciaSmart.Web.Models.Entities;

public enum CustomerStatus
{
    Active = 0,
    Inactive = 1,
    Suspended = 2
}

public class Customer
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int? PreferredBranchId { get; set; }
    public Branch? PreferredBranch { get; set; }

    public string? ContactNumber { get; set; }
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
