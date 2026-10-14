using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Repositories.Interfaces;

namespace FarmaciaSmart.Web.Repositories;

public class ProductRepository(ApplicationDbContext context) : IProductRepository
{
    public async Task<List<Product>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Products.Include(p => p.Category).OrderBy(p => p.Name).ToListAsync(cancellationToken);

    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default) =>
        await context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Barcode == barcode, cancellationToken);

    public async Task<bool> BarcodeExistsAsync(string barcode, int? excludeId = null, CancellationToken cancellationToken = default) =>
        await context.Products.AnyAsync(p => p.Barcode == barcode && (excludeId == null || p.Id != excludeId), cancellationToken);

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        context.Products.Add(product);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SetActiveStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            return false;
        }

        product.IsActive = isActive;
        product.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
