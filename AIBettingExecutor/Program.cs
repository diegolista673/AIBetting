
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

    var builder = WebApplication.CreateBuilder(args);

    // Configure Kestrel to listen on specific port
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(5004); // API port
    });

    // Add Serilog
    builder.Host.UseSerilog();

    // Load Executor configuration
    builder.Services.Configure<ExecutorConfiguration>(builder.Configuration.GetSection("Executor"));

    var config = builder.Configuration.GetSection("Executor").Get<ExecutorConfiguration>() 
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

    // Add services
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowDashboard", policy =>
        {
            policy.WithOrigins("http://localhost:5000")
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    // Start Prometheus metrics server using Kestrel
    var metricsServer = new KestrelMetricServer(port: config.PrometheusMetricsPort);
    metricsServer.Start();
    Log.Information("📊 Prometheus metrics server started on port {Port}", config.PrometheusMetricsPort);

    // Initialize Redis
    Log.Information("Connecting to Redis...");
    var redis = await ConnectionMultiplexer.ConnectAsync(config.Redis.ConnectionString);
    builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
    Log.Information("✅ Redis connected");

    // Initialize components - use logger from DI after app is built
    IBetfairClient betfairClient = config.Trading.UseMockBetfair
        ? new MockBetfairClient(null!)  // Logger will be injected later
        : new BetfairClient(
            config.Betfair.AppKey,
            config.Betfair.CertificatePath,
            config.Betfair.CertificatePassword,
            null); // Logger will be injected later

    builder.Services.AddSingleton(betfairClient);

    var orderManager = new OrderManager(
        betfairClient,
        new OrderManagerConfiguration
        {
            UnmatchedOrderTimeoutSeconds = config.OrderManager.UnmatchedOrderTimeoutSeconds,
            StatusCheckIntervalSeconds = config.OrderManager.StatusCheckIntervalSeconds,
            MaxOrdersPerCheck = config.OrderManager.MaxOrdersPerCheck
        },
        null); // Logger will be injected later

    builder.Services.AddSingleton(orderManager);

    var signalProcessor = new SignalProcessor(
        redis,
        new SignalProcessorConfiguration
        {
            SubscribeToSurebetSignals = config.SignalProcessor.SubscribeToSurebetSignals,
            SubscribeToStrategySignals = config.SignalProcessor.SubscribeToStrategySignals,
            MaxSignalAgeSeconds = config.SignalProcessor.MaxSignalAgeSeconds
        },
        null); // Logger will be injected later

    builder.Services.AddSingleton(signalProcessor);

    // Initialize Risk Manager
    var riskManager = new RedisRiskManager(redis);
    builder.Services.AddSingleton<IRiskManager>(riskManager);

    var riskValidator = new RiskValidator(
        riskManager,
        new RiskValidatorConfiguration
        {
            Enabled = config.Risk.Enabled,
            CircuitBreakerEnabled = config.Risk.CircuitBreakerEnabled
        },
        null); // Logger will be injected later

    builder.Services.AddSingleton(riskValidator);

    var tradeLogger = new TradeLogger(
        redis,
        config.Trading.CommissionRate,
        null); // Logger will be injected later

    builder.Services.AddSingleton(tradeLogger);

    // Initialize ExecutorService as hosted service
    var executorService = new ExecutorService(
        betfairClient,
        orderManager,
        signalProcessor,
        riskValidator,
        tradeLogger,
        config,
        null); // Logger will be injected later

    builder.Services.AddSingleton(executorService);
    builder.Services.AddHostedService<ExecutorBackgroundService>();

    var app = builder.Build();

    // Configure middleware
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "AIBetting Executor API v1");
            c.RoutePrefix = "swagger"; // Set Swagger UI at /swagger
        });
    }

    app.UseCors("AllowDashboard");
    app.UseAuthorization();

    // Add explicit root endpoint
    app.MapGet("/", () => Results.Json(new
    {
        service = "AIBetting Executor API",
        version = "1.0.0",
        status = "running",
        endpoints = new
        {
            swagger = "/swagger",
            health = "/api/health",
            circuitBreaker = "/api/circuit-breaker",
            trading = "/api/trading",
            config = "/api/config"
        }
    })).ExcludeFromDescription();

    // Add health check endpoint
    app.MapGet("/api/health", () => Results.Ok(new
    {
        status = "healthy",
        timestamp = DateTime.UtcNow,
        service = "executor"
    })).ExcludeFromDescription();

    app.MapControllers();

    Log.Information("✅ Executor API ready on http://localhost:5004");
    Log.Information("✅ Swagger UI: http://localhost:5004/swagger");

    await app.RunAsync();

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

// Background service wrapper for ExecutorService
public class ExecutorBackgroundService : BackgroundService
{
    private readonly ExecutorService _executorService;
    private readonly ILogger<ExecutorBackgroundService> _logger;

    public ExecutorBackgroundService(ExecutorService executorService, ILogger<ExecutorBackgroundService> logger)
    {
        _executorService = executorService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExecutorBackgroundService starting");
        await _executorService.RunAsync(stoppingToken);
        _logger.LogInformation("ExecutorBackgroundService stopped");
    }
}
