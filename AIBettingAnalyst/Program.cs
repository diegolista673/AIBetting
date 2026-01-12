using AIBettingAnalyst;
using StackExchange.Redis;
using Serilog;
using Prometheus;
using Microsoft.Extensions.Configuration;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(path: "logs/analyst-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7, fileSizeLimitBytes: 10_000_000)
    .CreateLogger();

// Load configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

try
{
    Log.Information("🚀 AIBettingAnalyst starting");

    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

    // Load configuration
    var connString = configuration["Redis:ConnectionString"] 
                     ?? throw new InvalidOperationException("Redis:ConnectionString not found");
    var minSurebetProfit = decimal.Parse(configuration["Analyst:MinSurebetProfitPercent"] ?? "0.5");
    var wapLevels = int.Parse(configuration["Analyst:WAPLevels"] ?? "3");
    var metricsPort = int.Parse(configuration["Analyst:PrometheusMetricsPort"] ?? "5002");

    // Mask password for logging
    var connStringMasked = connString.Contains("password=")
        ? System.Text.RegularExpressions.Regex.Replace(connString, @"password=[^,]+", "password=***")
        : connString;
    
    Log.Information("Connecting to Redis: {Conn}", connStringMasked);
    var redis = await ConnectionMultiplexer.ConnectAsync(connString);
    Log.Information("✅ Redis connected successfully");

    // Create Analyst service
    var analyst = new AnalystService(
        redis,
        configuration,
        minSurebetProfit: minSurebetProfit,
        wapLevels: wapLevels
    );

    // Start Prometheus metrics server
    var metricServer = new KestrelMetricServer(port: metricsPort, registry: Metrics.DefaultRegistry);
    metricServer.Start();
    Log.Information("📊 Prometheus metrics server started on http://localhost:{Port}/metrics", metricsPort);
    Log.Information("💡 Registered metrics:");
    Log.Information("   - aibetting_analyst_snapshots_processed_total");
    Log.Information("   - aibetting_analyst_signals_generated_total");
    Log.Information("   - aibetting_analyst_surebets_found_total");
    Log.Information("   - aibetting_analyst_processing_latency_seconds");
    Log.Information("   - aibetting_analyst_average_expected_roi");

    await Task.Delay(1000);

    Log.Information("💡 Monitoring URLs:");
    Log.Information("   • Analyst metrics: http://localhost:{Port}/metrics", metricsPort);
    Log.Information("   • Prometheus UI: http://localhost:9090 (if running)");
    Log.Information("   • Grafana dashboard: http://localhost:3000 (if running)");

    // Start analyst main loop
    await analyst.RunAsync(cts.Token);

    Log.Information("Analyst stopped gracefully");
    metricServer.Stop();
}
catch (Exception ex)
{
    Log.Error(ex, "💥 Analyst fatal error");
}
finally
{
    await Log.CloseAndFlushAsync();
}
