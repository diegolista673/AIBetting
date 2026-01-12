using System.Text.Json;
using StackExchange.Redis;
using AIBettingCore.Interfaces;
using AIBettingCore.Models;
using Serilog;

namespace AIBettingExplorer;

/// <summary>
/// Redis-backed implementation of <see cref="ICacheBus"/> using StackExchange.Redis.
/// - Publishes snapshots and signals via Pub/Sub
/// - Persists lightweight hashes for last-known values with TTL
/// - Manages global trading-enabled flag
/// </summary>
public class RedisCacheBus : ICacheBus
{
    private readonly ConnectionMultiplexer _mux;
    private readonly IDatabase _db;
    private readonly ISubscriber _sub;
    private readonly JsonSerializerOptions _json;
    private static readonly RedisChannel PriceUpdatesChannel = new RedisChannel(AIBettingCore.RedisKeys.ChannelPriceUpdates, RedisChannel.PatternMode.Literal);
    private static readonly RedisChannel TradingSignalsChannel = new RedisChannel(AIBettingCore.RedisKeys.ChannelTradingSignals, RedisChannel.PatternMode.Literal);

    public RedisCacheBus(ConnectionMultiplexer mux, JsonSerializerOptions? json = null)
    {
        _mux = mux;
        _db = _mux.GetDatabase();
        _sub = _mux.GetSubscriber();
        _json = json ?? new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    public async Task PublishSignalAsync(TradingSignal signal, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(signal, _json);
        Log.Information("Publish signal {MarketId}/{SelectionId} {Type} @ {Odds} stake {Stake}", signal.MarketId.Value, signal.SelectionId.Value, signal.Type, signal.Odds, signal.Stake);
        await _sub.PublishAsync(TradingSignalsChannel, payload);

        var key = AIBettingCore.RedisKeys.Signals(signal.MarketId, signal.SelectionId);
        await _db.HashSetAsync(key, new[]
        {
            new HashEntry("type", signal.Type.ToString()),
            new HashEntry("odds", (double)signal.Odds),
            new HashEntry("stake", (double)signal.Stake),
            new HashEntry("ts", signal.Timestamp.ToUnixTimeMilliseconds()),
            new HashEntry("reason", signal.Reason ?? string.Empty)
        }, flags: CommandFlags.FireAndForget);
        await _db.KeyExpireAsync(key, TimeSpan.FromMinutes(10));
    }

    public async Task PublishPriceAsync(MarketSnapshot snapshot, CancellationToken ct)
    {
        // Serialize and store the complete snapshot in Redis
        var snapshotKey = $"prices:{snapshot.MarketId.Value}:{snapshot.Timestamp:O}";
        var snapshotJson = JsonSerializer.Serialize(snapshot, _json);
        await _db.StringSetAsync(snapshotKey, snapshotJson, TimeSpan.FromMinutes(5));
        
        // Publish notification with marketId and timestamp
        var payload = JsonSerializer.Serialize(new { snapshot.MarketId, snapshot.Timestamp }, _json);
        Log.Information("Publish price {MarketId} @ {Timestamp}", snapshot.MarketId.Value, snapshot.Timestamp);
        await _sub.PublishAsync(PriceUpdatesChannel, payload);
        
        // Store individual runner prices for quick access
        foreach (var r in snapshot.Runners)
        {
            var key = AIBettingCore.RedisKeys.Prices(snapshot.MarketId, r.SelectionId);
            var lpm = r.LastPriceMatched ?? 0m;
            await _db.HashSetAsync(key, new[]
            {
                new HashEntry("lpm", (double)lpm),
                new HashEntry("ts", snapshot.Timestamp.ToUnixTimeMilliseconds())
            }, flags: CommandFlags.FireAndForget);
            await _db.KeyExpireAsync(key, TimeSpan.FromSeconds(120));
        }
    }

    public Task SetTradingEnabledAsync(bool enabled, CancellationToken ct)
    {
        Log.Information("Set trading-enabled = {Enabled}", enabled);
        return _db.StringSetAsync(AIBettingCore.RedisKeys.TradingEnabledFlag, enabled ? "true" : "false");
    }

    public async Task<bool> GetTradingEnabledAsync(CancellationToken ct)
    {
        var val = await _db.StringGetAsync(AIBettingCore.RedisKeys.TradingEnabledFlag);
        var enabled = val.HasValue && val.ToString() == "true";
        Log.Information("Get trading-enabled = {Enabled}", enabled);
        return enabled;
    }
}
