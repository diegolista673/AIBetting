using AIBettingCore.Models;
using AIBettingAnalyst.Models;
using Serilog;

namespace AIBettingAnalyst.Strategies;

/// <summary>
/// Base class for analysis strategies with common functionality.
/// </summary>
public abstract class AnalyzerBase : IAnalysisStrategy
{
    protected readonly ILogger _logger;
    protected readonly StrategyConfiguration _config;
    
    public abstract string StrategyName { get; }
    public abstract string Description { get; }
    public bool IsEnabled => _config.Enabled;
    
    protected AnalyzerBase(StrategyConfiguration config, ILogger? logger = null)
    {
        _config = config;
        _logger = logger ?? Log.ForContext(GetType());
    }
    
    public abstract Task<IEnumerable<StrategySignal>> AnalyzeAsync(
        MarketSnapshot snapshot, 
        AnalysisContext context);
    
    public virtual bool CanAnalyze(MarketSnapshot snapshot)
    {
        if (!IsEnabled) return false;
        
        // Basic validation
        if (snapshot.Runners == null || !snapshot.Runners.Any())
        {
            _logger.Debug("{Strategy}: No runners in market", StrategyName);
            return false;
        }
        
        if (!snapshot.TotalMatched.HasValue || snapshot.TotalMatched.Value < _config.MinLiquidity)
        {
            _logger.Debug("{Strategy}: Insufficient liquidity. Matched: {Matched}, Required: {Required}",
                StrategyName, snapshot.TotalMatched ?? 0, _config.MinLiquidity);
            return false;
        }
        
        if (snapshot.SecondsToStart.HasValue && snapshot.SecondsToStart.Value < _config.MinTimeToStart)
        {
            _logger.Debug("{Strategy}: Too close to start. Seconds: {Seconds}, Required: {Required}",
                StrategyName, snapshot.SecondsToStart.Value, _config.MinTimeToStart);
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Calculate market volatility based on price movements.
    /// </summary>
    protected decimal CalculateVolatility(List<MarketSnapshot> history)
    {
        if (history.Count < 2) return 0m;
        
        var priceChanges = new List<decimal>();
        for (int i = 0; i < history.Count - 1; i++)
        {
            var current = history[i].Runners.FirstOrDefault()?.LastPriceTraded ?? 0m;
            var previous = history[i + 1].Runners.FirstOrDefault()?.LastPriceTraded ?? 0m;
            
            if (previous > 0m)
            {
                var change = Math.Abs((current - previous) / previous);
                priceChanges.Add(change);
            }
        }
        
        if (!priceChanges.Any()) return 0m;
        
        // Return average absolute price change as volatility metric
        return priceChanges.Average();
    }
    
    /// <summary>
    /// Calculate liquidity score (0-1) based on available volume.
    /// </summary>
    protected decimal CalculateLiquidityScore(RunnerSnapshot runner)
    {
        var backVolume = runner.AvailableToBack?.Sum(p => p.Size) ?? 0m;
        var layVolume = runner.AvailableToLay?.Sum(p => p.Size) ?? 0m;
        var totalVolume = backVolume + layVolume;
        
        // Score based on £10k available volume
        var targetVolume = 10000m;
        return Math.Min(totalVolume / targetVolume, 1m);
    }
    
    /// <summary>
    /// Check if odds are within acceptable range for execution.
    /// </summary>
    protected bool IsOddsValid(decimal odds)
    {
        return odds >= _config.MinOdds && odds <= _config.MaxOdds;
    }
    
    /// <summary>
    /// Create signal with common fields populated.
    /// </summary>
    protected StrategySignal CreateSignal(
        MarketSnapshot snapshot,
        string signalType,
        double confidence,
        double expectedROI,
        RiskLevel risk,
        string reason)
    {
        return new StrategySignal
        {
            MarketId = snapshot.MarketId.Value,
            Strategy = StrategyName,
            SignalType = signalType,
            Confidence = confidence,
            ExpectedROI = expectedROI,
            Risk = risk,
            Reason = reason,
            MarketContext = new MarketContext
            {
                TotalMatched = snapshot.TotalMatched ?? 0m,
                SecondsToStart = snapshot.SecondsToStart ?? 0,
                EventName = snapshot.EventName,
                MarketVolatility = 0m, // Will be calculated by caller
                LiquidityScore = 0m    // Will be calculated by caller
            }
        };
    }
}

/// <summary>
/// Configuration for a strategy.
/// </summary>
public class StrategyConfiguration
{
    public bool Enabled { get; init; } = true;
    public decimal MinLiquidity { get; init; } = 1000m;
    public int MinTimeToStart { get; init; } = 300; // 5 minutes
    public decimal MinOdds { get; init; } = 1.1m;
    public decimal MaxOdds { get; init; } = 100m;
    public double MinConfidence { get; init; } = 0.6;
    public Dictionary<string, object> CustomSettings { get; init; } = new();
}
