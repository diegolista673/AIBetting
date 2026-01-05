using Microsoft.EntityFrameworkCore;
using TradeLogger.Data;
using TradeLogger.Data.Entities;

namespace TradeLogger;

public class TradeLoggerService
{
    private readonly TradingDbContext _db;

    public TradeLoggerService(TradingDbContext db)
    {
        _db = db;
    }

    public async Task LogTradeAsync(TradeEntity trade, CancellationToken ct = default)
    {
        if (trade.Id == Guid.Empty) trade.Id = Guid.NewGuid();
        trade.CreatedAt = DateTime.UtcNow;
        await _db.Trades.AddAsync(trade, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<decimal> GetNetProfitAsync(DateOnly date, CancellationToken ct = default)
    {
        var start = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        return await _db.Trades
            .Where(t => t.Timestamp >= start && t.Timestamp <= end && t.Status == "MATCHED" && t.NetProfit != null)
            .SumAsync(t => t.NetProfit ?? 0m, ct);
    }
}
