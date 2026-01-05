using Microsoft.EntityFrameworkCore;

namespace TradeLogger.Data;

public class TradingDbContext : DbContext
{
    public TradingDbContext(DbContextOptions<TradingDbContext> options) : base(options) { }

    public DbSet<Entities.TradeEntity> Trades => Set<Entities.TradeEntity>();
    public DbSet<Entities.DailySummaryEntity> DailySummaries => Set<Entities.DailySummaryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Entities.TradeEntity>(e =>
        {
            e.ToTable("trades");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Timestamp).HasColumnType("timestamptz").IsRequired();
            e.Property(x => x.MarketId).HasColumnName("market_id").IsRequired();
            e.Property(x => x.SelectionId).HasColumnName("selection_id").IsRequired();
            e.Property(x => x.Stake).HasColumnType("numeric").IsRequired();
            e.Property(x => x.Odds).HasColumnType("numeric").IsRequired();
            e.Property(x => x.Type).HasColumnName("type").IsRequired();
            e.Property(x => x.Status).HasColumnName("status").IsRequired();
            e.Property(x => x.ProfitLoss).HasColumnName("profit_loss").HasColumnType("numeric");
            e.Property(x => x.Commission).HasColumnName("commission").HasColumnType("numeric");
            e.Property(x => x.NetProfit).HasColumnName("net_profit").HasColumnType("numeric");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

            e.HasIndex(x => x.Timestamp);
            e.HasIndex(x => x.MarketId);
            e.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<Entities.DailySummaryEntity>(e =>
        {
            e.ToTable("daily_summaries");
            e.HasKey(x => x.Id);
            e.Property(x => x.Date).HasColumnType("date").IsRequired();
        });
    }
}
