using System.Net.Http.Json;
using System.Text.Json;

namespace AIBettingBlazorDashboard.Services;

public class PrometheusService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PrometheusService> _logger;
    private readonly string _prometheusUrl;

    public PrometheusService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<PrometheusService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("Prometheus");
        _prometheusUrl = configuration["Monitoring:PrometheusUrl"] ?? "http://localhost:9090";
        _logger = logger;
    }

    public async Task<double?> GetMetricValueAsync(string query)
    {
        try
        {
            var url = $"{_prometheusUrl}/api/v1/query?query={Uri.EscapeDataString(query)}";
            var response = await _httpClient.GetFromJsonAsync<PrometheusQueryResponse>(url);

            if (response?.Data?.Result != null && response.Data.Result.Count > 0)
            {
                var valueElement = response.Data.Result[0].Value;
                if (valueElement != null && valueElement.Count >= 2)
                {
                    var value = valueElement[1].ToString();
                    if (double.TryParse(value, out var result))
                    {
                        return result;
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying Prometheus: {Query}", query);
            return null;
        }
    }

    public async Task<List<PrometheusDataPoint>> GetMetricRangeAsync(string query, int minutes = 5)
    {
        try
        {
            var end = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var start = end - (minutes * 60);
            var step = Math.Max(15, minutes * 2); // 15s minimum step

            var url = $"{_prometheusUrl}/api/v1/query_range?query={Uri.EscapeDataString(query)}&start={start}&end={end}&step={step}s";
            var response = await _httpClient.GetFromJsonAsync<PrometheusQueryResponse>(url);

            var dataPoints = new List<PrometheusDataPoint>();

            if (response?.Data?.Result != null && response.Data.Result.Count > 0)
            {
                var values = response.Data.Result[0].Values;
                if (values != null)
                {
                    foreach (var valueArray in values)
                    {
                        if (valueArray.Count >= 2)
                        {
                            var timestamp = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(valueArray[0].GetDouble())).DateTime;
                            var valueStr = valueArray[1].ToString();
                            if (double.TryParse(valueStr, out var value))
                            {
                                dataPoints.Add(new PrometheusDataPoint(timestamp, value));
                            }
                        }
                    }
                }
            }

            return dataPoints;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying Prometheus range: {Query}", query);
            return new List<PrometheusDataPoint>();
        }
    }

    public async Task<SystemMetrics> GetSystemMetricsAsync()
    {
        var metrics = new SystemMetrics();

        // Service status (1 = UP, 0 = DOWN)
        metrics.ExplorerUp = await GetMetricValueAsync("up{job=\"aibetting-explorer\"}") == 1;
        metrics.AnalystUp = await GetMetricValueAsync("up{job=\"aibetting-analyst\"}") == 1;
        metrics.ExecutorUp = await GetMetricValueAsync("up{job=\"aibetting-executor\"}") == 1;

        // Executor metrics - sanitize to avoid Infinity/NaN
        metrics.AccountBalance = SanitizeValue(await GetMetricValueAsync("aibetting_executor_account_balance") ?? 0);
        metrics.CurrentExposure = SanitizeValue(await GetMetricValueAsync("aibetting_executor_current_exposure") ?? 0);
        metrics.CircuitBreakerTriggered = await GetMetricValueAsync("aibetting_executor_circuit_breaker_status") == 1;

        // Rates (per minute) - sanitize to avoid Infinity/NaN
        metrics.OrdersPerMinute = SanitizeValue(await GetMetricValueAsync("rate(aibetting_executor_orders_placed_total[1m]) * 60") ?? 0);
        metrics.SignalsPerMinute = SanitizeValue(await GetMetricValueAsync("rate(aibetting_analyst_signals_generated_total[1m]) * 60") ?? 0);
        metrics.PriceUpdatesPerMinute = SanitizeValue(await GetMetricValueAsync("rate(aibetting_price_updates_total[1m]) * 60") ?? 0);

        // Latencies (P99) - sanitize to avoid Infinity/NaN
        metrics.ExplorerLatencyP99 = SanitizeValue(await GetMetricValueAsync("histogram_quantile(0.99, rate(aibetting_processing_latency_seconds_bucket[5m]))") ?? 0);
        metrics.AnalystLatencyP99 = SanitizeValue(await GetMetricValueAsync("histogram_quantile(0.99, rate(aibetting_analyst_processing_latency_seconds_bucket[5m]))") ?? 0);
        metrics.ExecutorLatencyP99 = SanitizeValue(await GetMetricValueAsync("histogram_quantile(0.99, rate(aibetting_executor_order_execution_latency_seconds_bucket[5m]))") ?? 0);

        // Totals
        metrics.TotalOrdersPlaced = (long)(await GetMetricValueAsync("aibetting_executor_orders_placed_total") ?? 0);
        metrics.TotalOrdersMatched = (long)(await GetMetricValueAsync("aibetting_executor_orders_matched_total") ?? 0);
        metrics.TotalSignalsGenerated = (long)(await GetMetricValueAsync("aibetting_analyst_signals_generated_total") ?? 0);

        return metrics;
    }

    /// <summary>
    /// Sanitizes double values to prevent Infinity/NaN from being serialized to JSON
    /// </summary>
    private static double SanitizeValue(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0.0;
        }
        return value;
    }
}

public record PrometheusDataPoint(DateTime Timestamp, double Value);

public class SystemMetrics
{
    public bool ExplorerUp { get; set; }
    public bool AnalystUp { get; set; }
    public bool ExecutorUp { get; set; }
    public double AccountBalance { get; set; }
    public double CurrentExposure { get; set; }
    public bool CircuitBreakerTriggered { get; set; }
    public double OrdersPerMinute { get; set; }
    public double SignalsPerMinute { get; set; }
    public double PriceUpdatesPerMinute { get; set; }
    public double ExplorerLatencyP99 { get; set; }
    public double AnalystLatencyP99 { get; set; }
    public double ExecutorLatencyP99 { get; set; }
    public long TotalOrdersPlaced { get; set; }
    public long TotalOrdersMatched { get; set; }
    public long TotalSignalsGenerated { get; set; }
}

// Prometheus API response models
public class PrometheusQueryResponse
{
    public string? Status { get; set; }
    public PrometheusData? Data { get; set; }
}

public class PrometheusData
{
    public string? ResultType { get; set; }
    public List<PrometheusResult>? Result { get; set; }
}

public class PrometheusResult
{
    public Dictionary<string, string>? Metric { get; set; }
    public List<JsonElement>? Value { get; set; }
    public List<List<JsonElement>>? Values { get; set; }
}
