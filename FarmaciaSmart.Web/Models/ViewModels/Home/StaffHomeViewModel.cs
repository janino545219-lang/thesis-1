using FarmaciaSmart.Web.Models.ViewModels.Analytics;

namespace FarmaciaSmart.Web.Models.ViewModels.Home;

public class StaffHomeViewModel
{
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Administrator only -- reuses the same summary the Analytics page shows.</summary>
    public AnalyticsSummaryViewModel? BusinessOverview { get; set; }

    /// <summary>Administrator or Cashier.</summary>
    public int? TodaysSalesCount { get; set; }
    public decimal? TodaysSalesTotal { get; set; }

    /// <summary>Administrator or InventoryStaff.</summary>
    public int? LowStockCount { get; set; }
    public int? ExpiringSoonCount { get; set; }

    /// <summary>Administrator or Pharmacist.</summary>
    public int? PendingReviewsCount { get; set; }

    /// <summary>Administrator or HrPayroll.</summary>
    public int? ClockedInTodayCount { get; set; }
    public int? DraftPayrollCount { get; set; }
}
