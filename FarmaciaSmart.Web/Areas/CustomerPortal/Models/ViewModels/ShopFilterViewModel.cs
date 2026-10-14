using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Areas.CustomerPortal.Models.ViewModels;

public class ShopFilterViewModel
{
    public int? CategoryId { get; set; }
    public string? SearchTerm { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public List<string> Brands { get; set; } = [];

    public List<Category> Categories { get; set; } = [];
    public List<string> AvailableBrands { get; set; } = [];
    public List<ShopProductViewModel> Products { get; set; } = [];
    public int CartItemCount { get; set; }
    public string BranchName { get; set; } = string.Empty;
}
