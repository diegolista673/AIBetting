using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AIBettingAccounting.Data.Entities;

namespace AIBettingAccounting.Data;

/// <summary>
/// EF Core implementation of the accounting repository.
/// </summary>
public class AccountingRepository : IAccountingRepository
{
    private readonly TradingDbContext _db;

    public AccountingRepository(TradingDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task LogTradeAsync(TradeEntity trade, CancellationToken ct = default)
    {
        if (trade.Id == Guid.Empty) trade.Id = Guid.NewGuid();
        trade.CreatedAt = DateTime.UtcNow;
        await _db.Trades.AddAsync(trade, ct);
        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<decimal> GetNetProfitAsync(DateOnly date, CancellationToken ct = default)
    {
        var start = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        return await _db.Trades
            .Where(t => t.Timestamp >= start && t.Timestamp <= end && t.Status == "MATCHED" && t.NetProfit != null)
            .SumAsync(t => t.NetProfit ?? 0m, ct);
    }
}
