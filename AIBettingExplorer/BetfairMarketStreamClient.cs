using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AIBettingCore.Interfaces;
using AIBettingCore.Models;
using Serilog;

namespace AIBettingExplorer;

/// <summary>
/// Mock implementation of Betfair Stream API for development without real credentials.
/// Generates realistic market snapshots simulating Premier League matches.
/// </summary>
public class BetfairMarketStreamClient : IMarketStreamClient
{
    private bool _connected;
    private readonly Random _random = new();
    private readonly List<MockMarket> _markets = [];

    // Configurazione mock
    private readonly int _updateIntervalMs = 2000; // Snapshot ogni 2 secondi
    private readonly int _marketCount = 5; // Numero mercati simulati
    private readonly int _runnersPerMarket = 3; // Home, Draw, Away

    private class MockMarket
    {
        public required string MarketId { get; init; }
        public required string EventName { get; init; }
        public required DateTimeOffset StartTime { get; init; }
        public decimal TotalMatched { get; set; }
        public Dictionary<string, MockRunner> Runners { get; init; } = [];
    }


    private class MockRunner
    {
        public required string SelectionId { get; init; }
        public required string Name { get; init; }
        public decimal BackPrice { get; set; }
        public decimal LayPrice { get; set; }
        public decimal BackSize { get; set; }
        public decimal LaySize { get; set; }
    }


    /// <summary>
    /// Initialize mock connection and generate sample markets.
    /// </summary>
    public Task ConnectAsync(CancellationToken ct)
    {
        Log.Warning("🧪 MOCK MODE: Using simulated Betfair Stream API (no real connection)");
        Log.Information("Mock config: {Interval}ms interval, {Markets} markets, {Runners} runners each",
            _updateIntervalMs, _marketCount, _runnersPerMarket);

        _connected = true;
        InitializeMockMarkets();

        return Task.CompletedTask;
    }


    /// <summary>
    /// Disconnect mock stream.
    /// </summary>
    public Task DisconnectAsync(CancellationToken ct)
    {
        _connected = false;
        Log.Information("Mock stream disconnected");
        return Task.CompletedTask;
    }


    /// <summary>
    /// Generate realistic market snapshots with dynamic price movements.
    /// Simulates Premier League matches with evolving liquidity and odds.
    /// </summary>
    public async IAsyncEnumerable<MarketSnapshot> ReadSnapshotsAsync([EnumeratorCancellation] CancellationToken ct)
    {
        if (!_connected)
        {
            Log.Warning("Mock stream not connected");
            yield break;
        }

        Log.Information("🎲 Mock stream started - generating snapshots every {Interval}ms", _updateIntervalMs);

        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(_updateIntervalMs, ct);

            // Update tutti i mercati e genera snapshots
            foreach (var market in _markets)
            {
                UpdateMarketData(market);
                var snapshot = CreateSnapshot(market);
                Log.Debug("MOCK: Generated snapshot for {EventName} | Matched: £{Matched:N0}",
                    snapshot.EventName, snapshot.TotalMatched);
                yield return snapshot;
            }
        }
    }


    /// <summary>
    /// Initialize mock markets with realistic Premier League fixtures.
    /// </summary>
    private void InitializeMockMarkets()
    {
        var fixtures = new[]
        {
            ("Arsenal vs Manchester City", 1.90m, 3.40m, 4.20m),
            ("Liverpool vs Chelsea", 2.10m, 3.30m, 3.80m),
            ("Manchester United vs Tottenham", 2.50m, 3.20m, 3.10m),
            ("Newcastle vs Brighton", 2.30m, 3.30m, 3.50m),
            ("Aston Villa vs West Ham", 2.00m, 3.40m, 4.00m)
        };

        for (int i = 0; i < Math.Min(_marketCount, fixtures.Length); i++)
        {
            var (eventName, homeOdds, drawOdds, awayOdds) = fixtures[i];
            var marketId = $"1.{200000000 + i}";
            var startTime = DateTimeOffset.UtcNow.AddMinutes(_random.Next(30, 180));

            var market = new MockMarket
            {
                MarketId = marketId,
                EventName = eventName,
                StartTime = startTime,
                TotalMatched = _random.Next(50000, 200000)
            };

            // Home Win
            market.Runners[$"{marketId}_H"] = new MockRunner
            {
                SelectionId = $"{marketId}_H",
                Name = eventName.Split(" vs ")[0],
                BackPrice = homeOdds,
                LayPrice = homeOdds + 0.02m,
                BackSize = _random.Next(500, 3000),
                LaySize = _random.Next(400, 2500)
            };

            // Draw
            market.Runners[$"{marketId}_D"] = new MockRunner
            {
                SelectionId = $"{marketId}_D",
                Name = "The Draw",
                BackPrice = drawOdds,
                LayPrice = drawOdds + 0.02m,
                BackSize = _random.Next(300, 2000),
                LaySize = _random.Next(300, 2000)
            };

            // Away Win
            market.Runners[$"{marketId}_A"] = new MockRunner
            {
                SelectionId = $"{marketId}_A",
                Name = eventName.Split(" vs ")[1],
                BackPrice = awayOdds,
                LayPrice = awayOdds + 0.02m,
                BackSize = _random.Next(400, 2800),
                LaySize = _random.Next(350, 2300)
            };

            _markets.Add(market);
            Log.Information("Mock market created: {EventName} (Market: {MarketId})", eventName, marketId);
        }
    }


    /// <summary>
    /// Simulate realistic market dynamics: price movements, liquidity growth, spreads.
    /// </summary>
    private void UpdateMarketData(MockMarket market)
    {
        // Incrementa liquidità totale (simula betting activity)
        market.TotalMatched += _random.Next(1000, 5000);

        // Simula movimenti prezzi per ogni runner
        foreach (var runner in market.Runners.Values)
        {
            // Volatilità prezzi: ±2% random walk
            var priceChange = (decimal)(_random.NextDouble() * 0.04 - 0.02);
            runner.BackPrice = Math.Max(1.01m, Math.Min(100m, runner.BackPrice * (1 + priceChange)));
            runner.LayPrice = runner.BackPrice + (decimal)(_random.NextDouble() * 0.05 + 0.01); // Spread 1-5%

            // Round ai tick Betfair (0.01 per prezzi < 2, etc.)
            runner.BackPrice = Math.Round(runner.BackPrice, 2);
            runner.LayPrice = Math.Round(runner.LayPrice, 2);

            // Aggiorna size disponibili (simula order book depth)
            runner.BackSize += _random.Next(-200, 500);
            runner.LaySize += _random.Next(-200, 500);
            runner.BackSize = Math.Max(100, runner.BackSize);
            runner.LaySize = Math.Max(100, runner.LaySize);
        }
    }


    /// <summary>
    /// Convert mock market to MarketSnapshot domain model.
    /// </summary>
    private MarketSnapshot CreateSnapshot(MockMarket market)
    {
        var now = DateTimeOffset.UtcNow;
        var secondsToStart = (int)(market.StartTime - now).TotalSeconds;

        var runners = new List<RunnerSnapshot>();
        foreach (var (_, mockRunner) in market.Runners)
        {
            runners.Add(new RunnerSnapshot
            {
                SelectionId = new SelectionId(mockRunner.SelectionId),
                RunnerName = mockRunner.Name,
                LastPriceTraded = mockRunner.BackPrice,
                TotalMatched = mockRunner.BackSize + mockRunner.LaySize,
                AvailableToBack = new List<PriceSize>
                {
                    new(mockRunner.BackPrice, mockRunner.BackSize),
                    new(Math.Round(mockRunner.BackPrice * 0.98m, 2), mockRunner.BackSize * 0.7m),
                    new(Math.Round(mockRunner.BackPrice * 0.96m, 2), mockRunner.BackSize * 0.5m)
                },
                AvailableToLay = new List<PriceSize>
                {
                    new(mockRunner.LayPrice, mockRunner.LaySize),
                    new(Math.Round(mockRunner.LayPrice * 1.02m, 2), mockRunner.LaySize * 0.7m),
                    new(Math.Round(mockRunner.LayPrice * 1.04m, 2), mockRunner.LaySize * 0.5m)
                }
            });
        }

        return new MarketSnapshot
        {
            MarketId = new MarketId(market.MarketId),
            EventName = market.EventName,
            EventType = "Soccer",
            MarketType = "MATCH_ODDS",
            Timestamp = now,
            StartTime = market.StartTime,
            SecondsToStart = Math.Max(0, secondsToStart),
            TotalMatched = market.TotalMatched,
            Runners = runners
        };
    }
}
