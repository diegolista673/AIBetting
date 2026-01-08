using AIBettingCore.Interfaces;
using AIBettingCore.Models;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AIBettingCore.Services
{
    /// <summary>
    /// Redis-backed implementation of risk management service.
    /// Tracks exposure, validates orders against limits, and monitors circuit breaker conditions.
    /// </summary>
    public class RedisRiskManager : IRiskManager
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly TimeSpan _failedOrderWindow = TimeSpan.FromMinutes(5);

        public RedisRiskManager(IConnectionMultiplexer redis)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _db = _redis.GetDatabase();
        }

        /// <inheritdoc/>
        public async Task<RiskValidationResult> ValidateOrderAsync(PlaceOrderRequest request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);

            // Get current risk limits
            var limits = await GetRiskLimitsAsync(ct);

            // Check 1: Stake within maximum per order
            if (request.Stake > limits.MaxStakePerOrder)
            {
                return RiskValidationResult.Invalid($"Stake {request.Stake} exceeds max per order {limits.MaxStakePerOrder}");
            }

            // Check 2: Stake within percentage of bankroll
            var maxRiskAmount = limits.Bankroll * limits.MaxRiskPerTradePercent;
            var potentialLoss = request.Side == TradeSide.Lay
                ? request.Stake * (request.Odds - 1) // Lay liability
                : request.Stake; // Back stake

            if (potentialLoss > maxRiskAmount)
            {
                return RiskValidationResult.Invalid($"Risk {potentialLoss:F2} exceeds max risk per trade {maxRiskAmount:F2}");
            }

            // Check 3: Selection exposure limit
            var currentExposure = await GetMarketExposureAsync(request.MarketId, ct);
            var selectionExposure = currentExposure.TryGetValue(request.SelectionId.Value, out var existing) ? existing : 0;
            var projectedSelectionExposure = selectionExposure + potentialLoss;

            if (projectedSelectionExposure > limits.MaxExposurePerSelection)
            {
                return RiskValidationResult.Invalid(
                    $"Selection exposure {projectedSelectionExposure:F2} exceeds limit {limits.MaxExposurePerSelection:F2}",
                    projectedSelectionExposure);
            }

            // Check 4: Market total exposure limit
            var totalMarketExposure = currentExposure.Values.Sum() + potentialLoss;
            if (totalMarketExposure > limits.MaxExposurePerMarket)
            {
                return RiskValidationResult.Invalid(
                    $"Market exposure {totalMarketExposure:F2} exceeds limit {limits.MaxExposurePerMarket:F2}",
                    projectedSelectionExposure);
            }

            // Check 5: Daily loss limit
            var dailyPnL = await GetDailyPnLAsync(ct);
            if (dailyPnL < -limits.MaxDailyLoss)
            {
                return RiskValidationResult.Invalid($"Daily loss {Math.Abs(dailyPnL):F2} exceeds limit {limits.MaxDailyLoss:F2}");
            }

            // Check 6: Circuit breaker status
            if (await ShouldTriggerCircuitBreakerAsync(ct))
            {
                return RiskValidationResult.Invalid("Circuit breaker triggered - too many recent failures");
            }

            // Check 7: Trading enabled flag
            var tradingEnabled = await _db.StringGetAsync(RedisKeys.TradingEnabledFlag);
            if (!tradingEnabled.HasValue || tradingEnabled == "false" || tradingEnabled == "0")
            {
                return RiskValidationResult.Invalid("Trading is currently disabled");
            }

            return RiskValidationResult.Valid(projectedSelectionExposure);
        }

        /// <inheritdoc/>
        public async Task UpdateExposureAsync(MarketId marketId, SelectionId selectionId, TradeSide side, decimal stake, decimal odds, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(marketId);
            ArgumentNullException.ThrowIfNull(selectionId);

            var exposureKey = RedisKeys.Exposure(marketId);
            var positionsKey = RedisKeys.Positions(marketId);

            // Calculate exposure (liability for lay, stake for back)
            var exposure = side == TradeSide.Lay
                ? stake * (odds - 1)
                : stake;

            // Update exposure hash
            await _db.HashIncrementAsync(exposureKey, selectionId.Value, (double)exposure);

            // Update position tracking
            var positionData = new
            {
                SelectionId = selectionId.Value,
                Side = side.ToString(),
                Stake = stake,
                Odds = odds,
                Exposure = exposure,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            await _db.HashSetAsync(positionsKey, selectionId.Value, JsonSerializer.Serialize(positionData));

            // Set TTL on exposure keys (24 hours)
            await _db.KeyExpireAsync(exposureKey, TimeSpan.FromHours(24));
            await _db.KeyExpireAsync(positionsKey, TimeSpan.FromHours(24));

            // Publish exposure update
            var subscriber = _redis.GetSubscriber();
            await subscriber.PublishAsync(
                RedisChannel.Literal(RedisKeys.ChannelExposureUpdates),
                JsonSerializer.Serialize(new
                {
                    MarketId = marketId.Value,
                    SelectionId = selectionId.Value,
                    Exposure = exposure,
                    Timestamp = DateTimeOffset.UtcNow
                }));
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyDictionary<string, decimal>> GetMarketExposureAsync(MarketId marketId, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(marketId);

            var exposureKey = RedisKeys.Exposure(marketId);
            var entries = await _db.HashGetAllAsync(exposureKey);

            var result = new Dictionary<string, decimal>();
            foreach (var entry in entries)
            {
                if (entry.Value.HasValue && double.TryParse(entry.Value.ToString(), out var value))
                {
                    result[entry.Name.ToString()] = (decimal)value;
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task RecordFailedOrderAsync(string orderId, string reason, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                throw new ArgumentException("Order ID cannot be null or empty", nameof(orderId));

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var value = JsonSerializer.Serialize(new
            {
                OrderId = orderId,
                Reason = reason,
                Timestamp = timestamp
            });

            // Add to sorted set with timestamp as score
            await _db.SortedSetAddAsync(RedisKeys.FailedOrders, value, timestamp);

            // Remove entries older than the tracking window
            var cutoff = DateTimeOffset.UtcNow.Subtract(_failedOrderWindow).ToUnixTimeSeconds();
            await _db.SortedSetRemoveRangeByScoreAsync(RedisKeys.FailedOrders, double.NegativeInfinity, cutoff);
        }

        /// <inheritdoc/>
        public async Task<bool> ShouldTriggerCircuitBreakerAsync(CancellationToken ct)
        {
            // Get count of failed orders in the recent window
            var cutoff = DateTimeOffset.UtcNow.Subtract(_failedOrderWindow).ToUnixTimeSeconds();
            var recentFailures = await _db.SortedSetLengthAsync(
                RedisKeys.FailedOrders,
                cutoff,
                double.PositiveInfinity);

            return recentFailures >= CoreConstants.MaxConsecutiveFailures;
        }

        /// <inheritdoc/>
        public async Task<RiskLimits> GetRiskLimitsAsync(CancellationToken ct)
        {
            var entries = await _db.HashGetAllAsync(RedisKeys.RiskLimits);
            
            if (entries.Length == 0)
            {
                // Initialize with default limits if not set
                var defaults = RiskLimits.Default;
                await UpdateRiskLimitsAsync(defaults, ct);
                return defaults;
            }

            var dict = entries.ToDictionary(
                e => e.Name.ToString(),
                e => decimal.TryParse(e.Value.ToString(), out var v) ? v : 0m);

            return new RiskLimits
            {
                Bankroll = dict.GetValueOrDefault(nameof(RiskLimits.Bankroll), RiskLimits.Default.Bankroll),
                MaxExposurePerMarket = dict.GetValueOrDefault(nameof(RiskLimits.MaxExposurePerMarket), RiskLimits.Default.MaxExposurePerMarket),
                MaxExposurePerSelection = dict.GetValueOrDefault(nameof(RiskLimits.MaxExposurePerSelection), RiskLimits.Default.MaxExposurePerSelection),
                MaxStakePerOrder = dict.GetValueOrDefault(nameof(RiskLimits.MaxStakePerOrder), RiskLimits.Default.MaxStakePerOrder),
                MaxDailyLoss = dict.GetValueOrDefault(nameof(RiskLimits.MaxDailyLoss), RiskLimits.Default.MaxDailyLoss),
                MaxRiskPerTradePercent = dict.GetValueOrDefault(nameof(RiskLimits.MaxRiskPerTradePercent), RiskLimits.Default.MaxRiskPerTradePercent)
            };
        }

        /// <inheritdoc/>
        public async Task UpdateRiskLimitsAsync(RiskLimits limits, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(limits);

            var entries = new HashEntry[]
            {
                new(nameof(RiskLimits.Bankroll), limits.Bankroll.ToString("F2")),
                new(nameof(RiskLimits.MaxExposurePerMarket), limits.MaxExposurePerMarket.ToString("F2")),
                new(nameof(RiskLimits.MaxExposurePerSelection), limits.MaxExposurePerSelection.ToString("F2")),
                new(nameof(RiskLimits.MaxStakePerOrder), limits.MaxStakePerOrder.ToString("F2")),
                new(nameof(RiskLimits.MaxDailyLoss), limits.MaxDailyLoss.ToString("F2")),
                new(nameof(RiskLimits.MaxRiskPerTradePercent), limits.MaxRiskPerTradePercent.ToString("F4"))
            };

            await _db.HashSetAsync(RedisKeys.RiskLimits, entries);
        }

        /// <summary>
        /// Gets today's P&L from Redis.
        /// </summary>
        private async Task<decimal> GetDailyPnLAsync(CancellationToken ct)
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var pnlKey = RedisKeys.DailyPnL(today);
            var value = await _db.StringGetAsync(pnlKey);

            return value.HasValue && decimal.TryParse(value.ToString(), out var pnl) ? pnl : 0m;
        }
    }
}
