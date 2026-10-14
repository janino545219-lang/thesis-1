using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

public class CustomerService(ApplicationDbContext context) : ICustomerService
{
    public async Task<List<Customer>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Customers
            .Include(c => c.User)
            .Include(c => c.PreferredBranch)
            .OrderBy(c => c.User.LastName).ThenBy(c => c.User.FirstName)
            .ToListAsync(cancellationToken);

    public async Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Customers
            .Include(c => c.User)
            .Include(c => c.PreferredBranch)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<(bool Succeeded, string? Error)> SetStatusAsync(int id, CustomerStatus status, CancellationToken cancellationToken = default)
    {
        var customer = await context.Customers.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (customer is null)
        {
            return (false, "Customer not found.");
        }

        customer.Status = status;
        customer.UpdatedAt = DateTime.UtcNow;

        // ApplicationUser.IsActive is what CustomerPortal's Login action actually checks;
        // keep it in sync so Suspended/Inactive isn't a purely cosmetic label.
        var wasActive = customer.User.IsActive;
        customer.User.IsActive = status == CustomerStatus.Active;

        // Rotate the security stamp so an already-issued auth cookie stops working on the next
        // periodic re-validation (see Program.cs's SecurityStampValidatorOptions) instead of
        // staying valid for the rest of its up-to-8-hour lifetime after being suspended.
        if (wasActive && !customer.User.IsActive)
        {
            customer.User.SecurityStamp = Guid.NewGuid().ToString();
        }

        await context.SaveChangesAsync(cancellationToken);

        return (true, null);
    }
}
