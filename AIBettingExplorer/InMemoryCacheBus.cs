using AIBettingCore.Interfaces;
using System.Text.Json;
using AIBettingCore.Models;

namespace AIBettingExplorer;

/// <summary>
/// Simple in-memory implementation of <see cref="ICacheBus"/> for local development and testing.
/// Logs published messages to console and keeps a local trading-enabled flag.
/// </summary>
public class InMemoryCacheBus : ICacheBus
{
    /// <summary>
    /// Publish a trading signal by writing a JSON line to the console.
    /// </summary>
    public Task PublishSignalAsync(TradingSignal signal, CancellationToken ct)
    {
        Console.WriteLine($"Signal: {JsonSerializer.Serialize(signal)}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Publish a price snapshot by writing essential fields to the console.
    /// </summary>
    public Task PublishPriceAsync(MarketSnapshot snapshot, CancellationToken ct)
    {
        Console.WriteLine($"Price: {JsonSerializer.Serialize(new { snapshot.MarketId, snapshot.Timestamp })}");
        return Task.CompletedTask;
    }

    private bool _enabled = true;

    /// <summary>
    /// Set the in-memory trading enabled flag.
    /// </summary>
    public Task SetTradingEnabledAsync(bool enabled, CancellationToken ct)
    {
        _enabled = enabled;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Get the in-memory trading enabled flag.
    /// </summary>
    public Task<bool> GetTradingEnabledAsync(CancellationToken ct) => Task.FromResult(_enabled);
}
