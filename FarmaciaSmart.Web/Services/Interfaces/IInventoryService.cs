using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels;
using FarmaciaSmart.Web.Models.ViewModels.Inventories;

namespace FarmaciaSmart.Web.Services.Interfaces;

public interface IInventoryService
{
    Task<List<StockRowViewModel>> GetStockAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<InventoryTransaction>> GetRecentTransactionsAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> StockInAsync(StockInViewModel model, string performedByUserId, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> AdjustStockAsync(StockAdjustmentViewModel model, string performedByUserId, CancellationToken cancellationToken = default);
    Task<List<InventoryBatch>> GetAvailableBatchesAsync(CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> TransferStockAsync(TransferStockViewModel model, string performedByUserId, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> ReportDamagedAsync(DamagedItemViewModel model, string reportedByUserId, CancellationToken cancellationToken = default);
    Task<List<DamagedItem>> GetDamagedItemsAsync(CancellationToken cancellationToken = default);
    Task<Sale?> FindSaleByReceiptAsync(string receiptNumber, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> ProcessReturnAsync(ProcessReturnViewModel model, string performedByUserId, CancellationToken cancellationToken = default);
    Task<List<Return>> GetReturnsAsync(CancellationToken cancellationToken = default);
}
