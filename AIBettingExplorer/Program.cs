using AIBettingExplorer;
using AIBettingCore.Interfaces;
using StackExchange.Redis;
using System.Text.Json;
using Serilog;
using Microsoft.Extensions.Logging;

// Configure Serilog rolling file sink
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(path: "logs/explorer-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7, fileSizeLimitBytes: 10_000_000)
    .CreateLogger();

try
{
    Log.Information("Explorer starting");

    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

    var connString = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "localhost:6379,abortConnect=false";
    Log.Information("Connecting to Redis: {Conn}", connString);
    var conn = await ConnectionMultiplexer.ConnectAsync(connString);

    IMarketStreamClient stream = new BetfairMarketStreamClient();
    ICacheBus bus = new RedisCacheBus(conn, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    var explorer = new ExplorerService(stream, bus);

    await explorer.RunAsync(cts.Token);

    Log.Information("Explorer stopped");
}
catch (Exception ex)
{
    Log.Error(ex, "Explorer fatal error");
}
finally
{
    Log.CloseAndFlush();
}
