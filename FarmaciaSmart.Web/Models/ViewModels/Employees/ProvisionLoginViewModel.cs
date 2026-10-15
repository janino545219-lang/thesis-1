using System.ComponentModel.DataAnnotations;
using FarmaciaSmart.Web.Helpers;

namespace FarmaciaSmart.Web.Models.ViewModels.Employees;

public class ProvisionLoginViewModel
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;

    [Required, EmailAddress]
    [Display(Name = "Work Email / Username")]
    public string Email { get; set; } = string.Empty;

    public string Position { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Staff Role")]
    public string Role { get; set; } = ApplicationRoles.Cashier;

    [Required]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
