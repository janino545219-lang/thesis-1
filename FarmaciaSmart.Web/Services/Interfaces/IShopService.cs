using FarmaciaSmart.Web.Areas.CustomerPortal.Models.ViewModels;

namespace FarmaciaSmart.Web.Services.Interfaces;

/// <summary>
/// Product browsing for the Customer Portal's Shop feature. Only surfaces products that are
/// active AND in stock at the given branch -- a customer pre-ordering for pickup should never
/// see something they can't actually pick up.
/// </summary>
public interface IShopService
{
    Task<ShopFilterViewModel> BrowseAsync(
        int branchId, int? categoryId, string? searchTerm, decimal? minPrice, decimal? maxPrice,
        List<string>? brands, CancellationToken cancellationToken = default);
}
