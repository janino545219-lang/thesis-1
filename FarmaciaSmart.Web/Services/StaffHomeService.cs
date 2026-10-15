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
        int? branchId, bool isBranchManager = false, bool isSuperAdmin = false, CancellationToken cancellationToken = default)
    {
        var model = new StaffHomeViewModel
        {
            IsSuperAdmin = isSuperAdmin,
            IsAdministrator = isAdministrator || isSuperAdmin,
            IsBranchManager = isBranchManager,
            IsPharmacist = isPharmacist
        };

        if (branchId.HasValue)
        {
            model.AssignedBranchName = (await context.Branches.FindAsync([branchId.Value], cancellationToken))?.Name;
        }

        var isStaffUser = isSuperAdmin || isAdministrator || isBranchManager || isCashier || isInventoryStaff || isPharmacist || isHrPayroll;

        if (isSuperAdmin || isAdministrator)
        {
            model.BusinessOverview = await analyticsService.GetSummaryAsync(cancellationToken);
            model.SalesByBranch = await analyticsService.GetSalesByBranchAsync(cancellationToken);
            model.LowStockByBranch = await analyticsService.GetLowStockByBranchAsync(cancellationToken);
            model.TotalUsersCount = await context.Users.CountAsync(cancellationToken);
            model.TotalCustomersCount = await context.Customers.CountAsync(cancellationToken);
            model.TotalAuditLogsTodayCount = await context.AuditLogs.CountAsync(a => a.Timestamp >= DateTime.UtcNow.Date, cancellationToken);
        }

        if (isAdministrator || isBranchManager || isPharmacist)
        {
            model.SalesTrend = await analyticsService.GetSalesTrendAsync(14, cancellationToken);
            model.TopProducts = await analyticsService.GetTopProductsAsync(5, cancellationToken);
        }

        if (isStaffUser)
        {
            model.TotalProductsCount = await context.Products.CountAsync(p => p.IsActive, cancellationToken);
            model.TotalBranchesCount = await context.Branches.CountAsync(b => b.IsActive, cancellationToken);

            var ordersQuery = context.CustomerOrders.Where(o => o.Status == CustomerOrderStatus.Pending);
            if (branchId.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.BranchId == branchId.Value);
            }
            model.PendingOrdersCount = await ordersQuery.CountAsync(cancellationToken);

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

        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);

        if (isAdministrator || isCashier || isBranchManager || isPharmacist)
        {
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

        if (isAdministrator || isInventoryStaff || isBranchManager || isPharmacist)
        {
            var lowStockQuery = context.Inventories.Where(i => i.QuantityOnHand <= i.Product.LowStockThreshold);
            if (branchId.HasValue)
            {
                lowStockQuery = lowStockQuery.Where(i => i.BranchId == branchId.Value);
            }
            model.LowStockCount = await lowStockQuery.CountAsync(cancellationToken);

            var expirationCutoff = DateTime.UtcNow.Date.AddDays(30);
            var batchesQuery = context.InventoryBatches
                .Where(b => b.QuantityRemaining > 0 && b.ExpirationDate != null && b.ExpirationDate <= expirationCutoff);
            if (branchId.HasValue)
            {
                batchesQuery = batchesQuery.Where(b => b.BranchId == branchId.Value);
            }
            model.ExpiringSoonCount = await batchesQuery.CountAsync(cancellationToken);
        }

        if (isAdministrator || isPharmacist)
        {
            var pending = await recommendationService.GetPendingReviewAsync(cancellationToken);
            model.PendingReviewsCount = pending.Count;

            // Cash Flow & Expenses and Returns (ACM Figure 5: Pharmacist)
            var poQuery = context.PurchaseOrders.Where(p => p.CreatedAt >= todayStart && p.CreatedAt < todayEnd);
            if (branchId.HasValue)
            {
                poQuery = poQuery.Where(p => p.BranchId == branchId.Value);
            }
            model.TodaysExpensesTotal = await poQuery.SumAsync(p => (decimal?)p.TotalAmount, cancellationToken) ?? 0m;

            var returnsQuery = context.Returns.Include(r => r.Sale).Include(r => r.Branch).Where(r => r.ReturnDate >= todayStart && r.ReturnDate < todayEnd);
            if (branchId.HasValue)
            {
                returnsQuery = returnsQuery.Where(r => r.BranchId == branchId.Value);
            }
            var refundTotal = await returnsQuery.SumAsync(r => (decimal?)r.TotalRefundAmount, cancellationToken) ?? 0m;
            model.TodaysReturnsCount = await returnsQuery.CountAsync(cancellationToken);
            model.RecentReturns = await returnsQuery.OrderByDescending(r => r.ReturnDate).Take(5).ToListAsync(cancellationToken);

            model.TodaysNetCashFlow = (model.TodaysSalesTotal ?? 0m) - (model.TodaysExpensesTotal ?? 0m) - refundTotal;

            model.PharmacyPrescriptionsCount = await context.CustomerOrders
                .Where(o => o.Items.Any(i => i.Product.ProductType == ProductType.Pharmacy))
                .CountAsync(cancellationToken);
        }

        if (isAdministrator || isHrPayroll || isBranchManager)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var attendanceQuery = context.Attendances
                .Where(a => a.AttendanceDate == today && a.TimeIn != null);
            if (branchId.HasValue)
            {
                attendanceQuery = attendanceQuery.Where(a => a.Employee.BranchId == branchId.Value);
            }
            model.ClockedInTodayCount = await attendanceQuery.CountAsync(cancellationToken);

            if (isAdministrator || isHrPayroll)
            {
                model.DraftPayrollCount = await context.Payrolls
                    .CountAsync(p => p.Status == PayrollStatus.Draft, cancellationToken);
            }
        }

        return model;
    }
}
