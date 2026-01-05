using Microsoft.EntityFrameworkCore;
using AIBettingAccounting.Data;
using AIBettingAccounting.Data.Entities;

namespace AIBettingAccounting;

/// <summary>
/// Service layer for accounting operations: persisting trades and basic queries.
/// </summary>
public class AIBettingAccountingService
{
    private readonly TradingDbContext _db;

    public AIBettingAccountingService(TradingDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Persist a trade record into the database.
    /// </summary>
    public async Task LogTradeAsync(TradeEntity trade, CancellationToken ct = default)
    {
        if (trade.Id == Guid.Empty) trade.Id = Guid.NewGuid();
        trade.CreatedAt = DateTime.UtcNow;
        await _db.Trades.AddAsync(trade, ct);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Compute net profit for a given date.
    /// </summary>
    public async Task<decimal> GetNetProfitAsync(DateOnly date, CancellationToken ct = default)
    {
        var start = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        return await _db.Trades
            .Where(t => t.Timestamp >= start && t.Timestamp <= end && t.Status == "MATCHED" && t.NetProfit != null)
            .SumAsync(t => t.NetProfit ?? 0m, ct);
    }
}
