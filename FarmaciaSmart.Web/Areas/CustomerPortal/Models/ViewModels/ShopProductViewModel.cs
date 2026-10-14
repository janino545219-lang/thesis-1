namespace FarmaciaSmart.Web.Areas.CustomerPortal.Models.ViewModels;

public class ShopProductViewModel
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public decimal Price { get; set; }
    public int QuantityAvailable { get; set; }
}
