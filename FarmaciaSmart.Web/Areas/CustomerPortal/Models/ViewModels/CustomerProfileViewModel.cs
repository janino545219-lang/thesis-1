using System.ComponentModel.DataAnnotations;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Areas.CustomerPortal.Models.ViewModels;

public class CustomerProfileViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(30)]
    [Display(Name = "Contact number")]
    public string? ContactNumber { get; set; }

    [Display(Name = "Preferred branch")]
    public int? PreferredBranchId { get; set; }

    public IEnumerable<Branch> Branches { get; set; } = [];
}
