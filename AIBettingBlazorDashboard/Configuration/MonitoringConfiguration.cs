namespace AIBettingBlazorDashboard.Configuration;

/// <summary>
/// Configuration for Grafana and Prometheus monitoring integration
/// </summary>
public class MonitoringConfiguration
{
    public string GrafanaBaseUrl { get; set; } = "http://localhost:3000";
    public string PrometheusBaseUrl { get; set; } = "http://localhost:9090";
    public string ExplorerMetricsUrl { get; set; } = "http://localhost:5001/metrics";
    public string DefaultDashboard { get; set; } = "explorer";
    public string AutoRefreshInterval { get; set; } = "5s";
    public string DefaultTimeRange { get; set; } = "15m";
    public Dictionary<string, DashboardInfo> Dashboards { get; set; } = new();
}

public class DashboardInfo
{
    public string Uid { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
