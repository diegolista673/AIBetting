using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using AIBettingCore.Interfaces;
using AIBettingCore.Models;
using System.Runtime.CompilerServices;

namespace AIBettingExplorer;

/// <summary>
/// Minimal stub implementation of <see cref="IMarketStreamClient"/>.
/// Emits a demo <see cref="MarketSnapshot"/>; replace with real Betfair Stream integration.
/// </summary>
public class BetfairMarketStreamClient : IMarketStreamClient
{
    private ClientWebSocket? _ws;
    private readonly Uri _uri = new Uri("wss://stream-api.betfair.com/api/v1");

    /// <summary>
    /// Connect to the stream (no-op in stub).
    /// </summary>
    public async Task ConnectAsync(CancellationToken ct)
    {
        _ws = new ClientWebSocket();
        await _ws.ConnectAsync(_uri, ct);

        var appKey = Environment.GetEnvironmentVariable("BETFAIR_APPKEY") ?? string.Empty;
        var session = Environment.GetEnvironmentVariable("BETFAIR_SESSION") ?? string.Empty;
        var authMsg = JsonSerializer.Serialize(new { op = "authentication", appKey, session });
        await _ws.SendAsync(Encoding.UTF8.GetBytes(authMsg), WebSocketMessageType.Text, true, ct);
    }
    /// <summary>
    /// Disconnect from the stream (no-op in stub).
    /// </summary>
    public async Task DisconnectAsync(CancellationToken ct)
    {
        if (_ws is { State: WebSocketState.Open })
        {
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", ct);
        }
        _ws?.Dispose();
        _ws = null;
    }

    /// <summary>
    /// Reads messages from Betfair Stream API and maps minimal fields to MarketSnapshot.
    /// Requires env BETFAIR_APPKEY and BETFAIR_SESSION, and a prior subscription message from elsewhere.
    /// </summary>
    public async IAsyncEnumerable<MarketSnapshot> ReadSnapshotsAsync([EnumeratorCancellation] CancellationToken ct)
    {
        if (_ws == null || _ws.State != WebSocketState.Open)
            yield break;

        var buffer = new byte[64 * 1024];
        while (!ct.IsCancellationRequested && _ws.State == WebSocketState.Open)
        {
            var result = await _ws.ReceiveAsync(buffer, ct);
            if (result.MessageType == WebSocketMessageType.Close) break;

            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            // Minimal extract: timestamp and marketId if present
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("mc", out var mcArray) && mcArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var mc in mcArray.EnumerateArray())
                {
                    var marketIdStr = mc.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                    var tsMs = root.TryGetProperty("clk", out var clkProp) ? clkProp.GetString() : null;
                    var marketId = marketIdStr != null ? new MarketId(marketIdStr) : new MarketId("unknown");
                    var snapshot = new MarketSnapshot
                    {
                        MarketId = marketId,
                        EventName = "",
                        EventType = "",
                        MarketType = "",
                        Timestamp = DateTimeOffset.UtcNow,
                        StartTime = null,
                        SecondsToStart = null,
                        TotalMatched = null,
                        Runners = new List<RunnerSnapshot>()
                    };
                    yield return snapshot;
                }
            }
        }
    }
}
