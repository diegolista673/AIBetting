// See https://aka.ms/new-console-template for more information
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(path: "logs/executor-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7, fileSizeLimitBytes: 10_000_000)
    .CreateLogger();

try
{
    Log.Information("Executor starting");
    // TODO: wire bus and executor service
    Log.Information("Executor stopped");
}
catch (Exception ex)
{
    Log.Error(ex, "Executor fatal error");
}
finally
{
    Log.CloseAndFlush();
}

Console.WriteLine("Hello, World!");
