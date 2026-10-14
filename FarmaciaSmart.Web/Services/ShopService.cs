using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Areas.CustomerPortal.Models.ViewModels;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

public class ShopService(ApplicationDbContext context) : IShopService
{
    public async Task<ShopFilterViewModel> BrowseAsync(
        int branchId, int? categoryId, string? searchTerm, decimal? minPrice, decimal? maxPrice,
        List<string>? brands, CancellationToken cancellationToken = default)
    {
        var baseQuery = context.Inventories
            .Include(i => i.Product).ThenInclude(p => p.Category)
            .Where(i => i.BranchId == branchId && i.QuantityOnHand > 0 && i.Product.IsActive);

        var availableBrands = await baseQuery
            .Where(i => i.Product.Brand != null)
            .Select(i => i.Product.Brand!)
            .Distinct()
            .OrderBy(b => b)
            .ToListAsync(cancellationToken);

        var query = baseQuery;

        if (categoryId.HasValue)
        {
            query = query.Where(i => i.Product.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalizedSearchTerm = searchTerm.ToUpperInvariant();
            query = query.Where(i => i.Product.Name.ToUpper().Contains(normalizedSearchTerm));
        }

        if (minPrice.HasValue)
        {
            query = query.Where(i => i.Product.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(i => i.Product.Price <= maxPrice.Value);
        }

        if (brands is { Count: > 0 })
        {
            query = query.Where(i => i.Product.Brand != null && brands.Contains(i.Product.Brand));
        }

        var products = await query
            .OrderBy(i => i.Product.Name)
            .Select(i => new ShopProductViewModel
            {
                ProductId = i.ProductId,
                Name = i.Product.Name,
                Brand = i.Product.Brand,
                Price = i.Product.Price,
                QuantityAvailable = i.QuantityOnHand
            })
            .ToListAsync(cancellationToken);

        var categories = await context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(cancellationToken);
        var branchName = await context.Branches.Where(b => b.Id == branchId).Select(b => b.Name).FirstOrDefaultAsync(cancellationToken);

        return new ShopFilterViewModel
        {
            CategoryId = categoryId,
            SearchTerm = searchTerm,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            Brands = brands ?? [],
            Categories = categories,
            AvailableBrands = availableBrands,
            Products = products,
            BranchName = branchName ?? string.Empty
        };
    }
}
