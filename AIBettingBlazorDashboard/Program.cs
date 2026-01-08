using AIBettingBlazorDashboard.Components;
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
                .CreateLogger();
            builder.Host.UseSerilog();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddMudServices();

            // Redis connection for reading live data
            //builder.Services.AddSingleton(async (sp) =>
            //{
            //    var connString = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "localhost:6379,abortConnect=false";
            //    return await ConnectionMultiplexer.ConnectAsync(connString);
            //});

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
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            Log.Information("Blazor Dashboard starting");
            app.Run();
            Log.Information("Blazor Dashboard stopped");
        }
    }
}
