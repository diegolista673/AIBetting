
using AIBettingCore.Interfaces;
using AIBettingCore.Services;
using AIBettingExecutor;
using AIBettingExecutor.Accounting;
using AIBettingExecutor.BetfairAPI;
using AIBettingExecutor.Configuration;
using AIBettingExecutor.Interfaces;
using AIBettingExecutor.OrderManagement;
using AIBettingExecutor.RiskManagement;
using AIBettingExecutor.SignalProcessing;
using Microsoft.Extensions.Configuration;
using Prometheus;
using Serilog;
using StackExchange.Redis;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(path: "logs/executor-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7, fileSizeLimitBytes: 10_000_000)
    .CreateLogger();

try
{
    Log.Information("🚀 AIBetting Executor starting");
    Log.Information("=================================");

    // Load configuration
    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false)
        .AddEnvironmentVariables()
        .Build();

    var config = configuration.GetSection("Executor").Get<ExecutorConfiguration>() 
        ?? new ExecutorConfiguration();

    // Validate configuration only when not using mock client
    if (!config.Trading.UseMockBetfair)
    {
        if (string.IsNullOrEmpty(config.Betfair.AppKey))
        {
            Log.Error("❌ Betfair AppKey not configured");
            return;
        }

        if (string.IsNullOrEmpty(config.Betfair.CertificatePath))
        {
            Log.Error("❌ Betfair certificate path not configured");
            return;
        }
    }

    Log.Information("Configuration loaded:");
    Log.Information("  Betfair AppKey: {AppKey}", config.Betfair.AppKey);
    Log.Information("  Certificate: {Path}", config.Betfair.CertificatePath);
    Log.Information("  Redis: {Connection}", config.Redis.ConnectionString);
    Log.Information("  Risk Management: {Enabled}", config.Risk.Enabled ? "ENABLED" : "DISABLED");
    Log.Information("  Paper Trading: {Enabled}", config.Trading.EnablePaperTrading ? "ENABLED" : "DISABLED");
    Log.Information("  Use Mock Betfair: {UseMock}", config.Trading.UseMockBetfair ? "YES" : "NO");
    Log.Information("  Prometheus Port: {Port}", config.PrometheusMetricsPort);

    // Start Prometheus metrics server using Kestrel to avoid HttpListener ACL issues
    var metricsServer = new KestrelMetricServer(port: config.PrometheusMetricsPort);
    metricsServer.Start();
    Log.Information("📊 Prometheus metrics server started on port {Port}", config.PrometheusMetricsPort);

    // Initialize Redis
    Log.Information("Connecting to Redis...");
    var redis = await ConnectionMultiplexer.ConnectAsync(config.Redis.ConnectionString);
    Log.Information("✅ Redis connected");

    // Initialize components
    IBetfairClient betfairClient = config.Trading.UseMockBetfair
        ? new MockBetfairClient(Log.ForContext<MockBetfairClient>())
        : new BetfairClient(
            config.Betfair.AppKey,
            config.Betfair.CertificatePath,
            config.Betfair.CertificatePassword,
            Log.ForContext<BetfairClient>());

    var orderManager = new OrderManager(
        betfairClient,
        new OrderManagerConfiguration
        {
            UnmatchedOrderTimeoutSeconds = config.OrderManager.UnmatchedOrderTimeoutSeconds,
            StatusCheckIntervalSeconds = config.OrderManager.StatusCheckIntervalSeconds,
            MaxOrdersPerCheck = config.OrderManager.MaxOrdersPerCheck
        },
        Log.ForContext<OrderManager>());

    var signalProcessor = new SignalProcessor(
        redis,
        new SignalProcessorConfiguration
        {
            SubscribeToSurebetSignals = config.SignalProcessor.SubscribeToSurebetSignals,
            SubscribeToStrategySignals = config.SignalProcessor.SubscribeToStrategySignals,
            MaxSignalAgeSeconds = config.SignalProcessor.MaxSignalAgeSeconds
        },
        Log.ForContext<SignalProcessor>());

    // Initialize Risk Manager
    var riskManager = new RedisRiskManager(redis);

    var riskValidator = new RiskValidator(
        riskManager,
        new RiskValidatorConfiguration
        {
            Enabled = config.Risk.Enabled,
            CircuitBreakerEnabled = config.Risk.CircuitBreakerEnabled
        },
        Log.ForContext<RiskValidator>());

    var tradeLogger = new TradeLogger(
        redis,
        config.Trading.CommissionRate,
        Log.ForContext<TradeLogger>());

    // Initialize ExecutorService
    var executorService = new ExecutorService(
        betfairClient,
        orderManager,
        signalProcessor,
        riskValidator,
        tradeLogger,
        config,
        Log.ForContext<ExecutorService>());

    // Setup cancellation
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (sender, e) =>
    {
        e.Cancel = true;
        Log.Information("Shutdown requested...");
        cts.Cancel();
    };

    // Run executor
    await executorService.RunAsync(cts.Token);

    Log.Information("Executor stopped successfully");
}
catch (Exception ex)
{
    Log.Error(ex, "❌ Executor fatal error");
}
finally
{
    Log.CloseAndFlush();
}

Console.WriteLine("Hello, World!");
