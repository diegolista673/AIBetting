using AIBettingCore.Models;

namespace AIBettingCore
{
    /// <summary>
    /// Helper to standardize Redis keys and channels used across services.
    /// </summary>
    public static class RedisKeys
    {
        public static string Prices(MarketId marketId, SelectionId selectionId) => $"prices:{marketId.Value}:{selectionId.Value}";
        public static string Signals(MarketId marketId, SelectionId selectionId) => $"signals:{marketId.Value}:{selectionId.Value}";
        public static string Orders(string orderId) => $"orders:{orderId}";
        public const string TradingEnabledFlag = "flag:trading-enabled";
        public const string LatencyTimestamps = "latency:timestamps";

        public const string ChannelPriceUpdates = "channel:price-updates";
        public const string ChannelTradingSignals = "channel:trading-signals";
        public const string ChannelOrderUpdates = "channel:order-updates";
        public const string ChannelKillSwitch = "channel:kill-switch";
    }

    /// <summary>
    /// Core-wide constants for versioning and defaults.
    /// </summary>
    public static class CoreConstants
    {
        public const string Version = "1.0.0";
        public const decimal DefaultCommissionRate = 0.05m; // 5%
        public const int DefaultTimeoutMs = 100;
    }
}
