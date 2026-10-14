using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Products;
using FarmaciaSmart.Web.Repositories.Interfaces;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

public class ProductService(IProductRepository productRepository, ApplicationDbContext context) : IProductService
{
    public Task<List<Product>> GetAllAsync(CancellationToken cancellationToken = default) =>
        productRepository.GetAllAsync(cancellationToken);

    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        productRepository.GetByIdAsync(id, cancellationToken);

    public async Task<(bool Succeeded, string? Error)> CreateAsync(ProductViewModel model, CancellationToken cancellationToken = default)
    {
        var categoryError = await ValidateCategoryAsync(model.CategoryId, cancellationToken);
        if (categoryError is not null)
        {
            return (false, categoryError);
        }

        if (!string.IsNullOrWhiteSpace(model.Barcode) && await productRepository.BarcodeExistsAsync(model.Barcode, null, cancellationToken))
        {
            return (false, "A product with this barcode already exists.");
        }

        await productRepository.AddAsync(new Product
        {
            Name = model.Name,
            CategoryId = model.CategoryId,
            Brand = model.Brand,
            Unit = model.Unit,
            Barcode = model.Barcode,
            Price = model.Price,
            ProductType = model.ProductType,
            RequiresExpirationTracking = model.RequiresExpirationTracking,
            LowStockThreshold = model.LowStockThreshold,
            IsRecommendationEligible = model.IsRecommendationEligible
        }, cancellationToken);

        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> UpdateAsync(ProductViewModel model, CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(model.Id, cancellationToken);
        if (product is null)
        {
            return (false, "Product not found.");
        }

        var categoryError = await ValidateCategoryAsync(model.CategoryId, cancellationToken);
        if (categoryError is not null)
        {
            return (false, categoryError);
        }

        if (!string.IsNullOrWhiteSpace(model.Barcode) && await productRepository.BarcodeExistsAsync(model.Barcode, model.Id, cancellationToken))
        {
            return (false, "A product with this barcode already exists.");
        }

        product.Name = model.Name;
        product.CategoryId = model.CategoryId;
        product.Brand = model.Brand;
        product.Unit = model.Unit;
        product.Barcode = model.Barcode;
        product.Price = model.Price;
        product.ProductType = model.ProductType;
        product.RequiresExpirationTracking = model.RequiresExpirationTracking;
        product.LowStockThreshold = model.LowStockThreshold;
        product.IsRecommendationEligible = model.IsRecommendationEligible;
        product.UpdatedAt = DateTime.UtcNow;

        await productRepository.UpdateAsync(product, cancellationToken);
        return (true, null);
    }

    public Task<bool> SetActiveStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default) =>
        productRepository.SetActiveStatusAsync(id, isActive, cancellationToken);

    private async Task<string?> ValidateCategoryAsync(int categoryId, CancellationToken cancellationToken)
    {
        var isValid = await context.Categories.AnyAsync(c => c.Id == categoryId && c.IsActive, cancellationToken);
        return isValid ? null : "Selected category is not valid.";
    }
}
