using System.ComponentModel.DataAnnotations;
using FarmaciaSmart.Web.Helpers;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Models.ViewModels.Users;

public class UserItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserIndexViewModel
{
    public List<UserItemViewModel> Users { get; set; } = [];
    public string? SearchTerm { get; set; }
    public string? SelectedRole { get; set; }
    public int TotalUsers => Users.Count;
    public int ActiveUsers => Users.Count(u => u.IsActive);
}

public class CreateUserViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    [Display(Name = "Work Email / Username")]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "Contact Number")]
    public string? PhoneNumber { get; set; }

    [Required]
    [Display(Name = "Assigned Role")]
    public string Role { get; set; } = ApplicationRoles.Cashier;

    [Display(Name = "Assigned Branch")]
    public int? BranchId { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public List<Branch> Branches { get; set; } = [];
}

public class EditUserViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    [Display(Name = "Work Email")]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "Contact Number")]
    public string? PhoneNumber { get; set; }

    [Required]
    [Display(Name = "Assigned Role")]
    public string Role { get; set; } = string.Empty;

    [Display(Name = "Assigned Branch")]
    public int? BranchId { get; set; }

    [Display(Name = "Account Status")]
    public bool IsActive { get; set; }

    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password (Leave blank to keep current)")]
    public string? NewPassword { get; set; }

    public List<Branch> Branches { get; set; } = [];
}
