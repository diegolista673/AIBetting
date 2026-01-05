namespace TradeLogger.Data.Entities;

public class DailySummaryEntity
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public int TotalTrades { get; set; }
    public int WinningTrades { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal NetProfit { get; set; }
    public decimal ROI { get; set; }
}
