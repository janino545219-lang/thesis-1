using System.ComponentModel.DataAnnotations;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Models.ViewModels.Employees;

public class EmployeeViewModel
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Position { get; set; } = string.Empty;

    [Required, Display(Name = "Branch")]
    public int BranchId { get; set; }

    [Required, Display(Name = "Employment status")]
    public EmploymentStatus EmploymentStatus { get; set; } = EmploymentStatus.Active;

    [Phone, StringLength(30)]
    [Display(Name = "Contact number")]
    public string? ContactNumber { get; set; }

    [EmailAddress, StringLength(150)]
    public string? Email { get; set; }

    [Required, DataType(DataType.Date)]
    [Display(Name = "Hire date")]
    public DateTime HireDate { get; set; } = DateTime.Today;

    [Required, Range(0.01, double.MaxValue, ErrorMessage = "Monthly salary must be greater than zero.")]
    [Display(Name = "Monthly salary")]
    public decimal MonthlySalary { get; set; }

    public IEnumerable<Branch> Branches { get; set; } = [];

    [Display(Name = "Create system login account for this employee")]
    public bool CreateLoginAccount { get; set; } = true;

    [Display(Name = "Staff Role")]
    public string? SystemRole { get; set; } = FarmaciaSmart.Web.Helpers.ApplicationRoles.Cashier;

    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
    [Display(Name = "Initial Password")]
    public string? LoginPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(LoginPassword), ErrorMessage = "The password and confirmation password do not match.")]
    [Display(Name = "Confirm Password")]
    public string? ConfirmLoginPassword { get; set; }

    public string? UserId { get; set; }
}
