namespace AIBettingAccounting.Data.Entities;

/// <summary>
/// Aggregate daily performance metrics for reporting.
/// </summary>
public class DailySummaryEntity
{
    /// <summary>Identity column.</summary>
    public int Id { get; set; }
    /// <summary>Summary date.</summary>
    public DateOnly Date { get; set; }
    /// <summary>Total trades executed.</summary>
    public int TotalTrades { get; set; }
    /// <summary>Number of winning trades.</summary>
    public int WinningTrades { get; set; }
    /// <summary>Total gross profit.</summary>
    public decimal GrossProfit { get; set; }
    /// <summary>Total commission paid.</summary>
    public decimal TotalCommission { get; set; }
    /// <summary>Total net profit.</summary>
    public decimal NetProfit { get; set; }
    /// <summary>Return on investment.</summary>
    public decimal ROI { get; set; }
}
