using AIBettingCore.Models;
using AIBettingAnalyst.Models;
using Serilog;

namespace AIBettingAnalyst.Strategies;

/// <summary>
/// Orchestrates multiple analysis strategies, manages execution priority,
/// and resolves conflicts between signals.
/// </summary>
public class StrategyOrchestrator
{
    private readonly List<IAnalysisStrategy> _strategies;
    private readonly ILogger _logger;
    private readonly OrchestratorConfiguration _config;
    
    public StrategyOrchestrator(
        IEnumerable<IAnalysisStrategy> strategies,
        OrchestratorConfiguration config,
        ILogger? logger = null)
    {
        _strategies = strategies.Where(s => s.IsEnabled).ToList();
        _config = config;
        _logger = logger ?? Log.ForContext<StrategyOrchestrator>();
        
        _logger.Information("Strategy Orchestrator initialized with {Count} strategies", _strategies.Count);
        foreach (var strategy in _strategies)
        {
            _logger.Information("  - {Strategy}: {Description}", strategy.StrategyName, strategy.Description);
        }
    }
    
    /// <summary>
    /// Analyze market using all enabled strategies and return prioritized signals.
    /// </summary>
    public async Task<IEnumerable<StrategySignal>> AnalyzeMarketAsync(
        MarketSnapshot snapshot,
        AnalysisContext context)
    {
        var allSignals = new List<StrategySignal>();
        
        // Run all strategies in parallel
        var tasks = _strategies.Select(strategy => 
            ExecuteStrategyAsync(strategy, snapshot, context));
        
        var results = await Task.WhenAll(tasks);
        
        foreach (var signals in results)
        {
            allSignals.AddRange(signals);
        }
        
        if (!allSignals.Any())
            return Enumerable.Empty<StrategySignal>();
        
        _logger.Debug("Total signals generated: {Count}", allSignals.Count);
        
        // Filter, prioritize, and resolve conflicts
        var filteredSignals = FilterSignals(allSignals);
        var resolvedSignals = ResolveConflicts(filteredSignals);
        var prioritizedSignals = PrioritizeSignals(resolvedSignals);
        
        return prioritizedSignals;
    }
    
    private async Task<IEnumerable<StrategySignal>> ExecuteStrategyAsync(
        IAnalysisStrategy strategy,
        MarketSnapshot snapshot,
        AnalysisContext context)
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var signals = await strategy.AnalyzeAsync(snapshot, context);
            sw.Stop();
            
            if (signals.Any())
            {
                _logger.Information(
                    "{Strategy} generated {Count} signal(s) in {Ms}ms",
                    strategy.StrategyName, signals.Count(), sw.ElapsedMilliseconds);
            }
            
            return signals;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Strategy {Strategy} failed", strategy.StrategyName);
            return Enumerable.Empty<StrategySignal>();
        }
    }
    
    /// <summary>
    /// Filter signals based on quality thresholds.
    /// </summary>
    private List<StrategySignal> FilterSignals(List<StrategySignal> signals)
    {
        var filtered = signals.Where(s =>
            s.Confidence >= _config.MinConfidence &&
            s.ExpectedROI >= _config.MinExpectedROI &&
            s.Risk <= _config.MaxRisk
        ).ToList();
        
        if (filtered.Count < signals.Count)
        {
            _logger.Debug(
                "Filtered signals: {Original} → {Filtered} (removed {Removed})",
                signals.Count, filtered.Count, signals.Count - filtered.Count);
        }
        
        return filtered;
    }
    
    /// <summary>
    /// Resolve conflicts when multiple strategies signal different actions on same selection.
    /// </summary>
    private List<StrategySignal> ResolveConflicts(List<StrategySignal> signals)
    {
        var resolved = new List<StrategySignal>();
        
        // Group by selection
        var groupedBySelection = signals
            .GroupBy(s => s.PrimarySelection?.SelectionId ?? "unknown")
            .ToList();
        
        foreach (var group in groupedBySelection)
        {
            if (group.Count() == 1)
            {
                resolved.Add(group.First());
                continue;
            }
            
            // Multiple signals for same selection - resolve conflict
            _logger.Debug(
                "Conflict detected for selection {Selection}: {Count} signals",
                group.Key, group.Count());
            
            var conflictResolved = ResolveConflict(group.ToList());
            if (conflictResolved != null)
                resolved.Add(conflictResolved);
        }
        
        return resolved;
    }
    
    /// <summary>
    /// Resolve conflict between multiple signals for same selection.
    /// </summary>
    private StrategySignal? ResolveConflict(List<StrategySignal> conflictingSignals)
    {
        // Strategy 1: If actions are same, combine confidence
        var actions = conflictingSignals.Select(s => s.Action).Distinct().ToList();
        
        if (actions.Count == 1)
        {
            // Same action - take highest confidence
            var best = conflictingSignals.OrderByDescending(s => s.Confidence).First();
            _logger.Debug(
                "Conflict resolved: Same action, selecting highest confidence ({Strategy})",
                best.Strategy);
            return best;
        }
        
        // Strategy 2: Opposite actions - use weighted confidence
        var backSignals = conflictingSignals.Where(s => s.Action == TradeAction.Back).ToList();
        var laySignals = conflictingSignals.Where(s => s.Action == TradeAction.Lay).ToList();
        
        var backWeight = backSignals.Sum(s => s.Confidence * s.Priority);
        var layWeight = laySignals.Sum(s => s.Confidence * s.Priority);
        
        if (Math.Abs(backWeight - layWeight) < _config.ConflictThreshold)
        {
            // Too close - no trade
            _logger.Debug(
                "Conflict unresolved: Weights too close (Back: {Back:F2}, Lay: {Lay:F2})",
                backWeight, layWeight);
            return null;
        }
        
        // Take the stronger signal
        var winner = backWeight > layWeight ? backSignals.First() : laySignals.First();
        _logger.Information(
            "Conflict resolved: {Strategy} wins (weight: {Weight:F2})",
            winner.Strategy, backWeight > layWeight ? backWeight : layWeight);
        
        return winner;
    }
    
    /// <summary>
    /// Prioritize signals for execution order.
    /// </summary>
    private List<StrategySignal> PrioritizeSignals(List<StrategySignal> signals)
    {
        // Sort by:
        // 1. Priority (high to low)
        // 2. Confidence (high to low)
        // 3. Expected ROI (high to low)
        var prioritized = signals
            .OrderByDescending(s => s.Priority)
            .ThenByDescending(s => s.Confidence)
            .ThenByDescending(s => s.ExpectedROI)
            .Take(_config.MaxSignalsPerAnalysis)
            .ToList();
        
        if (prioritized.Any())
        {
            _logger.Information(
                "Prioritized {Count} signal(s) for execution:",
                prioritized.Count);
            
            foreach (var signal in prioritized)
            {
                _logger.Information(
                    "  {Priority}. {Strategy} - {Type}: {Selection} @ {Odds:F2}, " +
                    "Confidence: {Conf:F2}, ROI: {ROI:F2}%, Risk: {Risk}",
                    signal.Priority, signal.Strategy, signal.SignalType,
                    signal.PrimarySelection?.SelectionName ?? "N/A",
                    signal.PrimarySelection?.RecommendedOdds ?? 0,
                    signal.Confidence, signal.ExpectedROI, signal.Risk);
            }
        }
        
        return prioritized;
    }
}

/// <summary>
/// Configuration for strategy orchestration.
/// </summary>
public class OrchestratorConfiguration
{
    public double MinConfidence { get; init; } = 0.6;
    public double MinExpectedROI { get; init; } = 0.3; // 0.3%
    public RiskLevel MaxRisk { get; init; } = RiskLevel.High;
    public int MaxSignalsPerAnalysis { get; init; } = 5; // Top 5 signals
    public double ConflictThreshold { get; init; } = 10.0; // Min difference to resolve conflict
}
