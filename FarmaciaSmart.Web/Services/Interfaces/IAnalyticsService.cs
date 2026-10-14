using FarmaciaSmart.Web.Models.ViewModels.Analytics;

namespace FarmaciaSmart.Web.Services.Interfaces;

public interface IAnalyticsService
{
    Task<List<DailySalesPoint>> GetSalesTrendAsync(int days, CancellationToken cancellationToken = default);
    Task<List<TopProductPoint>> GetTopProductsAsync(int take, CancellationToken cancellationToken = default);
    Task<List<BranchSalesPoint>> GetSalesByBranchAsync(CancellationToken cancellationToken = default);
    Task<List<BranchLowStockPoint>> GetLowStockByBranchAsync(CancellationToken cancellationToken = default);
    Task<AnalyticsSummaryViewModel> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<List<BusyPeriodPoint>> GetBusyPeriodsAsync(int lookbackDays = 90, CancellationToken cancellationToken = default);
}
