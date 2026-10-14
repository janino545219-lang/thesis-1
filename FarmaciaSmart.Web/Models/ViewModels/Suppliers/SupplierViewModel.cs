using System.ComponentModel.DataAnnotations;

namespace FarmaciaSmart.Web.Models.ViewModels.Suppliers;

public class SupplierViewModel
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Contact person")]
    public string? ContactPerson { get; set; }

    [Phone, StringLength(30)]
    public string? Phone { get; set; }

    [EmailAddress, StringLength(150)]
    public string? Email { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }
}
