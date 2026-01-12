using AIBettingCore.Models;
using AIBettingAnalyst.Models;
using Serilog;

namespace AIBettingAnalyst.Strategies;

/// <summary>
/// Value Bet Strategy: identifies selections trading at odds higher than their true probability,
/// representing positive expected value (EV+) opportunities.
/// </summary>
public class ValueBetStrategy : AnalyzerBase
{
    private readonly ValueBetConfiguration _valueConfig;
    
    public override string StrategyName => "VALUE_BET";
    public override string Description => "Detects selections with positive expected value vs true odds";
    
    public ValueBetStrategy(ValueBetConfiguration config, ILogger? logger = null)
        : base(config, logger)
    {
        _valueConfig = config;
    }
    
    public override async Task<IEnumerable<StrategySignal>> AnalyzeAsync(
        MarketSnapshot snapshot,
        AnalysisContext context)
    {
        if (!CanAnalyze(snapshot))
            return Enumerable.Empty<StrategySignal>();
        
        var signals = new List<StrategySignal>();
        
        foreach (var runner in snapshot.Runners)
        {
            var signal = await DetectValueBetAsync(runner, snapshot, context);
            if (signal != null)
                signals.Add(signal);
        }
        
        return signals;
    }
    
    private async Task<StrategySignal?> DetectValueBetAsync(
        RunnerSnapshot runner,
        MarketSnapshot snapshot,
        AnalysisContext context)
    {
        var marketOdds = runner.LastPriceTraded ?? GetMidPrice(runner);
        if (marketOdds == 0m || !IsOddsValid(marketOdds))
            return null;
        
        // Estimate "true" odds using multiple factors
        var trueOdds = EstimateTrueOdds(runner, snapshot, context);
        if (trueOdds == 0m) return null;
        
        // Calculate value: market odds should be higher than true odds for value
        var valuePercentage = ((marketOdds - trueOdds) / trueOdds) * 100m;
        
        if (valuePercentage < _valueConfig.MinValuePercentage)
            return null;
        
        // Calculate expected value
        var trueProbability = 1m / trueOdds;
        var marketProbability = 1m / marketOdds;
        var ev = (trueProbability * (marketOdds - 1m)) - (1m - trueProbability);
        
        if (ev < _valueConfig.MinExpectedValue)
            return null;
        
        var confidence = CalculateConfidence(valuePercentage, ev, runner);
        if (confidence < _config.MinConfidence)
            return null;
        
        var signal = CreateSignal(
            snapshot,
            "VALUE_BET",
            confidence,
            (double)(ev * 100m),
            RiskLevel.Medium,
            $"Value bet: Market {marketOdds:F2} vs True {trueOdds:F2}, EV: {ev:F2}");
        
        return signal with
        {
            Action = TradeAction.Back,
            Priority = 60,
            ValidityWindow = 120,
            PrimarySelection = new SelectionSignal
            {
                SelectionId = runner.SelectionId.Value,
                SelectionName = runner.RunnerName,
                RecommendedOdds = marketOdds,
                Stake = CalculateKellyStake(marketOdds, trueProbability),
                BetType = BetType.Back
            },
            Metadata = new Dictionary<string, object>
            {
                ["marketOdds"] = marketOdds,
                ["trueOdds"] = trueOdds,
                ["valuePercentage"] = valuePercentage,
                ["expectedValue"] = ev,
                ["trueProbability"] = trueProbability
            }
        };
    }
    
    private decimal EstimateTrueOdds(RunnerSnapshot runner, MarketSnapshot snapshot, AnalysisContext context)
    {
        // Simplified true odds estimation using multiple signals
        var factors = new List<decimal>();
        
        // 1. Historical performance (if available in context)
        // 2. Weight of money distribution
        var womOdds = EstimateFromWeightOfMoney(runner, snapshot);
        if (womOdds > 0m) factors.Add(womOdds);
        
        // 3. Market consensus (average of available odds)
        var consensusOdds = GetConsensusOdds(runner);
        if (consensusOdds > 0m) factors.Add(consensusOdds);
        
        // 4. Volume-weighted price
        var vwap = CalculateVWAP(runner);
        if (vwap > 0m) factors.Add(vwap);
        
        return factors.Any() ? factors.Average() : 0m;
    }
    
    private decimal EstimateFromWeightOfMoney(RunnerSnapshot runner, MarketSnapshot snapshot)
    {
        var totalMarketVolume = snapshot.Runners.Sum(r => r.TotalMatched);
        if (totalMarketVolume == 0m) return 0m;
        
        var runnerProportion = runner.TotalMatched / totalMarketVolume;
        return runnerProportion > 0m ? 1m / runnerProportion : 0m;
    }
    
    private decimal GetConsensusOdds(RunnerSnapshot runner)
    {
        var availableOdds = new List<decimal>();
        
        if (runner.AvailableToBack != null)
            availableOdds.AddRange(runner.AvailableToBack.Take(3).Select(p => p.Price));
        
        if (runner.AvailableToLay != null)
            availableOdds.AddRange(runner.AvailableToLay.Take(3).Select(p => p.Price));
        
        return availableOdds.Any() ? availableOdds.Average() : 0m;
    }
    
    private decimal CalculateVWAP(RunnerSnapshot runner)
    {
        decimal totalValue = 0m;
        decimal totalVolume = 0m;
        
        if (runner.AvailableToBack != null)
        {
            foreach (var level in runner.AvailableToBack.Take(5))
            {
                totalValue += level.Price * level.Size;
                totalVolume += level.Size;
            }
        }
        
        return totalVolume > 0m ? totalValue / totalVolume : 0m;
    }
    
    private decimal GetMidPrice(RunnerSnapshot runner)
    {
        var bestBack = runner.AvailableToBack?.FirstOrDefault()?.Price ?? 0m;
        var bestLay = runner.AvailableToLay?.FirstOrDefault()?.Price ?? 0m;
        return bestBack > 0m && bestLay > 0m ? (bestBack + bestLay) / 2m : bestBack > 0m ? bestBack : bestLay;
    }
    
    private double CalculateConfidence(decimal valuePercentage, decimal ev, RunnerSnapshot runner)
    {
        var valueScore = Math.Min((double)valuePercentage / 20.0, 1.0);
        var evScore = Math.Min((double)(ev * 10m), 1.0);
        var liquidityScore = (double)CalculateLiquidityScore(runner);
        
        return (valueScore * 0.5 + evScore * 0.3 + liquidityScore * 0.2);
    }
    
    private decimal CalculateKellyStake(decimal marketOdds, decimal trueProbability)
    {
        // Kelly Criterion: f = (bp - q) / b
        // where b = odds-1, p = true prob, q = 1-p
        var b = marketOdds - 1m;
        var q = 1m - trueProbability;
        var f = (b * trueProbability - q) / b;
        
        // Use fractional Kelly for safety
        var fractionalKelly = f * _valueConfig.KellyFraction;
        
        return Math.Max(10m, Math.Min(fractionalKelly * _valueConfig.MaxStake, _valueConfig.MaxStake));
    }
}

public class ValueBetConfiguration : StrategyConfiguration
{
    public decimal MinValuePercentage { get; init; } = 5m; // 5% value
    public decimal MinExpectedValue { get; init; } = 0.05m; // 5% EV
    public decimal KellyFraction { get; init; } = 0.25m; // Quarter Kelly
    public decimal MaxStake { get; init; } = 100m;
}
