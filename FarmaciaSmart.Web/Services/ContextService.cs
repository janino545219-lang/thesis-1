using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Recommendations;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

public class ContextService(ApplicationDbContext context) : IContextService
{
    public async Task<(RecommendationContext? Context, string? Error)> BuildContextAsync(
        int customerId, int categoryId, int? branchId, decimal? maxPrice = null, CancellationToken cancellationToken = default)
    {
        if (!await context.Categories.AnyAsync(c => c.Id == categoryId && c.IsActive, cancellationToken))
        {
            return (null, "Selected category is not valid.");
        }

        var resolvedBranchId = branchId;

        if (!resolvedBranchId.HasValue)
        {
            var customer = await context.Customers.FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);
            resolvedBranchId = customer?.PreferredBranchId;
        }

        if (!resolvedBranchId.HasValue)
        {
            var fallbackBranchId = await context.Branches
                .Where(b => b.IsActive)
                .Select(b => (int?)b.Id)
                .FirstOrDefaultAsync(cancellationToken);
            resolvedBranchId = fallbackBranchId;
        }

        if (!resolvedBranchId.HasValue)
        {
            return (null, "No branch is available to check product availability.");
        }

        if (!await context.Branches.AnyAsync(b => b.Id == resolvedBranchId && b.IsActive, cancellationToken))
        {
            return (null, "Resolved branch is not valid.");
        }

        var resolvedMaxPrice = await ResolveMaxPriceAsync(customerId, maxPrice, cancellationToken);

        return (new RecommendationContext
        {
            CustomerId = customerId,
            CategoryId = categoryId,
            BranchId = resolvedBranchId.Value,
            MaxPrice = resolvedMaxPrice
        }, null);
    }

    /// <summary>
    /// Section 8/9's explicit-preference profile, scoped to the one signal actually produced today:
    /// a budget ceiling stated via conversational refinement (Section 18). An explicitly-supplied
    /// maxPrice is remembered as the customer's new default; otherwise a previously-remembered
    /// default is recalled so the preference persists beyond a single refine chain.
    /// </summary>
    private async Task<decimal?> ResolveMaxPriceAsync(int customerId, decimal? explicitMaxPrice, CancellationToken cancellationToken)
    {
        var preference = await context.CustomerPreferences.FirstOrDefaultAsync(p => p.CustomerId == customerId, cancellationToken);

        if (explicitMaxPrice.HasValue)
        {
            if (preference is null)
            {
                context.CustomerPreferences.Add(new CustomerPreference { CustomerId = customerId, MaxBudget = explicitMaxPrice });
            }
            else
            {
                preference.MaxBudget = explicitMaxPrice;
                preference.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync(cancellationToken);
            return explicitMaxPrice;
        }

        return preference?.MaxBudget;
    }
}
