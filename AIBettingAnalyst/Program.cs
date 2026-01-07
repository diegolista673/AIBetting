using Serilog;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(path: "logs/analyst-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7, fileSizeLimitBytes: 10_000_000)
    .CreateLogger();

try
{
    Log.Information("Analyst starting");
    // TODO: wire bus and analyst service
    Log.Information("Analyst stopped");
}
catch (Exception ex)
{
    Log.Error(ex, "Analyst fatal error");
}
finally
{
    Log.CloseAndFlush();
}
