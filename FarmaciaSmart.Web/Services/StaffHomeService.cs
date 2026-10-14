using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Home;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

public class StaffHomeService(
    ApplicationDbContext context,
    IAnalyticsService analyticsService,
    IRecommendationService recommendationService) : IStaffHomeService
{
    public async Task<StaffHomeViewModel> GetSummaryAsync(
        bool isAdministrator, bool isCashier, bool isInventoryStaff, bool isPharmacist, bool isHrPayroll,
        int? branchId, CancellationToken cancellationToken = default)
    {
        var model = new StaffHomeViewModel();

        var isStaffUser = isAdministrator || isCashier || isInventoryStaff || isPharmacist || isHrPayroll;

        if (isAdministrator)
        {
            model.BusinessOverview = await analyticsService.GetSummaryAsync(cancellationToken);
        }

        if (isStaffUser)
        {
            model.TotalProductsCount = await context.Products.CountAsync(p => p.IsActive, cancellationToken);
            model.TotalBranchesCount = await context.Branches.CountAsync(b => b.IsActive, cancellationToken);
            model.PendingOrdersCount = await context.CustomerOrders.CountAsync(o => o.Status == CustomerOrderStatus.Pending, cancellationToken);

            var transactionsQuery = context.Sales
                .Include(s => s.Branch)
                .Include(s => s.CashierUser)
                .Where(s => s.Status == SaleStatus.Completed);

            if (branchId.HasValue)
            {
                transactionsQuery = transactionsQuery.Where(s => s.BranchId == branchId.Value);
            }

            model.RecentTransactions = await transactionsQuery
                .OrderByDescending(s => s.SaleDate)
                .Take(5)
                .ToListAsync(cancellationToken);

            var inventoryQuery = context.InventoryTransactions
                .Include(t => t.Product)
                .Include(t => t.Branch);

            if (branchId.HasValue)
            {
                model.RecentInventoryActivity = await inventoryQuery
                    .Where(t => t.BranchId == branchId.Value)
                    .OrderByDescending(t => t.Timestamp)
                    .Take(5)
                    .ToListAsync(cancellationToken);
            }
            else
            {
                model.RecentInventoryActivity = await inventoryQuery
                    .OrderByDescending(t => t.Timestamp)
                    .Take(5)
                    .ToListAsync(cancellationToken);
            }
        }

        if (isAdministrator || isCashier)
        {
            var todayStart = DateTime.UtcNow.Date;
            var todayEnd = todayStart.AddDays(1);

            var salesQuery = context.Sales.Where(s =>
                s.Status == SaleStatus.Completed && s.SaleDate >= todayStart && s.SaleDate < todayEnd);

            if (branchId.HasValue)
            {
                salesQuery = salesQuery.Where(s => s.BranchId == branchId.Value);
            }

            var todaysSales = await salesQuery.Select(s => s.TotalAmount).ToListAsync(cancellationToken);
            model.TodaysSalesCount = todaysSales.Count;
            model.TodaysSalesTotal = todaysSales.Sum();
        }

        if (isAdministrator || isInventoryStaff)
        {
            model.LowStockCount = await context.Inventories
                .CountAsync(i => i.QuantityOnHand <= i.Product.LowStockThreshold, cancellationToken);

            var expirationCutoff = DateTime.UtcNow.Date.AddDays(30);
            model.ExpiringSoonCount = await context.InventoryBatches
                .CountAsync(b => b.QuantityRemaining > 0 && b.ExpirationDate != null && b.ExpirationDate <= expirationCutoff, cancellationToken);
        }

        if (isAdministrator || isPharmacist)
        {
            var pending = await recommendationService.GetPendingReviewAsync(cancellationToken);
            model.PendingReviewsCount = pending.Count;
        }

        if (isAdministrator || isHrPayroll)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            model.ClockedInTodayCount = await context.Attendances
                .CountAsync(a => a.AttendanceDate == today && a.TimeIn != null, cancellationToken);
            model.DraftPayrollCount = await context.Payrolls
                .CountAsync(p => p.Status == PayrollStatus.Draft, cancellationToken);
        }

        return model;
    }
}
