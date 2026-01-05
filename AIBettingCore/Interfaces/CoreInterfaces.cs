using System.Threading;
using System.Threading.Tasks;

namespace AIBettingCore.Interfaces
{
    /// <summary>
    /// Abstraction over Betfair Stream consumer, provides async snapshots feed.
    /// </summary>
    public interface IMarketStreamClient
    {
        Task ConnectAsync(CancellationToken ct);
        Task DisconnectAsync(CancellationToken ct);
        IAsyncEnumerable<Models.MarketSnapshot> ReadSnapshotsAsync(CancellationToken ct);
    }

    /// <summary>
    /// Redis pub/sub + key-value contract used across services for prices/signals/flags.
    /// </summary>
    public interface ICacheBus
    {
        /// <summary>Publish trading signal messages.</summary>
        Task PublishSignalAsync(Models.TradingSignal signal, CancellationToken ct);
        /// <summary>Publish price updates.</summary>
        Task PublishPriceAsync(Models.MarketSnapshot snapshot, CancellationToken ct);
        /// <summary>Enable/disable trading via flag.</summary>
        Task SetTradingEnabledAsync(bool enabled, CancellationToken ct);
        /// <summary>Read trading-enabled flag.</summary>
        Task<bool> GetTradingEnabledAsync(CancellationToken ct);
    }

    /// <summary>
    /// Executor interface for place/cancel/list orders on the exchange.
    /// </summary>
    public interface IOrderExecutor
    {
        Task<Models.OrderResult> PlaceAsync(Models.PlaceOrderRequest request, CancellationToken ct);
        Task<Models.CancelOrderResult> CancelAsync(string orderId, CancellationToken ct);
        Task<IReadOnlyList<Models.CurrentOrder>> ListAsync(Models.MarketId marketId, CancellationToken ct);
    }

    /// <summary>
    /// Accounting persistence contract for logging trades.
    /// </summary>
    public interface ITradeLogger
    {
        Task LogAsync(Models.TradeRecord trade, CancellationToken ct);
    }
}
