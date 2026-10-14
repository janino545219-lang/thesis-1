namespace FarmaciaSmart.Web.Models.ViewModels.Analytics;

public class DailySalesPoint
{
    public DateOnly Date { get; set; }
    public decimal TotalAmount { get; set; }
}

public class TopProductPoint
{
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
}

public class BranchSalesPoint
{
    public string BranchName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}

public class BranchLowStockPoint
{
    public string BranchName { get; set; } = string.Empty;
    public int LowStockCount { get; set; }
}

/// <summary>One cell of the Busy Periods heatmap: how many completed sales happened in this day-of-week/hour slot.</summary>
public class BusyPeriodPoint
{
    public DayOfWeek DayOfWeek { get; set; }
    public int Hour { get; set; }
    public int SaleCount { get; set; }
}
