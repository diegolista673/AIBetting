using AIBettingBlazorDashboard.Components;
using AIBettingBlazorDashboard.Configuration;
using AIBettingBlazorDashboard.Services;
using AIBettingBlazorDashboard.Hubs;
using AIBettingBlazorDashboard.BackgroundServices;
using MudBlazor.Services;
using Serilog;
using StackExchange.Redis;
using AIBettingCore;

namespace AIBettingBlazorDashboard
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure Serilog rolling file
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(path: "logs/blazordashboard-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7, fileSizeLimitBytes: 10_000_000)
                .WriteTo.Console()
                .CreateLogger();
            builder.Host.UseSerilog();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddMudServices();

            // SignalR for real-time updates with JSON config for NaN/Infinity
            builder.Services.AddSignalR()
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.NumberHandling = 
                        System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals;
                });

            // HTTP Clients
            builder.Services.AddHttpClient("Prometheus");
            builder.Services.AddHttpClient("Executor");

            // Configure Monitoring settings
            builder.Services.Configure<MonitoringConfiguration>(
                builder.Configuration.GetSection("Monitoring"));

            // Custom services
            builder.Services.AddSingleton<PrometheusService>();
            builder.Services.AddSingleton<ExecutorApiService>();
            
            // Background service for streaming metrics
            builder.Services.AddHostedService<MetricsStreamerService>();

            // Redis connection (optional, for future use)
            var redisConnectionString = builder.Configuration["Redis:ConnectionString"] ?? "localhost:16379,abortConnect=false";
            builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
                ConnectionMultiplexer.Connect(redisConnectionString));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseHttpsRedirection();

            app.UseAntiforgery();

            app.MapStaticAssets();
            
            // Map SignalR hub
            app.MapHub<MetricsHub>("/metricshub");
            
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            Log.Information("✅ Blazor Dashboard starting on port {Port}", builder.Configuration["ASPNETCORE_URLS"] ?? "http://localhost:5000");
            app.Run();
            Log.Information("Blazor Dashboard stopped");
        }
    }
}
