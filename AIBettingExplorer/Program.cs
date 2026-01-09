using AIBettingExplorer;
using AIBettingCore.Interfaces;
using StackExchange.Redis;
using System.Text.Json;
using Serilog;
using Prometheus; // ← Aggiungi questo

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(path: "logs/explorer-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7, fileSizeLimitBytes: 10_000_000)
    .CreateLogger();

// Avvia metrics server
var metricServer = new MetricServer(port: 5001);
metricServer.Start();
Log.Information("Prometheus metrics exposed on http://localhost:5001/metrics");

// Define Prometheus metrics
var priceUpdatesCounter = Metrics.CreateCounter(
    "aibetting_price_updates_total",
    "Total price updates from Betfair stream"
);

var streamLatencyHistogram = Metrics.CreateHistogram(
    "aibetting_stream_latency_seconds",
    "Latency from stream to Redis",
    new HistogramConfiguration
    {
        Buckets = Histogram.ExponentialBuckets(0.001, 2, 10) // 1ms to ~1sec
    }
);

try
{
    Log.Information("Explorer starting");

    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

    var connString = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "localhost:6379,abortConnect=false";
    Log.Information("Connecting to Redis: {Conn}", connString);
    var conn = await ConnectionMultiplexer.ConnectAsync(connString);

    IMarketStreamClient stream = new BetfairMarketStreamClient();
    ICacheBus bus = new RedisCacheBus(conn, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    var explorer = new ExplorerService(stream, bus);

    await explorer.RunAsync(cts.Token);

    Log.Information("Explorer stopped");
    metricServer.Stop();
}
catch (Exception ex)
{
    Log.Error(ex, "Explorer fatal error");
}
finally
{
    Log.CloseAndFlush();
}
