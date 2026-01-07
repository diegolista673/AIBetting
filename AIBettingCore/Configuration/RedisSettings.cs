namespace AIBettingCore.Configuration;

// Centralized Redis configuration used across services
public class RedisSettings
{
    // Connection string, e.g. "localhost:6379,abortConnect=false"
    public string ConnectionString { get; set; } = "localhost:6379,abortConnect=false";

    // Optional password (ACL user configured on server)
    public string? Password { get; set; }

    // TTL policies (seconds)
    public int PricesTtlSeconds { get; set; } = 120;
    public int SignalsTtlSeconds { get; set; } = 600;
    public int OrdersTtlSeconds { get; set; } = 86400;

    // Channel names (defaults aligned with RedisKeys)
    public string ChannelPriceUpdates { get; set; } = RedisKeys.ChannelPriceUpdates;
    public string ChannelTradingSignals { get; set; } = RedisKeys.ChannelTradingSignals;
    public string ChannelOrderUpdates { get; set; } = RedisKeys.ChannelOrderUpdates;
    public string ChannelKillSwitch { get; set; } = RedisKeys.ChannelKillSwitch;

    // Flag key for trading enabled
    public string TradingEnabledFlagKey { get; set; } = RedisKeys.TradingEnabledFlag;

    // Sorted set key for latency metrics
    public string LatencySortedSetKey { get; set; } = RedisKeys.LatencyTimestamps;
}
