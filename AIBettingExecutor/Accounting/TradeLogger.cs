using AIBettingAccounting.Data.Entities;
using AIBettingCore.Models;
using AIBettingExecutor.OrderManagement;
using Serilog;
using StackExchange.Redis;
using System.Text.Json;

namespace AIBettingExecutor.Accounting;

/// <summary>
/// Logs executed trades to Redis and prepares data for persistence to PostgreSQL.
/// </summary>
public class TradeLogger
{
    private readonly IConnectionMultiplexer _redis;
    private readonly Serilog.ILogger _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly decimal _commissionRate;

    public TradeLogger(
        IConnectionMultiplexer redis,
        decimal commissionRate = 0.05m,
        Serilog.ILogger? logger = null)
    {
        _redis = redis;
        _commissionRate = commissionRate;
        _logger = logger ?? Log.ForContext<TradeLogger>();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Log a matched order as a trade.
    /// </summary>
    public async Task LogTradeAsync(ManagedOrder order, CancellationToken ct = default)
    {
        try
        {
            // Calculate commission
            var potentialWin = (order.RequestedOdds - 1) * order.MatchedSize;
            var commission = potentialWin * _commissionRate;

            var trade = new TradeEntity
            {
                Id = Guid.NewGuid(),
                Timestamp = order.PlacedAt.UtcDateTime,
                MarketId = order.MarketId,
                SelectionId = order.SelectionId,
                Stake = order.MatchedSize,
                Odds = order.AveragePriceMatched ?? order.RequestedOdds,
                Type = order.Side.ToString().ToUpperInvariant(),
                Status = order.Status.ToString().ToUpperInvariant(),
                Commission = commission,
                NetProfit = null, // Will be calculated when market settles
                CreatedAt = DateTime.UtcNow
            };

            // Store in Redis for async processing
            var db = _redis.GetDatabase();
            var key = $"trades:pending:{trade.Id}";
            var json = JsonSerializer.Serialize(trade, _jsonOptions);
            await db.StringSetAsync(key, json, TimeSpan.FromHours(24));

            // Publish notification for accounting service
            var publisher = _redis.GetSubscriber();
            await publisher.PublishAsync(
                new RedisChannel("channel:trades", RedisChannel.PatternMode.Literal),
                json);

            _logger.Information("📝 Trade logged: {OrderId} - {Side} {Stake}@{Odds} on {Selection}",
                order.OrderId, order.Side, order.MatchedSize,
                order.AveragePriceMatched ?? order.RequestedOdds,
                order.SelectionId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error logging trade for order {OrderId}", order.OrderId);
        }
    }

    /// <summary>
    /// Log multiple trades in batch.
    /// </summary>
    public async Task LogTradesBatchAsync(IEnumerable<ManagedOrder> orders, CancellationToken ct = default)
    {
        foreach (var order in orders)
        {
            await LogTradeAsync(order, ct);
        }
    }

    /// <summary>
    /// Update trade with settlement result.
    /// </summary>
    public async Task UpdateTradeResultAsync(
        Guid tradeId,
        decimal profitLoss,
        CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = $"trades:settled:{tradeId}";

            var settlement = new
            {
                tradeId,
                profitLoss,
                netProfit = profitLoss * (1 - _commissionRate),
                settledAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(settlement, _jsonOptions);
            await db.StringSetAsync(key, json, TimeSpan.FromDays(7));

            _logger.Information("✅ Trade {TradeId} settled: P/L = {ProfitLoss}", tradeId, profitLoss);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error updating trade result for {TradeId}", tradeId);
        }
    }
}
