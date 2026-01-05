namespace TradeLogger.Data.Entities;

public class TradeEntity
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string MarketId { get; set; } = string.Empty;
    public string SelectionId { get; set; } = string.Empty;
    public decimal Stake { get; set; }
    public decimal Odds { get; set; }
    public string Type { get; set; } = string.Empty; // BACK / LAY
    public string Status { get; set; } = string.Empty; // PENDING / MATCHED / UNMATCHED / CANCELLED
    public decimal? ProfitLoss { get; set; }
    public decimal Commission { get; set; }
    public decimal? NetProfit { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
