namespace AIBettingExecutor.Configuration;

/// <summary>
/// Configuration for Executor service.
/// </summary>
public class ExecutorConfiguration
{
    /// <summary>
    /// Betfair API settings.
    /// </summary>
    public BetfairSettings Betfair { get; init; } = new();

    /// <summary>
    /// Redis connection settings.
    /// </summary>
    public RedisSettings Redis { get; init; } = new();

    /// <summary>
    /// Order manager settings.
    /// </summary>
    public OrderManagerSettings OrderManager { get; init; } = new();

    /// <summary>
    /// Signal processor settings.
    /// </summary>
    public SignalProcessorSettings SignalProcessor { get; init; } = new();

    /// <summary>
    /// Risk management settings.
    /// </summary>
    public RiskManagementSettings Risk { get; init; } = new();

    /// <summary>
    /// Trading settings.
    /// </summary>
    public TradingSettings Trading { get; init; } = new();

    /// <summary>
    /// Prometheus metrics port.
    /// </summary>
    public int PrometheusMetricsPort { get; init; } = 5003;

    /// <summary>
    /// API HTTP port for control endpoints.
    /// </summary>
    public int ApiPort { get; init; } = 5004;
}

/// <summary>
/// Betfair API configuration.
/// </summary>
public class BetfairSettings
{
    public string AppKey { get; init; } = string.Empty;
    public string CertificatePath { get; init; } = string.Empty;
    public string CertificatePassword { get; init; } = string.Empty;
    public int ApiTimeoutSeconds { get; init; } = 30;
    public int ReauthenticationIntervalHours { get; init; } = 6;
}

/// <summary>
/// Redis configuration.
/// </summary>
public class RedisSettings
{
    public string ConnectionString { get; init; } = "localhost:6379";
}

/// <summary>
/// Order manager configuration.
/// </summary>
public class OrderManagerSettings
{
    public int UnmatchedOrderTimeoutSeconds { get; init; } = 30;
    public int StatusCheckIntervalSeconds { get; init; } = 5;
    public int MaxOrdersPerCheck { get; init; } = 10;
}

/// <summary>
/// Signal processor configuration.
/// </summary>
public class SignalProcessorSettings
{
    public bool SubscribeToSurebetSignals { get; init; } = true;
    public bool SubscribeToStrategySignals { get; init; } = true;
    public int MaxSignalAgeSeconds { get; init; } = 60;
}

/// <summary>
/// Risk management configuration.
/// </summary>
public class RiskManagementSettings
{
    public bool Enabled { get; init; } = true;
    public bool CircuitBreakerEnabled { get; init; } = true;
    public decimal MaxStakePerOrder { get; init; } = 100m;
    public decimal MaxExposurePerMarket { get; init; } = 500m;
    public decimal MaxExposurePerSelection { get; init; } = 200m;
    public decimal MaxDailyLoss { get; init; } = 500m;
    public decimal MaxRiskPerTradePercent { get; init; } = 0.02m; // 2%
    public int CircuitBreakerFailureThreshold { get; init; } = 5;
    public int CircuitBreakerWindowMinutes { get; init; } = 10;
}

/// <summary>
/// Trading configuration.
/// </summary>
public class TradingSettings
{
    public decimal CommissionRate { get; init; } = 0.05m; // 5%
    public decimal MinOdds { get; init; } = 1.01m;
    public decimal MaxOdds { get; init; } = 1000m;
    public decimal MinStake { get; init; } = 2m;
    public bool EnablePaperTrading { get; init; } = false;
    public bool UseMockBetfair { get; init; } = false;
}
