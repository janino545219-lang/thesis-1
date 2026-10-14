namespace FarmaciaSmart.Web.Models.ViewModels.Analytics;

public class AnalyticsDashboardViewModel
{
    public AnalyticsSummaryViewModel Summary { get; set; } = new();
    public List<DailySalesPoint> SalesTrend { get; set; } = [];
    public List<TopProductPoint> TopProducts { get; set; } = [];
    public List<BranchSalesPoint> SalesByBranch { get; set; } = [];
    public List<BranchLowStockPoint> LowStockByBranch { get; set; } = [];
    public List<BusyPeriodPoint> BusyPeriods { get; set; } = [];
}
