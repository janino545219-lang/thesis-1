using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Branches;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

public class BranchService(ApplicationDbContext context) : IBranchService
{
    public async Task<List<Branch>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Branches.OrderBy(b => b.Name).ToListAsync(cancellationToken);

    public async Task<Branch?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Branches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<(bool Succeeded, string? Error)> CreateAsync(BranchViewModel model, CancellationToken cancellationToken = default)
    {
        if (await context.Branches.AnyAsync(b => b.Name == model.Name || b.Code == model.Code, cancellationToken))
        {
            return (false, "A branch with the same name or code already exists.");
        }

        context.Branches.Add(new Branch
        {
            Name = model.Name,
            Code = model.Code,
            Address = model.Address,
            ContactNumber = model.ContactNumber
        });

        await context.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> UpdateAsync(BranchViewModel model, CancellationToken cancellationToken = default)
    {
        var branch = await context.Branches.FirstOrDefaultAsync(b => b.Id == model.Id, cancellationToken);
        if (branch is null)
        {
            return (false, "Branch not found.");
        }

        if (await context.Branches.AnyAsync(b => b.Id != model.Id && (b.Name == model.Name || b.Code == model.Code), cancellationToken))
        {
            return (false, "A branch with the same name or code already exists.");
        }

        branch.Name = model.Name;
        branch.Code = model.Code;
        branch.Address = model.Address;
        branch.ContactNumber = model.ContactNumber;
        branch.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<bool> SetActiveStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var branch = await context.Branches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (branch is null)
        {
            return false;
        }

        branch.IsActive = isActive;
        branch.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
