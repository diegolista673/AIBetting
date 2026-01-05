namespace AIBettingAccounting.Data.Entities;

/// <summary>
/// Trade persistence model mapped to PostgreSQL table `trades`.
/// </summary>
public class TradeEntity
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }
    /// <summary>Event timestamp in UTC.</summary>
    public DateTime Timestamp { get; set; }
    /// <summary>Betfair market id.</summary>
    public string MarketId { get; set; } = string.Empty;
    /// <summary>Betfair selection id.</summary>
    public string SelectionId { get; set; } = string.Empty;
    /// <summary>Stake amount.</summary>
    public decimal Stake { get; set; }
    /// <summary>Odds at which the trade occurred.</summary>
    public decimal Odds { get; set; }
    /// <summary>Order side type (BACK/LAY).</summary>
    public string Type { get; set; } = string.Empty; // BACK / LAY
    /// <summary>Order status (PENDING/MATCHED/UNMATCHED/CANCELLED).</summary>
    public string Status { get; set; } = string.Empty; // PENDING / MATCHED / UNMATCHED / CANCELLED
    /// <summary>Gross profit/loss before commission.</summary>
    public decimal? ProfitLoss { get; set; }
    /// <summary>Total commission charged.</summary>
    public decimal Commission { get; set; }
    /// <summary>Net profit after commission.</summary>
    public decimal? NetProfit { get; set; }
    /// <summary>Row creation timestamp in UTC.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
