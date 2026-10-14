namespace FarmaciaSmart.Web.Models.ViewModels.Analytics;

public class AnalyticsSummaryViewModel
{
    public decimal TotalSalesThisMonth { get; set; }
    public decimal? SalesPercentChange { get; set; }

    public int TransactionsThisMonth { get; set; }
    public decimal? TransactionsPercentChange { get; set; }

    public decimal AvgOrderValueThisMonth { get; set; }
    public decimal? AvgOrderValuePercentChange { get; set; }

    public int LowStockItemCount { get; set; }

    public int NewCustomersThisMonth { get; set; }
    public decimal? NewCustomersPercentChange { get; set; }
}
