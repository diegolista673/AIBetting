using Microsoft.AspNetCore.SignalR;

namespace AIBettingBlazorDashboard.Hubs;

public class MetricsHub : Hub
{
    private readonly ILogger<MetricsHub> _logger;

    public MetricsHub(ILogger<MetricsHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task Subscribe(string metricName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, metricName);
        _logger.LogInformation("Client {ConnectionId} subscribed to {MetricName}", Context.ConnectionId, metricName);
    }

    public async Task Unsubscribe(string metricName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, metricName);
        _logger.LogInformation("Client {ConnectionId} unsubscribed from {MetricName}", Context.ConnectionId, metricName);
    }
}
