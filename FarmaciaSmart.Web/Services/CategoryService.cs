using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Categories;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

public class CategoryService(ApplicationDbContext context) : ICategoryService
{
    public async Task<List<Category>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Categories.OrderBy(c => c.Name).ToListAsync(cancellationToken);

    public async Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<(bool Succeeded, string? Error)> CreateAsync(CategoryViewModel model, CancellationToken cancellationToken = default)
    {
        if (await context.Categories.AnyAsync(c => c.Name == model.Name, cancellationToken))
        {
            return (false, "A category with the same name already exists.");
        }

        context.Categories.Add(new Category
        {
            Name = model.Name,
            Description = model.Description
        });

        await context.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> UpdateAsync(CategoryViewModel model, CancellationToken cancellationToken = default)
    {
        var category = await context.Categories.FirstOrDefaultAsync(c => c.Id == model.Id, cancellationToken);
        if (category is null)
        {
            return (false, "Category not found.");
        }

        if (await context.Categories.AnyAsync(c => c.Id != model.Id && c.Name == model.Name, cancellationToken))
        {
            return (false, "A category with the same name already exists.");
        }

        category.Name = model.Name;
        category.Description = model.Description;
        category.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<bool> SetActiveStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var category = await context.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null)
        {
            return false;
        }

        category.IsActive = isActive;
        category.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
