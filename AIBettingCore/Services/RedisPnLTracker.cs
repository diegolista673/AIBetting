using StackExchange.Redis;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AIBettingCore.Services
{
    /// <summary>
    /// Helper service to update P&L metrics in Redis for risk monitoring.
    /// Used by Accounting service after trades are settled.
    /// </summary>
    public class RedisPnLTracker
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;

        public RedisPnLTracker(IConnectionMultiplexer redis)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _db = _redis.GetDatabase();
        }

        /// <summary>
        /// Updates daily and total P&L after a trade is settled.
        /// </summary>
        /// <param name="netProfit">Net profit/loss from the trade (positive for profit, negative for loss).</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task UpdatePnLAsync(decimal netProfit, CancellationToken ct = default)
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var dailyKey = RedisKeys.DailyPnL(today);

            // Update daily P&L
            await _db.StringIncrementAsync(dailyKey, (double)netProfit);
            
            // Set TTL for daily key (48 hours to allow overlap)
            await _db.KeyExpireAsync(dailyKey, TimeSpan.FromHours(48));

            // Update total P&L
            await _db.StringIncrementAsync(RedisKeys.TotalPnL, (double)netProfit);
        }

        /// <summary>
        /// Gets the daily P&L for a specific date.
        /// </summary>
        /// <param name="date">Date in yyyy-MM-dd format.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>P&L for the specified date.</returns>
        public async Task<decimal> GetDailyPnLAsync(string date, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(date))
                throw new ArgumentException("Date cannot be null or empty", nameof(date));

            var dailyKey = RedisKeys.DailyPnL(date);
            var value = await _db.StringGetAsync(dailyKey);

            return value.HasValue && decimal.TryParse(value.ToString(), out var pnl) ? pnl : 0m;
        }

        /// <summary>
        /// Gets today's P&L.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Today's P&L.</returns>
        public async Task<decimal> GetTodayPnLAsync(CancellationToken ct = default)
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            return await GetDailyPnLAsync(today, ct);
        }

        /// <summary>
        /// Gets total cumulative P&L.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Total P&L across all time.</returns>
        public async Task<decimal> GetTotalPnLAsync(CancellationToken ct = default)
        {
            var value = await _db.StringGetAsync(RedisKeys.TotalPnL);
            return value.HasValue && decimal.TryParse(value.ToString(), out var pnl) ? pnl : 0m;
        }

        /// <summary>
        /// Resets daily P&L for a specific date (admin function).
        /// </summary>
        /// <param name="date">Date in yyyy-MM-dd format.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task ResetDailyPnLAsync(string date, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(date))
                throw new ArgumentException("Date cannot be null or empty", nameof(date));

            var dailyKey = RedisKeys.DailyPnL(date);
            await _db.KeyDeleteAsync(dailyKey);
        }
    }
}
