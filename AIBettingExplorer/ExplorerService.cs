using AIBettingCore.Interfaces;
using Prometheus; // ← Aggiungi questo

namespace AIBettingExplorer;

/// <summary>
/// Consumes market snapshots from the stream client and publishes them to the cache bus.
/// </summary>
public class ExplorerService
{
    private readonly IMarketStreamClient _stream;
    private readonly ICacheBus _bus;

    // Metriche Prometheus
    private static readonly Counter PriceUpdates = Metrics.CreateCounter(
        "aibetting_price_updates_total",
        "Total price updates processed"
    );

    private static readonly Histogram ProcessingLatency = Metrics.CreateHistogram(
        "aibetting_processing_latency_seconds",
        "Time to process price update"
    );

    /// <summary>
    /// Initialize the explorer with a stream client and a cache bus.
    /// </summary>
    public ExplorerService(IMarketStreamClient stream, ICacheBus bus)
    {
        _stream = stream;
        _bus = bus;
    }

    /// <summary>
    /// Connects to the market stream, forwards snapshots to the bus, then disconnects.
    /// </summary>
    public async Task RunAsync(CancellationToken ct)
    {
        await _stream.ConnectAsync(ct);
        
        await foreach (var snapshot in _stream.ReadSnapshotsAsync(ct))
        {
            // Misura latenza e incrementa contatore
            using (ProcessingLatency.NewTimer())
            {
                await _bus.PublishPriceAsync(snapshot, ct);
                PriceUpdates.Inc();
            }
        }
        
        await _stream.DisconnectAsync(ct);
    }
}
