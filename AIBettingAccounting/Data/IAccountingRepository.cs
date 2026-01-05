using System.Threading;
using System.Threading.Tasks;
using AIBettingAccounting.Data.Entities;

namespace AIBettingAccounting.Data;

/// <summary>
/// Repository abstraction for accounting operations (trades persistence and queries).
/// </summary>
public interface IAccountingRepository
{
    /// <summary>
    /// Persist a trade into the database.
    /// </summary>
    Task LogTradeAsync(TradeEntity trade, CancellationToken ct = default);

    /// <summary>
    /// Calculate net profit for a specific date.
    /// </summary>
    Task<decimal> GetNetProfitAsync(DateOnly date, CancellationToken ct = default);
}
