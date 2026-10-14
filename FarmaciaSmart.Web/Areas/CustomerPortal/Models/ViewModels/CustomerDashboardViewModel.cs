using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Areas.CustomerPortal.Models.ViewModels;

public class MonthlySpendPoint
{
    public string MonthLabel { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}

public class CustomerDashboardViewModel
{
    public Customer Customer { get; set; } = null!;

    public decimal TotalSpent { get; set; }
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int RecommendationsReceived { get; set; }

    public List<MonthlySpendPoint> SpendingTrend { get; set; } = [];

    public int PendingOrderCount { get; set; }
    public int FulfilledOrderCount { get; set; }
    public int CancelledOrderCount { get; set; }

    public List<Sale> RecentPurchases { get; set; } = [];
    public List<Recommendation> RecentRecommendations { get; set; } = [];
}
