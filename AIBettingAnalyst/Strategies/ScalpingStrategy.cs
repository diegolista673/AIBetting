using AIBettingCore.Models;
using AIBettingAnalyst.Models;
using Serilog;

namespace AIBettingAnalyst.Strategies;

/// <summary>
/// Scalping strategy: detects short-term momentum opportunities for quick profits.
/// Looks for price movements with strong volume and favorable liquidity.
/// </summary>
public class ScalpingStrategy : AnalyzerBase
{
    private readonly ScalpingConfiguration _scalpConfig;
    
    public override string StrategyName => "SCALPING";
    public override string Description => "Short-term momentum trading with quick entry/exit";
    
    public ScalpingStrategy(ScalpingConfiguration config, ILogger? logger = null)
        : base(config, logger)
    {
        _scalpConfig = config;
    }
    
    public override async Task<IEnumerable<StrategySignal>> AnalyzeAsync(
        MarketSnapshot snapshot,
        AnalysisContext context)
    {
        if (!CanAnalyze(snapshot))
            return Enumerable.Empty<StrategySignal>();
        
        var signals = new List<StrategySignal>();
        
        // Need historical data for momentum calculation
        if (context.HistoricalSnapshots.Count < _scalpConfig.MinHistoryDepth)
        {
            _logger.Debug("{Strategy}: Insufficient history. Have: {Count}, Need: {Required}",
                StrategyName, context.HistoricalSnapshots.Count, _scalpConfig.MinHistoryDepth);
            return signals;
        }
        
        foreach (var runner in snapshot.Runners)
        {
            var signal = await AnalyzeRunnerAsync(runner, snapshot, context);
            if (signal != null)
                signals.Add(signal);
        }
        
        return signals;
    }
    
    private async Task<StrategySignal?> AnalyzeRunnerAsync(
        RunnerSnapshot runner,
        MarketSnapshot snapshot,
        AnalysisContext context)
    {
        // 1. Calculate momentum
        var momentum = CalculateMomentum(runner, context.HistoricalSnapshots);
        if (Math.Abs(momentum) < _scalpConfig.MinMomentumThreshold)
            return null;
        
        // 2. Check velocity (rate of price change)
        var velocity = CalculateVelocity(runner, context.HistoricalSnapshots);
        if (Math.Abs(velocity) < _scalpConfig.MinVelocityThreshold)
            return null;
        
        // 3. Verify liquidity for scalping
        var liquidityScore = CalculateLiquidityScore(runner);
        if (liquidityScore < _scalpConfig.MinLiquidityScore)
            return null;
        
        // 4. Check spread is acceptable for quick execution
        var spread = CalculateSpread(runner);
        if (spread > _scalpConfig.MaxSpread)
            return null;
        
        // 5. Detect direction (LONG = back, SHORT = lay)
        var direction = momentum > 0 ? TradeAction.Back : TradeAction.Lay;
        var currentPrice = runner.LastPriceTraded ?? GetMidPrice(runner);
        
        if (!IsOddsValid(currentPrice))
            return null;
        
        // 6. Calculate entry/exit levels
        var (entryPrice, stopLoss, takeProfit) = CalculateLevels(
            currentPrice, momentum, direction);
        
        // 7. Estimate confidence and ROI
        var confidence = CalculateConfidence(momentum, velocity, liquidityScore, spread);
        if (confidence < _config.MinConfidence)
            return null;
        
        var expectedROI = CalculateExpectedROI(currentPrice, takeProfit, direction);
        if (expectedROI < _scalpConfig.MinExpectedROI)
            return null;
        
        // 8. Determine risk level
        var risk = DetermineRisk(momentum, velocity, snapshot.SecondsToStart ?? 0);
        
        _logger.Information(
            "{Strategy}: Scalp opportunity detected on {Selection}. " +
            "Direction: {Direction}, Momentum: {Momentum:F4}, Velocity: {Velocity:F4}, " +
            "Confidence: {Confidence:F2}, ROI: {ROI:F2}%",
            StrategyName, runner.RunnerName, direction, momentum, velocity, confidence, expectedROI);
        
        var signal = CreateSignal(
            snapshot,
            $"SCALP_{(direction == TradeAction.Back ? "LONG" : "SHORT")}",
            confidence,
            expectedROI,
            risk,
            $"Momentum scalp: {momentum:F2}% price movement, velocity {velocity:F2}%/min");
        
        return signal with
        {
            Action = direction,
            Priority = 80, // High priority for scalping (time-sensitive)
            ValidityWindow = _scalpConfig.SignalValiditySeconds,
            PrimarySelection = new SelectionSignal
            {
                SelectionId = runner.SelectionId.Value,
                SelectionName = runner.RunnerName,
                RecommendedOdds = entryPrice,
                Stake = CalculateStake(expectedROI, risk),
                BetType = direction == TradeAction.Back ? BetType.Back : BetType.Lay,
                StopLoss = stopLoss,
                TakeProfit = takeProfit
            },
            Metadata = new Dictionary<string, object>
            {
                ["momentum"] = momentum,
                ["velocity"] = velocity,
                ["liquidityScore"] = liquidityScore,
                ["spread"] = spread,
                ["currentPrice"] = currentPrice,
                ["timeToStart"] = snapshot.SecondsToStart ?? 0
            }
        };
    }
    
    /// <summary>
    /// Calculate price momentum (percentage change over period).
    /// </summary>
    private decimal CalculateMomentum(RunnerSnapshot current, List<MarketSnapshot> history)
    {
        var currentPrice = current.LastPriceTraded ?? GetMidPrice(current);
        
        // Find same runner in history
        var historicalPrices = new List<decimal>();
        foreach (var snapshot in history.Take(_scalpConfig.MomentumPeriod))
        {
            var historicalRunner = snapshot.Runners.FirstOrDefault(
                r => r.SelectionId.Value == current.SelectionId.Value);
            
            if (historicalRunner != null)
            {
                var price = historicalRunner.LastPriceTraded ?? GetMidPrice(historicalRunner);
                if (price > 0m)
                    historicalPrices.Add(price);
            }
        }
        
        if (!historicalPrices.Any()) return 0m;
        
        var oldestPrice = historicalPrices.Last();
        if (oldestPrice == 0m) return 0m;
        
        // Momentum = (Current - Oldest) / Oldest * 100
        return ((currentPrice - oldestPrice) / oldestPrice) * 100m;
    }
    
    /// <summary>
    /// Calculate velocity (momentum per unit time).
    /// </summary>
    private decimal CalculateVelocity(RunnerSnapshot current, List<MarketSnapshot> history)
    {
        var momentum = CalculateMomentum(current, history);
        var timeWindowMinutes = _scalpConfig.MomentumPeriod; // Assuming 1 snapshot per minute
        
        if (timeWindowMinutes == 0) return 0m;
        
        return momentum / timeWindowMinutes; // % per minute
    }
    
    /// <summary>
    /// Calculate bid-ask spread.
    /// </summary>
    private decimal CalculateSpread(RunnerSnapshot runner)
    {
        var bestBack = runner.AvailableToBack?.FirstOrDefault()?.Price ?? 0m;
        var bestLay = runner.AvailableToLay?.FirstOrDefault()?.Price ?? 0m;
        
        if (bestBack == 0m || bestLay == 0m) return decimal.MaxValue;
        
        return bestLay - bestBack;
    }
    
    /// <summary>
    /// Get mid price between best back and lay.
    /// </summary>
    private decimal GetMidPrice(RunnerSnapshot runner)
    {
        var bestBack = runner.AvailableToBack?.FirstOrDefault()?.Price ?? 0m;
        var bestLay = runner.AvailableToLay?.FirstOrDefault()?.Price ?? 0m;
        
        if (bestBack == 0m && bestLay == 0m) return 0m;
        if (bestBack == 0m) return bestLay;
        if (bestLay == 0m) return bestBack;
        
        return (bestBack + bestLay) / 2m;
    }
    
    /// <summary>
    /// Calculate entry, stop loss, and take profit levels.
    /// </summary>
    private (decimal entry, decimal stopLoss, decimal takeProfit) CalculateLevels(
        decimal currentPrice,
        decimal momentum,
        TradeAction direction)
    {
        var entry = currentPrice;
        
        // Stop loss: 1-2 ticks away (depends on odds range)
        var tickSize = GetTickSize(currentPrice);
        var stopLossTicks = _scalpConfig.StopLossTicks;
        var takeProfitTicks = _scalpConfig.TakeProfitTicks;
        
        decimal stopLoss, takeProfit;
        
        if (direction == TradeAction.Back)
        {
            // For back bet, stop loss is higher odds (worse), take profit is lower odds (better)
            stopLoss = currentPrice + (tickSize * stopLossTicks);
            takeProfit = currentPrice - (tickSize * takeProfitTicks);
        }
        else
        {
            // For lay bet, opposite
            stopLoss = currentPrice - (tickSize * stopLossTicks);
            takeProfit = currentPrice + (tickSize * takeProfitTicks);
        }
        
        return (entry, Math.Max(1.01m, stopLoss), Math.Max(1.01m, takeProfit));
    }
    
    /// <summary>
    /// Get Betfair tick size for given odds.
    /// </summary>
    private decimal GetTickSize(decimal odds)
    {
        if (odds < 2m) return 0.01m;
        if (odds < 3m) return 0.02m;
        if (odds < 4m) return 0.05m;
        if (odds < 6m) return 0.1m;
        if (odds < 10m) return 0.2m;
        if (odds < 20m) return 0.5m;
        if (odds < 30m) return 1m;
        if (odds < 50m) return 2m;
        return 5m;
    }
    
    /// <summary>
    /// Calculate signal confidence.
    /// </summary>
    private double CalculateConfidence(
        decimal momentum,
        decimal velocity,
        decimal liquidityScore,
        decimal spread)
    {
        // Strong momentum + velocity = higher confidence
        var momentumScore = Math.Min(Math.Abs(momentum) / 5m, 1m); // 5% momentum = max score
        var velocityScore = Math.Min(Math.Abs(velocity) / 1m, 1m); // 1% per min = max score
        var spreadScore = spread < 0.02m ? 1m : spread < 0.05m ? 0.7m : 0.4m;
        
        var confidence = (
            (double)momentumScore * 0.4 +
            (double)velocityScore * 0.3 +
            (double)liquidityScore * 0.2 +
            (double)spreadScore * 0.1
        );
        
        return Math.Clamp(confidence, 0, 1);
    }
    
    /// <summary>
    /// Calculate expected ROI.
    /// </summary>
    private double CalculateExpectedROI(decimal entryPrice, decimal targetPrice, TradeAction direction)
    {
        if (entryPrice == 0m) return 0;
        
        var priceDiff = direction == TradeAction.Back
            ? entryPrice - targetPrice
            : targetPrice - entryPrice;
        
        return (double)((priceDiff / entryPrice) * 100m);
    }
    
    /// <summary>
    /// Determine risk level.
    /// </summary>
    private RiskLevel DetermineRisk(decimal momentum, decimal velocity, int secondsToStart)
    {
        // High momentum/velocity = higher risk
        // Close to start = higher risk
        var momentumRisk = Math.Abs(momentum) > 3m ? 1 : 0;
        var velocityRisk = Math.Abs(velocity) > 0.5m ? 1 : 0;
        var timeRisk = secondsToStart < 600 ? 1 : 0; // < 10 minutes
        
        var totalRisk = momentumRisk + velocityRisk + timeRisk;
        
        return totalRisk switch
        {
            0 => RiskLevel.Low,
            1 => RiskLevel.Medium,
            2 => RiskLevel.High,
            _ => RiskLevel.VeryHigh
        };
    }
    
    /// <summary>
    /// Calculate stake based on ROI and risk.
    /// </summary>
    private decimal CalculateStake(double expectedROI, RiskLevel risk)
    {
        var baseStake = _scalpConfig.BaseStake;
        
        // Adjust by risk
        var riskMultiplier = risk switch
        {
            RiskLevel.Low => 1.5m,
            RiskLevel.Medium => 1.0m,
            RiskLevel.High => 0.5m,
            RiskLevel.VeryHigh => 0.25m,
            _ => 1.0m
        };
        
        return baseStake * riskMultiplier;
    }
}

/// <summary>
/// Configuration for scalping strategy.
/// </summary>
public class ScalpingConfiguration : StrategyConfiguration
{
    public int MinHistoryDepth { get; init; } = 5;
    public decimal MinMomentumThreshold { get; init; } = 0.5m; // 0.5% price change
    public decimal MinVelocityThreshold { get; init; } = 0.1m; // 0.1% per minute
    public decimal MinLiquidityScore { get; init; } = 0.5m;
    public decimal MaxSpread { get; init; } = 0.05m; // 5 ticks
    public int MomentumPeriod { get; init; } = 5; // Last 5 snapshots
    public double MinExpectedROI { get; init; } = 0.3; // 0.3% minimum profit
    public int StopLossTicks { get; init; } = 2;
    public int TakeProfitTicks { get; init; } = 3;
    public int SignalValiditySeconds { get; init; } = 30; // Scalping is time-sensitive
    public decimal BaseStake { get; init; } = 50m; // £50 base stake
}
