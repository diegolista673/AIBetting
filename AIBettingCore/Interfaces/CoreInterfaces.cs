using System.Threading;
using System.Threading.Tasks;

namespace AIBettingCore.Interfaces
{
    /// <summary>
    /// Abstraction over Betfair Stream consumer, provides async snapshots feed.
    /// Implementations should manage WebSocket lifecycle and map raw updates to MarketSnapshot.
    /// </summary>
    public interface IMarketStreamClient
    {
        /// <summary>Establish connection to the market stream.</summary>
        Task ConnectAsync(CancellationToken ct);
        /// <summary>Close connection to the market stream.</summary>
        Task DisconnectAsync(CancellationToken ct);
        /// <summary>
        /// Read a continuous asynchronous sequence of market snapshots until cancellation.
        /// </summary>
        IAsyncEnumerable<Models.MarketSnapshot> ReadSnapshotsAsync(CancellationToken ct);
    }

    /// <summary>
    /// Redis pub/sub + key-value contract used across services for prices/signals/flags.
    /// Implementations should publish to channels and persist minimal state with TTL.
    /// </summary>
    public interface ICacheBus
    {
        /// <summary>Publish trading signal messages to channel and cache last signal.</summary>
        Task PublishSignalAsync(Models.TradingSignal signal, CancellationToken ct);
        /// <summary>Publish price updates to channel and cache last per-runner fields.</summary>
        Task PublishPriceAsync(Models.MarketSnapshot snapshot, CancellationToken ct);
        /// <summary>Enable/disable trading via global flag key.</summary>
        Task SetTradingEnabledAsync(bool enabled, CancellationToken ct);
        /// <summary>Read trading-enabled global flag.</summary>
        Task<bool> GetTradingEnabledAsync(CancellationToken ct);
    }

    /// <summary>
    /// Executor interface for place/cancel/list orders on the exchange.
    /// Should wrap Betfair JSON-RPC and ensure idempotency and status handling.
    /// </summary>
    public interface IOrderExecutor
    {
        /// <summary>Place an order on the exchange.</summary>
        Task<Models.OrderResult> PlaceAsync(Models.PlaceOrderRequest request, CancellationToken ct);
        /// <summary>Cancel an existing order by id.</summary>
        Task<Models.CancelOrderResult> CancelAsync(string orderId, CancellationToken ct);
        /// <summary>List current orders for a specific market.</summary>
        Task<IReadOnlyList<Models.CurrentOrder>> ListAsync(Models.MarketId marketId, CancellationToken ct);
    }

    /// <summary>
    /// Accounting persistence contract for logging trades.
    /// Implementations should persist `TradeRecord` into durable storage.
    /// </summary>
    public interface ITradeLogger
    {
        /// <summary>Persist a trade record.</summary>
        Task LogAsync(Models.TradeRecord trade, CancellationToken ct);
    }
}
