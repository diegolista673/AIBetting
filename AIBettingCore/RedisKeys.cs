using AIBettingCore.Models;
using System;

namespace AIBettingCore
{
    /// <summary>
    /// Helper to standardize Redis keys and channels used across services.
    /// </summary>
    public static class RedisKeys
    {
        // Key prefixes
        private const string PricesPrefix = "prices:";
        private const string SignalsPrefix = "signals:";
        private const string OrdersPrefix = "orders:";
        private const string ExposurePrefix = "exposure:";
        private const string PositionsPrefix = "positions:";
        private const string PnLPrefix = "pnl:";

        /// <summary>
        /// Key for prices hash per market and selection: prices:{marketId}:{selectionId}.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when marketId or selectionId is null.</exception>
        /// <exception cref="ArgumentException">Thrown when marketId or selectionId value is empty or whitespace.</exception>
        public static string Prices(MarketId marketId, SelectionId selectionId)
        {
            ArgumentNullException.ThrowIfNull(marketId);
            ArgumentNullException.ThrowIfNull(selectionId);
            
            if (string.IsNullOrWhiteSpace(marketId.Value))
                throw new ArgumentException("MarketId value cannot be empty or whitespace.", nameof(marketId));
            
            if (string.IsNullOrWhiteSpace(selectionId.Value))
                throw new ArgumentException("SelectionId value cannot be empty or whitespace.", nameof(selectionId));
            
            return $"{PricesPrefix}{marketId.Value}:{selectionId.Value}";
        }

        /// <summary>
        /// Key for signals hash per market and selection: signals:{marketId}:{selectionId}.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when marketId or selectionId is null.</exception>
        /// <exception cref="ArgumentException">Thrown when marketId or selectionId value is empty or whitespace.</exception>
        public static string Signals(MarketId marketId, SelectionId selectionId)
        {
            ArgumentNullException.ThrowIfNull(marketId);
            ArgumentNullException.ThrowIfNull(selectionId);
            
            if (string.IsNullOrWhiteSpace(marketId.Value))
                throw new ArgumentException("MarketId value cannot be empty or whitespace.", nameof(marketId));
            
            if (string.IsNullOrWhiteSpace(selectionId.Value))
                throw new ArgumentException("SelectionId value cannot be empty or whitespace.", nameof(selectionId));
            
            return $"{SignalsPrefix}{marketId.Value}:{selectionId.Value}";
        }

        /// <summary>
        /// Key for an order hash by id: orders:{orderId}.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when orderId is null, empty or whitespace.</exception>
        public static string Orders(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                throw new ArgumentException("Order ID cannot be null, empty or whitespace.", nameof(orderId));
            
            return $"{OrdersPrefix}{orderId}";
        }

        /// <summary>
        /// Key for exposure hash per market: exposure:{marketId}.
        /// Stores current exposure (liability) for each selection in the market.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when marketId is null.</exception>
        /// <exception cref="ArgumentException">Thrown when marketId value is empty or whitespace.</exception>
        public static string Exposure(MarketId marketId)
        {
            ArgumentNullException.ThrowIfNull(marketId);
            
            if (string.IsNullOrWhiteSpace(marketId.Value))
                throw new ArgumentException("MarketId value cannot be empty or whitespace.", nameof(marketId));
            
            return $"{ExposurePrefix}{marketId.Value}";
        }

        /// <summary>
        /// Key for positions hash per market: positions:{marketId}.
        /// Stores open positions for each selection in the market.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when marketId is null.</exception>
        /// <exception cref="ArgumentException">Thrown when marketId value is empty or whitespace.</exception>
        public static string Positions(MarketId marketId)
        {
            ArgumentNullException.ThrowIfNull(marketId);
            
            if (string.IsNullOrWhiteSpace(marketId.Value))
                throw new ArgumentException("MarketId value cannot be empty or whitespace.", nameof(marketId));
            
            return $"{PositionsPrefix}{marketId.Value}";
        }

        /// <summary>
        /// Key for daily P&L hash: pnl:daily:{date}.
        /// Stores aggregated profit/loss for a specific date (format: yyyy-MM-dd).
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when date string is null, empty or whitespace.</exception>
        public static string DailyPnL(string date)
        {
            if (string.IsNullOrWhiteSpace(date))
                throw new ArgumentException("Date cannot be null, empty or whitespace.", nameof(date));
            
            return $"{PnLPrefix}daily:{date}";
        }

        /// <summary>
        /// Key for total P&L counter: pnl:total.
        /// Stores cumulative profit/loss across all time.
        /// </summary>
        public const string TotalPnL = "pnl:total";

        /// <summary>
        /// Flag key to enable/disable trading globally.
        /// </summary>
        public const string TradingEnabledFlag = "flag:trading-enabled";

        /// <summary>
        /// Sorted set key to store latency timestamps for monitoring.
        /// </summary>
        public const string LatencyTimestamps = "latency:timestamps";

        /// <summary>
        /// Hash key to store current bankroll and risk limits.
        /// </summary>
        public const string RiskLimits = "risk:limits";

        /// <summary>
        /// Sorted set key to track failed orders for circuit breaker logic.
        /// </summary>
        public const string FailedOrders = "failed:orders";

        /// <summary>
        /// Circuit breaker status key. Value: "0" = open, "1" = triggered.
        /// </summary>
        public const string CircuitBreakerStatus = "circuit-breaker:status";

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
        /// <summary>
        /// Pub/Sub channel for exposure updates.
        /// </summary>
        public const string ChannelExposureUpdates = "channel:exposure-updates";
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
        /// <summary>
        /// Maximum allowed latency in milliseconds before triggering kill-switch.
        /// </summary>
        public const int MaxLatencyMs = 500;
        /// <summary>
        /// Maximum number of consecutive failed orders before circuit breaker activates.
        /// </summary>
        public const int MaxConsecutiveFailures = 3;
        /// <summary>
        /// Minimum required liquidity (total matched) in currency units for market selection.
        /// </summary>
        public const decimal MinLiquidityThreshold = 50000m;
    }
}
