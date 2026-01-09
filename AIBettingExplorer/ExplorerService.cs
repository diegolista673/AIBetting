using AIBettingCore.Interfaces;
using Prometheus;
using Serilog;

namespace AIBettingExplorer;

/// <summary>
/// Consumes market snapshots from the stream client and publishes them to the cache bus.
/// </summary>
public class ExplorerService
{
    private readonly IMarketStreamClient _stream;
    private readonly ICacheBus _bus;

    // ✅ Usa Metrics.DefaultRegistry per garantire che le metriche siano esportate
    private static readonly Counter PriceUpdates = Metrics.WithCustomRegistry(Metrics.DefaultRegistry).CreateCounter(
        "aibetting_price_updates_total",
        "Total price updates processed"
    );

    private static readonly Histogram ProcessingLatency = Metrics.WithCustomRegistry(Metrics.DefaultRegistry).CreateHistogram(
        "aibetting_processing_latency_seconds",
        "Time to process price update"
    );

    private long _totalProcessed = 0;

    /// <summary>
    /// Initialize the explorer with a stream client and a cache bus.
    /// </summary>
    public ExplorerService(IMarketStreamClient stream, ICacheBus bus)
    {
        _stream = stream;
        _bus = bus;
        
        // Log metrica inizializzazione
        Log.Information("📊 Prometheus metrics initialized: aibetting_price_updates_total, aibetting_processing_latency_seconds");
        Log.Information("📊 Using Prometheus.NET DefaultRegistry for metrics export");
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
                _totalProcessed++;
                
                // Log ogni 50 snapshots per confermare che le metriche vengono aggiornate
                if (_totalProcessed % 50 == 0)
                {
                    Log.Information("📈 Metrics update: {Total} snapshots processed (aibetting_price_updates_total)", _totalProcessed);
                }
            }
        }
        
        await _stream.DisconnectAsync(ct);
    }
}
