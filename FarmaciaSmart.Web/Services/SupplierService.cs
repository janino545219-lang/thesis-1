using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Suppliers;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

public class SupplierService(ApplicationDbContext context) : ISupplierService
{
    public async Task<List<Supplier>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Suppliers.OrderBy(s => s.Name).ToListAsync(cancellationToken);

    public async Task<Supplier?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Suppliers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<(bool Succeeded, string? Error)> CreateAsync(SupplierViewModel model, CancellationToken cancellationToken = default)
    {
        if (await context.Suppliers.AnyAsync(s => s.Name == model.Name, cancellationToken))
        {
            return (false, "A supplier with the same name already exists.");
        }

        context.Suppliers.Add(new Supplier
        {
            Name = model.Name,
            ContactPerson = model.ContactPerson,
            Phone = model.Phone,
            Email = model.Email,
            Address = model.Address
        });

        await context.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> UpdateAsync(SupplierViewModel model, CancellationToken cancellationToken = default)
    {
        var supplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Id == model.Id, cancellationToken);
        if (supplier is null)
        {
            return (false, "Supplier not found.");
        }

        if (await context.Suppliers.AnyAsync(s => s.Id != model.Id && s.Name == model.Name, cancellationToken))
        {
            return (false, "A supplier with the same name already exists.");
        }

        supplier.Name = model.Name;
        supplier.ContactPerson = model.ContactPerson;
        supplier.Phone = model.Phone;
        supplier.Email = model.Email;
        supplier.Address = model.Address;
        supplier.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<bool> SetActiveStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var supplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (supplier is null)
        {
            return false;
        }

        supplier.IsActive = isActive;
        supplier.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
