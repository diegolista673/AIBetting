using AIBettingBlazorDashboard.Hubs;
using AIBettingBlazorDashboard.Services;
using Microsoft.AspNetCore.SignalR;

namespace AIBettingBlazorDashboard.BackgroundServices;

public class MetricsStreamerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<MetricsHub> _hubContext;
    private readonly ILogger<MetricsStreamerService> _logger;
    private readonly int _intervalSeconds;

    public MetricsStreamerService(
        IServiceProvider serviceProvider,
        IHubContext<MetricsHub> hubContext,
        IConfiguration configuration,
        ILogger<MetricsStreamerService> logger)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _logger = logger;
        _intervalSeconds = configuration.GetValue<int>("Monitoring:StreamIntervalSeconds", 5);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MetricsStreamerService started with {IntervalSeconds}s interval", _intervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var prometheusService = scope.ServiceProvider.GetRequiredService<PrometheusService>();

                var metrics = await prometheusService.GetSystemMetricsAsync();

                // Broadcast to all connected clients
                await _hubContext.Clients.All.SendAsync("UpdateMetrics", metrics, stoppingToken);

                _logger.LogDebug("Metrics broadcasted to clients");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error streaming metrics");
            }

            await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), stoppingToken);
        }

        _logger.LogInformation("MetricsStreamerService stopped");
    }
}
