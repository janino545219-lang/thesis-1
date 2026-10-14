using Microsoft.AspNetCore.Identity;

namespace FarmaciaSmart.Web.Models.Entities;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Home branch for branch-scoped staff. Null for customers and branch-unrestricted admins.</summary>
    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }
}
