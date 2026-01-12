using AIBettingCore.Models;
using AIBettingAnalyst.Models;

namespace AIBettingAnalyst.Strategies;

/// <summary>
/// Interface for market analysis strategies that detect trading opportunities.
/// </summary>
public interface IAnalysisStrategy
{
    /// <summary>
    /// Strategy name for identification and logging.
    /// </summary>
    string StrategyName { get; }
    
    /// <summary>
    /// Strategy description.
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Whether this strategy is enabled in configuration.
    /// </summary>
    bool IsEnabled { get; }
    
    /// <summary>
    /// Analyze a market snapshot and detect trading opportunities.
    /// </summary>
    /// <param name="snapshot">Current market snapshot</param>
    /// <param name="context">Additional context like historical data</param>
    /// <returns>List of detected signals, empty if no opportunities found</returns>
    Task<IEnumerable<StrategySignal>> AnalyzeAsync(MarketSnapshot snapshot, AnalysisContext context);
    
    /// <summary>
    /// Validate if this strategy can be applied to the given market.
    /// </summary>
    /// <param name="snapshot">Market snapshot to validate</param>
    /// <returns>True if strategy is applicable</returns>
    bool CanAnalyze(MarketSnapshot snapshot);
}

/// <summary>
/// Additional context for strategy analysis.
/// </summary>
public class AnalysisContext
{
    /// <summary>
    /// Historical snapshots for trend analysis (most recent first).
    /// </summary>
    public List<MarketSnapshot> HistoricalSnapshots { get; init; } = new();
    
    /// <summary>
    /// Time elapsed since market started (seconds).
    /// </summary>
    public int MarketAge { get; init; }
    
    /// <summary>
    /// Current timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// External market conditions or indicators.
    /// </summary>
    public Dictionary<string, object> ExternalData { get; init; } = new();
}
