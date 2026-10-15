using FarmaciaSmart.Web.Models.Entities;
using FarmaciaSmart.Web.Models.ViewModels.Analytics;

namespace FarmaciaSmart.Web.Models.ViewModels.Home;

public class StaffHomeViewModel
{
    public string DisplayName { get; set; } = string.Empty;
    public bool IsSuperAdmin { get; set; }
    public bool IsAdministrator { get; set; }
    public bool IsBranchManager { get; set; }
    public bool IsPharmacist { get; set; }
    public string? AssignedBranchName { get; set; }

    /// <summary>Pharmacist specific metrics (ACM Figure 5: Pharmacist).</summary>
    public int? PharmacyPrescriptionsCount { get; set; }
    public int? TodaysReturnsCount { get; set; }
    public decimal? TodaysExpensesTotal { get; set; }
    public decimal? TodaysNetCashFlow { get; set; }
    public List<Return>? RecentReturns { get; set; }

    /// <summary>SuperAdmin & Administrator system governance metrics.</summary>
    public int? TotalUsersCount { get; set; }
    public int? TotalCustomersCount { get; set; }
    public int? TotalAuditLogsTodayCount { get; set; }

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

    /// <summary>Live catalog and branch counts from database records.</summary>
    public int? TotalProductsCount { get; set; }
    public int? TotalBranchesCount { get; set; }
    public int? PendingOrdersCount { get; set; }

    /// <summary>Recent transactions and inventory activities from live records.</summary>
    public List<Sale>? RecentTransactions { get; set; }
    public List<InventoryTransaction>? RecentInventoryActivity { get; set; }

    /// <summary>Administrator only -- Chart.js datasets for interactive graphs.</summary>
    public List<DailySalesPoint>? SalesTrend { get; set; }
    public List<BranchSalesPoint>? SalesByBranch { get; set; }
    public List<TopProductPoint>? TopProducts { get; set; }
    public List<BranchLowStockPoint>? LowStockByBranch { get; set; }
}
