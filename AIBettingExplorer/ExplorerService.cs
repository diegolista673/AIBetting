using AIBettingCore.Interfaces;
using AIBettingCore.Models;

namespace AIBettingExplorer;

/// <summary>
/// Consumes market snapshots from the stream client and publishes them to the cache bus.
/// </summary>
public class ExplorerService
{
    private readonly IMarketStreamClient _stream;
    private readonly ICacheBus _bus;

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
            await _bus.PublishPriceAsync(snapshot, ct);
        }
        await _stream.DisconnectAsync(ct);
    }
}
