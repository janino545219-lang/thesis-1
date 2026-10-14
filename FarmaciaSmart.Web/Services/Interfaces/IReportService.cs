using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Inventories;

namespace FarmaciaSmart.Web.Services.Interfaces;

public interface IReportService
{
    Task<List<Sale>> GetSalesSummaryAsync(DateOnly? from, DateOnly? to, int? branchId, CancellationToken cancellationToken = default);
    Task<List<StockRowViewModel>> GetLowStockReportAsync(int? branchId, CancellationToken cancellationToken = default);
    Task<List<InventoryBatch>> GetExpirationReportAsync(int daysAhead, int? branchId, CancellationToken cancellationToken = default);
    Task<List<PurchaseOrder>> GetPurchasesReportAsync(DateOnly? from, DateOnly? to, int? branchId, int? supplierId, CancellationToken cancellationToken = default);
}
