namespace FarmaciaSmart.Web.Models.Entities;

public enum EmploymentStatus
{
    Active = 0,
    OnLeave = 1,
    Terminated = 2
}

public class Employee
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string Position { get; set; } = string.Empty;

    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public EmploymentStatus EmploymentStatus { get; set; } = EmploymentStatus.Active;
    public string? ContactNumber { get; set; }
    public string? Email { get; set; }
    public DateTime HireDate { get; set; }
    public decimal MonthlySalary { get; set; }

    /// <summary>Optional link once a system login is provisioned for this employee (Stage 2 gap: no admin-facing "create staff user" screen yet).</summary>
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
