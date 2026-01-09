using AIBettingExplorer;
using AIBettingCore.Interfaces;
using StackExchange.Redis;
using System.Text.Json;
using Serilog;
using Prometheus;
using Microsoft.Extensions.Configuration;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(path: "logs/explorer-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7, fileSizeLimitBytes: 10_000_000)
    .CreateLogger();

// Load configuration from appsettings.json
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

try
{
    Log.Information("🚀 AIBettingExplorer starting");

    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

    // ✅ Leggi connection string da appsettings.json
    var connString = configuration["Redis:ConnectionString"] 
                     ?? throw new InvalidOperationException("Redis:ConnectionString not found in appsettings.json");
    
    // Mask password per logging
    var connStringMasked = connString.Contains("password=")
        ? System.Text.RegularExpressions.Regex.Replace(connString, @"password=[^,]+", "password=***")
        : connString;
    
    Log.Information("Connecting to Redis: {Conn}", connStringMasked);
    var conn = await ConnectionMultiplexer.ConnectAsync(connString);
    Log.Information("✅ Redis connected successfully");

    IMarketStreamClient stream = new BetfairMarketStreamClient();
    ICacheBus bus = new RedisCacheBus(conn, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    
    // ✅ Crea ExplorerService per inizializzare le metriche statiche
    var explorer = new ExplorerService(stream, bus);
    Log.Information("📊 ExplorerService initialized with Prometheus metrics");
    
    // ✅ Crea metrica di test usando DefaultRegistry esplicitamente
    var testMetric = Metrics.WithCustomRegistry(Metrics.DefaultRegistry).CreateCounter(
        "aibetting_startup_test", 
        "Test counter to verify metrics export"
    );
    testMetric.Inc();
    Log.Information("✅ Test metric 'aibetting_startup_test' created in DefaultRegistry");

    // ✅ Usa KestrelMetricServer con DefaultRegistry esplicito
    var metricServer = new KestrelMetricServer(port: 5001, registry: Metrics.DefaultRegistry);
    metricServer.Start();
    Log.Information("📊 Prometheus KestrelMetricServer started on http://localhost:5001/metrics");
    Log.Information("📊 Using Prometheus.NET DefaultRegistry");
    Log.Information("💡 Registered metrics:");
    Log.Information("   - aibetting_price_updates_total");
    Log.Information("   - aibetting_processing_latency_seconds");
    Log.Information("   - aibetting_startup_test");
    
    // Aspetta che il server si avvii completamente
    await Task.Delay(1000);

    Log.Information("💡 Monitoring URLs:");
    Log.Information("   • Prometheus metrics: http://localhost:5001/metrics");
    Log.Information("   • Prometheus UI: http://localhost:9090 (if running)");
    Log.Information("   • Grafana dashboard: http://localhost:3000 (if running)");

    // Avvia il loop principale di Explorer
    await explorer.RunAsync(cts.Token);

    Log.Information("Explorer stopped gracefully");
    metricServer.Stop();
}
catch (Exception ex)
{
    Log.Error(ex, "💥 Explorer fatal error");
}
finally
{
    await Log.CloseAndFlushAsync();
}
