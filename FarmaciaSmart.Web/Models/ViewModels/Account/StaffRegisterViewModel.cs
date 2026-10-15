using System.ComponentModel.DataAnnotations;
using FarmaciaSmart.Web.Helpers;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Models.ViewModels.Account;

public class StaffRegisterViewModel
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "Work email address")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Please enter a valid contact phone number.")]
    [Display(Name = "Contact number")]
    public string? ContactNumber { get; set; }

    [Display(Name = "Assigned Pharmacy Branch")]
    public int? BranchId { get; set; }

    [Required(ErrorMessage = "Please select an assigned staff role.")]
    [Display(Name = "Staff Role")]
    public string Role { get; set; } = ApplicationRoles.Administrator;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Password and confirmation do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public IEnumerable<Branch> Branches { get; set; } = [];
}
