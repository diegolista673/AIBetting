using AIBettingCore.Models;

namespace AIBettingCore
{
    /// <summary>
    /// Helper to standardize Redis keys and channels used across services.
    /// </summary>
    public static class RedisKeys
    {
        /// <summary>
        /// Key for prices hash per market and selection: prices:{marketId}:{selectionId}.
        /// </summary>
        public static string Prices(MarketId marketId, SelectionId selectionId) => $"prices:{marketId.Value}:{selectionId.Value}";

        /// <summary>
        /// Key for signals hash per market and selection: signals:{marketId}:{selectionId}.
        /// </summary>
        public static string Signals(MarketId marketId, SelectionId selectionId) => $"signals:{marketId.Value}:{selectionId.Value}";

        /// <summary>
        /// Key for an order hash by id: orders:{orderId}.
        /// </summary>
        public static string Orders(string orderId) => $"orders:{orderId}";

        /// <summary>
        /// Flag key to enable/disable trading globally.
        /// </summary>
        public const string TradingEnabledFlag = "flag:trading-enabled";

        /// <summary>
        /// Sorted set key to store latency timestamps for monitoring.
        /// </summary>
        public const string LatencyTimestamps = "latency:timestamps";

        /// <summary>
        /// Pub/Sub channel for price updates broadcast.
        /// </summary>
        public const string ChannelPriceUpdates = "channel:price-updates";
        /// <summary>
        /// Pub/Sub channel for trading signals.
        /// </summary>
        public const string ChannelTradingSignals = "channel:trading-signals";
        /// <summary>
        /// Pub/Sub channel for order state updates.
        /// </summary>
        public const string ChannelOrderUpdates = "channel:order-updates";
        /// <summary>
        /// Pub/Sub channel for kill-switch notifications.
        /// </summary>
        public const string ChannelKillSwitch = "channel:kill-switch";
    }

    /// <summary>
    /// Core-wide constants for versioning and defaults.
    /// </summary>
    public static class CoreConstants
    {
        /// <summary>
        /// Current core version.
        /// </summary>
        public const string Version = "1.0.0";
        /// <summary>
        /// Default commission rate applied to net profit calculations.
        /// </summary>
        public const decimal DefaultCommissionRate = 0.05m; // 5%
        /// <summary>
        /// Default timeout in milliseconds for internal operations.
        /// </summary>
        public const int DefaultTimeoutMs = 100;
    }
}
