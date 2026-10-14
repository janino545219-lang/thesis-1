using System.ComponentModel.DataAnnotations;

namespace FarmaciaSmart.Web.Models.ViewModels.Branches;

public class BranchViewModel
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [Phone, StringLength(30)]
    [Display(Name = "Contact number")]
    public string? ContactNumber { get; set; }
}
