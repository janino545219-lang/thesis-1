using System.ComponentModel.DataAnnotations;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Models.ViewModels.Payrolls;

public class PayrollItemViewModel
{
    [Required, Display(Name = "Type")]
    public PayrollItemType ItemType { get; set; }

    [Required, StringLength(150)]
    public string Description { get; set; } = string.Empty;

    [Required, Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }
}
