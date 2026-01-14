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
    
    // Strategy constants
    private const decimal ConfidenceScaleFactor = 10m;
    private const int DefaultPriority = 70;
    private const int DefaultValidityWindowSeconds = 60;
    
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
            var signal = DetectGreenUpOpportunity(runner, snapshot, context);
            if (signal != null)
                signals.Add(signal);
        }
        
        return await Task.FromResult<IEnumerable<StrategySignal>>(signals);
    }
    
    private StrategySignal? DetectGreenUpOpportunity(
        RunnerSnapshot runner,
        MarketSnapshot snapshot,
        AnalysisContext context)
    {
        if (context.HistoricalSnapshots.Count < 3)
        {
            _logger.Debug("{Strategy}: Insufficient history for {Runner}", 
                StrategyName, runner.RunnerName);
            return null;
        }
        
        // Calculate price improvement from entry point (simulated)
        var currentPrice = runner.LastPriceTraded ?? GetMidPrice(runner);
        if (currentPrice == 0m)
        {
            _logger.Debug("{Strategy}: Invalid current price for {Runner}", 
                StrategyName, runner.RunnerName);
            return null;
        }
        
        var entryPrice = GetHistoricalPrice(runner, context.HistoricalSnapshots, 5);
        
        if (entryPrice == 0m)
        {
            _logger.Debug("{Strategy}: Invalid entry price for {Runner}", 
                StrategyName, runner.RunnerName);
            return null;
        }
        
        var priceImprovement = Math.Abs((currentPrice - entryPrice) / entryPrice) * 100m;
        
        if (priceImprovement < _greenConfig.MinPriceImprovement)
        {
            _logger.Debug("{Strategy}: Price improvement {Improvement:F2}% below threshold {Threshold:F2}%",
                StrategyName, priceImprovement, _greenConfig.MinPriceImprovement);
            return null;
        }
        
        // Calculate potential profit if greened up now
        var hedgeOdds = currentPrice;
        var profitPotential = CalculateGreenUpProfit(entryPrice, hedgeOdds);
        
        if (profitPotential < _greenConfig.MinProfitThreshold)
        {
            _logger.Debug("{Strategy}: Profit potential {Profit:F2}% below threshold {Threshold:F2}%",
                StrategyName, profitPotential, _greenConfig.MinProfitThreshold);
            return null;
        }
        
        var confidence = CalculateConfidence(priceImprovement);
        
        _logger.Information(
            "{Strategy}: Green-up opportunity on {Runner} - Price improved {Improvement:F2}%, " +
            "Profit potential: {Profit:F2}%, Confidence: {Confidence:F2}",
            StrategyName, runner.RunnerName, priceImprovement, profitPotential, confidence);
        
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
            Priority = DefaultPriority,
            ValidityWindow = DefaultValidityWindowSeconds,
            PrimarySelection = new SelectionSignal
            {
                SelectionId = runner.SelectionId.Value,
                SelectionName = runner.RunnerName,
                RecommendedOdds = hedgeOdds,
                Stake = _greenConfig.DefaultHedgeStake,
                BetType = BetType.Lay
            },
            Metadata = new Dictionary<string, object>
            {
                ["priceImprovement"] = priceImprovement,
                ["entryPrice"] = entryPrice,
                ["hedgePrice"] = hedgeOdds,
                ["profitPotential"] = profitPotential,
                ["currentPrice"] = currentPrice
            }
        };
    }
    
    /// <summary>
    /// Calculate confidence based on price improvement.
    /// Higher improvement = higher confidence (capped at 1.0).
    /// </summary>
    private double CalculateConfidence(decimal priceImprovement)
    {
        return (double)Math.Min(priceImprovement / ConfidenceScaleFactor, 1m);
    }
    
    private decimal GetMidPrice(RunnerSnapshot runner)
    {
        var bestBack = runner.AvailableToBack?.FirstOrDefault()?.Price ?? 0m;
        var bestLay = runner.AvailableToLay?.FirstOrDefault()?.Price ?? 0m;
        
        if (bestBack > 0m && bestLay > 0m)
            return (bestBack + bestLay) / 2m;
        
        return bestBack > 0m ? bestBack : bestLay;
    }

    private decimal GetHistoricalPrice(RunnerSnapshot current, List<MarketSnapshot> history, int periodsBack)
    {
        if (periodsBack <= 0 || periodsBack > history.Count)
        {
            _logger.Debug("{Strategy}: Invalid periods back: {Periods}", StrategyName, periodsBack);
            return 0m;
        }
        
        var historical = history.Skip(periodsBack - 1).FirstOrDefault();
        if (historical == null)
        {
            _logger.Debug("{Strategy}: No historical snapshot found at period {Period}", 
                StrategyName, periodsBack);
            return 0m;
        }
        
        var runner = historical.Runners.FirstOrDefault(r => r.SelectionId.Value == current.SelectionId.Value);
        if (runner == null)
        {
            _logger.Debug("{Strategy}: Runner {Runner} not found in historical snapshot", 
                StrategyName, current.RunnerName);
            return 0m;
        }
        
        return runner.LastPriceTraded ?? GetMidPrice(runner);
    }

    private decimal CalculateGreenUpProfit(decimal entryOdds, decimal hedgeOdds)
    {
        if (entryOdds == 0m)
            return 0m;
        
        // Simplified profit calculation: percentage difference
        return Math.Abs((entryOdds - hedgeOdds) / entryOdds) * 100m;
    }
}

public class GreenUpConfiguration : StrategyConfiguration
{
    public decimal MinPriceImprovement { get; init; } = 3m; // 3% improvement
    public decimal MinProfitThreshold { get; init; } = 1m; // 1% profit minimum
    public decimal DefaultHedgeStake { get; init; } = 50m; // Default stake for hedge
}
