using Prometheus;

namespace AIBettingExecutor.Metrics;

/// <summary>
/// Prometheus metrics for executor service.
/// </summary>
public static class ExecutorMetrics
{
    // Orders
    public static readonly Counter OrdersPlaced = Prometheus.Metrics.CreateCounter(
        "aibetting_executor_orders_placed_total",
        "Total orders placed on Betfair exchange",
        new CounterConfiguration { LabelNames = new[] { "side", "market_type" } }
    );

    public static readonly Counter OrdersMatched = Prometheus.Metrics.CreateCounter(
        "aibetting_executor_orders_matched_total",
        "Total orders fully matched",
        new CounterConfiguration { LabelNames = new[] { "side" } }
    );

    public static readonly Counter OrdersCancelled = Prometheus.Metrics.CreateCounter(
        "aibetting_executor_orders_cancelled_total",
        "Total orders cancelled",
        new CounterConfiguration { LabelNames = new[] { "reason" } }
    );

    public static readonly Counter OrdersFailed = Prometheus.Metrics.CreateCounter(
        "aibetting_executor_orders_failed_total",
        "Total orders that failed to place",
        new CounterConfiguration { LabelNames = new[] { "reason" } }
    );

    public static readonly Gauge ActiveOrders = Prometheus.Metrics.CreateGauge(
        "aibetting_executor_active_orders",
        "Number of currently active orders"
    );

    // Signals
    public static readonly Counter SignalsReceived = Prometheus.Metrics.CreateCounter(
        "aibetting_executor_signals_received_total",
        "Total trading signals received",
        new CounterConfiguration { LabelNames = new[] { "strategy" } }
    );

    public static readonly Counter SignalsRejected = Prometheus.Metrics.CreateCounter(
        "aibetting_executor_signals_rejected_total",
        "Total signals rejected by risk validation",
        new CounterConfiguration { LabelNames = new[] { "reason" } }
    );

    // Execution
    public static readonly Histogram OrderExecutionLatency = Prometheus.Metrics.CreateHistogram(
        "aibetting_executor_order_execution_latency_seconds",
        "Time from signal received to order placed"
    );

    public static readonly Histogram OrderMatchLatency = Prometheus.Metrics.CreateHistogram(
        "aibetting_executor_order_match_latency_seconds",
        "Time from order placed to fully matched"
    );

    // Risk & Exposure
    public static readonly Gauge CurrentExposure = Prometheus.Metrics.CreateGauge(
        "aibetting_executor_current_exposure",
        "Current total exposure across all markets"
    );

    public static readonly Counter RiskViolations = Prometheus.Metrics.CreateCounter(
        "aibetting_executor_risk_violations_total",
        "Total risk limit violations",
        new CounterConfiguration { LabelNames = new[] { "violation_type" } }
    );

    public static readonly Gauge CircuitBreakerStatus = Prometheus.Metrics.CreateGauge(
        "aibetting_executor_circuit_breaker_status",
        "Circuit breaker status (0=open, 1=closed)"
    );

    // Trading Performance
    public static readonly Counter TotalStakeDeployed = Prometheus.Metrics.CreateCounter(
        "aibetting_executor_total_stake_deployed",
        "Total stake deployed across all orders"
    );

    public static readonly Gauge AccountBalance = Prometheus.Metrics.CreateGauge(
        "aibetting_executor_account_balance",
        "Current account balance"
    );

    public static readonly Gauge AvailableBalance = Prometheus.Metrics.CreateGauge(
        "aibetting_executor_available_balance",
        "Available balance for betting"
    );

    // System Health
    public static readonly Gauge BetfairConnectionStatus = Prometheus.Metrics.CreateGauge(
        "aibetting_executor_betfair_connection_status",
        "Betfair API connection status (0=disconnected, 1=connected)"
    );

    public static readonly Counter BetfairApiErrors = Prometheus.Metrics.CreateCounter(
        "aibetting_executor_betfair_api_errors_total",
        "Total Betfair API errors",
        new CounterConfiguration { LabelNames = new[] { "error_type" } }
    );

    public static readonly Histogram BetfairApiLatency = Prometheus.Metrics.CreateHistogram(
        "aibetting_executor_betfair_api_latency_seconds",
        "Betfair API request latency"
    );
}
