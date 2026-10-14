using Microsoft.EntityFrameworkCore;
using FarmaciaSmart.Web.Data;
using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Inventories;
using FarmaciaSmart.Web.Services.Interfaces;

namespace FarmaciaSmart.Web.Services;

public class ReportService(ApplicationDbContext context, IInventoryService inventoryService) : IReportService
{
    public async Task<List<Sale>> GetSalesSummaryAsync(DateOnly? from, DateOnly? to, int? branchId, CancellationToken cancellationToken = default)
    {
        var query = context.Sales
            .Include(s => s.Branch)
            .Include(s => s.CashierUser)
            .Where(s => s.Status == SaleStatus.Completed)
            .AsQueryable();

        if (from.HasValue)
        {
            var fromDate = from.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(s => s.SaleDate >= fromDate);
        }

        if (to.HasValue)
        {
            var toDate = to.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(s => s.SaleDate <= toDate);
        }

        if (branchId.HasValue)
        {
            query = query.Where(s => s.BranchId == branchId.Value);
        }

        return await query.OrderByDescending(s => s.SaleDate).ToListAsync(cancellationToken);
    }

    public async Task<List<StockRowViewModel>> GetLowStockReportAsync(int? branchId, CancellationToken cancellationToken = default)
    {
        var stock = await inventoryService.GetStockAsync(cancellationToken);
        var lowStock = stock.Where(s => s.Inventory.QuantityOnHand <= s.Inventory.Product.LowStockThreshold);

        if (branchId.HasValue)
        {
            lowStock = lowStock.Where(s => s.Inventory.BranchId == branchId.Value);
        }

        return lowStock.ToList();
    }

    public async Task<List<InventoryBatch>> GetExpirationReportAsync(int daysAhead, int? branchId, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.Date.AddDays(daysAhead);

        var query = context.InventoryBatches
            .Include(b => b.Product)
            .Include(b => b.Branch)
            .Where(b => b.QuantityRemaining > 0 && b.ExpirationDate != null && b.ExpirationDate <= cutoff)
            .AsQueryable();

        if (branchId.HasValue)
        {
            query = query.Where(b => b.BranchId == branchId.Value);
        }

        return await query.OrderBy(b => b.ExpirationDate).ToListAsync(cancellationToken);
    }

    public async Task<List<PurchaseOrder>> GetPurchasesReportAsync(
        DateOnly? from, DateOnly? to, int? branchId, int? supplierId, CancellationToken cancellationToken = default)
    {
        var query = context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Branch)
            .AsQueryable();

        if (from.HasValue)
        {
            var fromDate = from.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(p => p.CreatedAt >= fromDate);
        }

        if (to.HasValue)
        {
            var toDate = to.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(p => p.CreatedAt <= toDate);
        }

        if (branchId.HasValue)
        {
            query = query.Where(p => p.BranchId == branchId.Value);
        }

        if (supplierId.HasValue)
        {
            query = query.Where(p => p.SupplierId == supplierId.Value);
        }

        return await query.OrderByDescending(p => p.CreatedAt).ToListAsync(cancellationToken);
    }
}
