using System.ComponentModel.DataAnnotations;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Models.ViewModels.Sales;

public class CheckoutViewModel
{
    public List<CartItemDto> CartItems { get; set; } = [];
    public decimal SubTotal => CartItems.Sum(i => i.LineTotal);

    [Display(Name = "Senior Citizen / PWD discount (20%)")]
    public bool ApplySeniorPwdDiscount { get; set; }

    public decimal DiscountAmount => ApplySeniorPwdDiscount ? Math.Round(SubTotal * 0.20m, 2) : 0m;
    public decimal TotalAmount => SubTotal - DiscountAmount;

    [Display(Name = "Customer contact number (optional)")]
    [Phone]
    public string? CustomerContactNumber { get; set; }

    [Required, Display(Name = "Payment method")]
    public PaymentMethod PaymentMethod { get; set; }

    [Required, Range(0.01, double.MaxValue, ErrorMessage = "Amount tendered must be greater than zero.")]
    [Display(Name = "Amount tendered")]
    public decimal AmountTendered { get; set; }
}
