using System.Net.Http.Json;

namespace AIBettingBlazorDashboard.Services;

public class ExecutorApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExecutorApiService> _logger;
    private readonly string _executorApiUrl;

    public ExecutorApiService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ExecutorApiService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("Executor");
        _executorApiUrl = configuration["Services:ExecutorApiUrl"] ?? "http://localhost:5003";
        _logger = logger;
    }

    public async Task<bool> ResetCircuitBreakerAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync($"{_executorApiUrl}/api/circuit-breaker/reset", null);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Circuit breaker reset successful");
                return true;
            }

            _logger.LogWarning("Circuit breaker reset failed: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting circuit breaker");
            return false;
        }
    }

    public async Task<bool> PauseTradingAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync($"{_executorApiUrl}/api/trading/pause", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing trading");
            return false;
        }
    }

    public async Task<bool> ResumeTradingAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync($"{_executorApiUrl}/api/trading/resume", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming trading");
            return false;
        }
    }

    public async Task<RiskConfig?> GetRiskConfigAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<RiskConfig>($"{_executorApiUrl}/api/config/risk");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting risk config");
            return null;
        }
    }

    public async Task<bool> UpdateRiskConfigAsync(RiskConfig config)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{_executorApiUrl}/api/config/risk", config);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating risk config");
            return false;
        }
    }
}

public class RiskConfig
{
    public bool Enabled { get; set; }
    public decimal MaxStakePerOrder { get; set; }
    public decimal MaxExposurePerMarket { get; set; }
    public decimal MaxExposurePerSelection { get; set; }
    public decimal MaxDailyLoss { get; set; }
    public bool CircuitBreakerEnabled { get; set; }
    public int CircuitBreakerFailureThreshold { get; set; }
    public int CircuitBreakerWindowMinutes { get; set; }
}
