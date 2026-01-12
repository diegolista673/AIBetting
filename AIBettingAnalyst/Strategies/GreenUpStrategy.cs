using AIBettingCore.Models;
using AIBettingAnalyst.Models;
using Serilog;

namespace AIBettingAnalyst.Strategies;

/// <summary>
/// Green-Up Strategy: identifies opportunities to lock in guaranteed profit
/// by hedging existing positions or capitalizing on price movements.
/// </summary>
public class GreenUpStrategy : AnalyzerBase
{
    private readonly GreenUpConfiguration _greenConfig;
    
    public override string StrategyName => "GREEN_UP";
    public override string Description => "Lock-in profit by hedging favorable price movements";
    
    public GreenUpStrategy(GreenUpConfiguration config, ILogger? logger = null)
        : base(config, logger)
    {
        _greenConfig = config;
    }
    
    public override async Task<IEnumerable<StrategySignal>> AnalyzeAsync(
        MarketSnapshot snapshot,
        AnalysisContext context)
    {
        if (!CanAnalyze(snapshot))
            return Enumerable.Empty<StrategySignal>();
        
        // Green-up requires position tracking - simplified version detects favorable movements
        var signals = new List<StrategySignal>();
        
        foreach (var runner in snapshot.Runners)
        {
            // Check if price has moved favorably for potential green-up
            var signal = await DetectGreenUpOpportunityAsync(runner, snapshot, context);
            if (signal != null)
                signals.Add(signal);
        }
        
        return signals;
    }
    
    private async Task<StrategySignal?> DetectGreenUpOpportunityAsync(
        RunnerSnapshot runner,
        MarketSnapshot snapshot,
        AnalysisContext context)
    {
        if (context.HistoricalSnapshots.Count < 3)
            return null;
        
        // Calculate price improvement from entry point (simulated)
        var currentPrice = runner.LastPriceTraded ?? GetMidPrice(runner);
        var entryPrice = GetHistoricalPrice(runner, context.HistoricalSnapshots, 5);
        
        if (entryPrice == 0m) return null;
        
        var priceImprovement = Math.Abs((currentPrice - entryPrice) / entryPrice) * 100m;
        
        if (priceImprovement < _greenConfig.MinPriceImprovement)
            return null;
        
        // Calculate potential profit if greened up now
        var hedgeOdds = currentPrice;
        var profitPotential = CalculateGreenUpProfit(entryPrice, hedgeOdds);
        
        if (profitPotential < _greenConfig.MinProfitThreshold)
            return null;
        
        var confidence = (double)Math.Min(priceImprovement / 10m, 1m);
        
        var signal = CreateSignal(
            snapshot,
            "GREEN_UP_OPPORTUNITY",
            confidence,
            (double)profitPotential,
            RiskLevel.Low,
            $"Green-up available: {priceImprovement:F2}% price improvement, {profitPotential:F2}% profit");
        
        return signal with
        {
            Action = TradeAction.Hedge,
            Priority = 70,
            ValidityWindow = 60,
            PrimarySelection = new SelectionSignal
            {
                SelectionId = runner.SelectionId.Value,
                SelectionName = runner.RunnerName,
                RecommendedOdds = hedgeOdds,
                Stake = 50m,
                BetType = BetType.Lay
            },
            Metadata = new Dictionary<string, object>
            {
                ["priceImprovement"] = priceImprovement,
                ["entryPrice"] = entryPrice,
                ["hedgePrice"] = hedgeOdds
            }
        };
    }
    
    private decimal GetMidPrice(RunnerSnapshot runner)
    {
        var bestBack = runner.AvailableToBack?.FirstOrDefault()?.Price ?? 0m;
        var bestLay = runner.AvailableToLay?.FirstOrDefault()?.Price ?? 0m;
        return bestBack > 0m && bestLay > 0m ? (bestBack + bestLay) / 2m : bestBack > 0m ? bestBack : bestLay;
    }
    
    private decimal GetHistoricalPrice(RunnerSnapshot current, List<MarketSnapshot> history, int periodsBack)
    {
        var historical = history.Skip(periodsBack - 1).FirstOrDefault();
        if (historical == null) return 0m;
        
        var runner = historical.Runners.FirstOrDefault(r => r.SelectionId.Value == current.SelectionId.Value);
        return runner?.LastPriceTraded ?? GetMidPrice(runner!);
    }
    
    private decimal CalculateGreenUpProfit(decimal entryOdds, decimal hedgeOdds)
    {
        // Simplified profit calculation
        return Math.Abs((entryOdds - hedgeOdds) / entryOdds) * 100m;
    }
}

public class GreenUpConfiguration : StrategyConfiguration
{
    public decimal MinPriceImprovement { get; init; } = 3m; // 3% improvement
    public decimal MinProfitThreshold { get; init; } = 1m; // 1% profit minimum
}
