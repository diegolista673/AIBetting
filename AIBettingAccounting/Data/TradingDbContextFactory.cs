using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using System;

namespace AIBettingAccounting.Data;

// Design-time factory to enable EF Core migrations scaffolding
public class TradingDbContextFactory : IDesignTimeDbContextFactory<TradingDbContext>
{
    public TradingDbContext CreateDbContext(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables();

        var config = builder.Build();

        var connectionString = config.GetConnectionString("Accounting")
            ?? config["ACCOUNTING_CONNECTION"]
            ?? "Host=localhost;Port=5432;Database=aibetting;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<TradingDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new TradingDbContext(optionsBuilder.Options);
    }
}
