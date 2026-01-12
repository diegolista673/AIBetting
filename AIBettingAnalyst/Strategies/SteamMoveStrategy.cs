using AIBettingCore.Models;
using AIBettingAnalyst.Models;
using Serilog;

namespace AIBettingAnalyst.Strategies;

/// <summary>
/// Steam Move Detection: identifies sudden large money influx indicating insider information
/// or significant market sentiment shift. "Steam" refers to informed money moving the market.
/// </summary>
public class SteamMoveStrategy : AnalyzerBase
{
    private readonly SteamMoveConfiguration _steamConfig;
    
    public override string StrategyName => "STEAM_MOVE";
    public override string Description => "Detects sudden volume spikes and sharp price movements";
    
    public SteamMoveStrategy(SteamMoveConfiguration config, ILogger? logger = null)
        : base(config, logger)
    {
        _steamConfig = config;
    }
    
    public override async Task<IEnumerable<StrategySignal>> AnalyzeAsync(
        MarketSnapshot snapshot,
        AnalysisContext context)
    {
        if (!CanAnalyze(snapshot))
            return Enumerable.Empty<StrategySignal>();
        
        // Need sufficient history to detect "sudden" changes
        if (context.HistoricalSnapshots.Count < _steamConfig.MinHistoryDepth)
            return Enumerable.Empty<StrategySignal>();
        
        var signals = new List<StrategySignal>();
        
        foreach (var runner in snapshot.Runners)
        {
            var signal = await DetectSteamMoveAsync(runner, snapshot, context);
            if (signal != null)
                signals.Add(signal);
        }
        
        return signals;
    }
    
    private async Task<StrategySignal?> DetectSteamMoveAsync(
        RunnerSnapshot runner,
        MarketSnapshot snapshot,
        AnalysisContext context)
    {
        // 1. Calculate volume spike
        var volumeSpike = CalculateVolumeSpike(runner, context.HistoricalSnapshots);
        if (volumeSpike < _steamConfig.MinVolumeSpikeMultiplier)
            return null;
        
        // 2. Detect sharp price movement
        var priceMovement = CalculatePriceMovement(runner, context.HistoricalSnapshots);
        if (Math.Abs(priceMovement) < _steamConfig.MinPriceMovement)
            return null;
        
        // 3. Check price acceleration (movement is accelerating, not slowing)
        var acceleration = CalculateAcceleration(runner, context.HistoricalSnapshots);
        if (acceleration < _steamConfig.MinAcceleration)
            return null;
        
        // 4. Verify weight of money shift
        var womShift = CalculateWeightOfMoneyShift(runner, context.HistoricalSnapshots);
        if (Math.Abs(womShift) < _steamConfig.MinWoMShift)
            return null;
        
        // 5. Check market pressure
        var pressure = CalculateMarketPressure(runner);
        var pressureDirection = pressure > 0 ? "BULLISH" : "BEARISH";
        
        // 6. Determine direction (follow the steam)
        var direction = priceMovement < 0 ? TradeAction.Back : TradeAction.Lay;
        var currentPrice = runner.LastPriceTraded ?? GetMidPrice(runner);
        
        if (!IsOddsValid(currentPrice))
            return null;
        
        // 7. Calculate confidence based on multiple factors
        var confidence = CalculateConfidence(
            volumeSpike, priceMovement, acceleration, womShift, Math.Abs(pressure));
        
        if (confidence < _config.MinConfidence)
            return null;
        
        // 8. Estimate ROI (steam moves can be very profitable)
        var expectedROI = CalculateExpectedROI(priceMovement, volumeSpike);
        if (expectedROI < _steamConfig.MinExpectedROI)
            return null;
        
        // 9. Risk assessment
        var risk = DetermineRisk(volumeSpike, priceMovement, snapshot.SecondsToStart ?? 0);
        
        _logger.Warning(
            "{Strategy}: STEAM DETECTED on {Selection}! " +
            "Volume Spike: {VolumeSpike:F1}x, Price Move: {PriceMove:F2}%, " +
            "Acceleration: {Accel:F2}, WoM Shift: {WoM:F2}%, Pressure: {Pressure} " +
            "Confidence: {Confidence:F2}, ROI: {ROI:F2}%",
            StrategyName, runner.RunnerName, volumeSpike, priceMovement,
            acceleration, womShift, pressureDirection, confidence, expectedROI);
        
        var signal = CreateSignal(
            snapshot,
            $"STEAM_{pressureDirection}",
            confidence,
            expectedROI,
            risk,
            $"Steam move detected: {volumeSpike:F1}x volume spike, " +
            $"{priceMovement:F2}% price movement, {womShift:F1}% WoM shift");
        
        return signal with
        {
            Action = direction,
            Priority = 95, // Very high priority - time critical
            ValidityWindow = _steamConfig.SignalValiditySeconds,
            PrimarySelection = new SelectionSignal
            {
                SelectionId = runner.SelectionId.Value,
                SelectionName = runner.RunnerName,
                RecommendedOdds = currentPrice,
                Stake = CalculateStake(expectedROI, confidence, risk),
                BetType = direction == TradeAction.Back ? BetType.Back : BetType.Lay,
                StopLoss = null, // No stop loss for steam (ride it out)
                TakeProfit = CalculateTakeProfit(currentPrice, priceMovement, direction)
            },
            Metadata = new Dictionary<string, object>
            {
                ["volumeSpike"] = volumeSpike,
                ["priceMovement"] = priceMovement,
                ["acceleration"] = acceleration,
                ["womShift"] = womShift,
                ["marketPressure"] = pressure,
                ["steamStrength"] = CalculateSteamStrength(volumeSpike, priceMovement)
            }
        };
    }
    
    /// <summary>
    /// Calculate volume spike compared to average.
    /// </summary>
    private decimal CalculateVolumeSpike(RunnerSnapshot current, List<MarketSnapshot> history)
    {
        var currentVolume = current.TotalMatched;
        
        // Calculate average historical volume
        var historicalVolumes = new List<decimal>();
        foreach (var snapshot in history.Take(_steamConfig.VolumePeriod))
        {
            var historicalRunner = snapshot.Runners.FirstOrDefault(
                r => r.SelectionId.Value == current.SelectionId.Value);
            
            if (historicalRunner != null)
                historicalVolumes.Add(historicalRunner.TotalMatched);
        }
        
        if (!historicalVolumes.Any() || historicalVolumes.Average() == 0m)
            return 0m;
        
        var avgVolume = historicalVolumes.Average();
        return currentVolume / avgVolume; // Returns multiplier (e.g., 3.5x)
    }
    
    /// <summary>
    /// Calculate price movement percentage.
    /// </summary>
    private decimal CalculatePriceMovement(RunnerSnapshot current, List<MarketSnapshot> history)
    {
        var currentPrice = current.LastPriceTraded ?? GetMidPrice(current);
        
        var historicalRunner = history.Skip(_steamConfig.PriceMovementPeriod - 1)
            .FirstOrDefault()?
            .Runners.FirstOrDefault(r => r.SelectionId.Value == current.SelectionId.Value);
        
        if (historicalRunner == null) return 0m;
        
        var oldPrice = historicalRunner.LastPriceTraded ?? GetMidPrice(historicalRunner);
        if (oldPrice == 0m) return 0m;
        
        return ((currentPrice - oldPrice) / oldPrice) * 100m;
    }
    
    /// <summary>
    /// Calculate acceleration (change in momentum).
    /// </summary>
    private decimal CalculateAcceleration(RunnerSnapshot current, List<MarketSnapshot> history)
    {
        if (history.Count < _steamConfig.AccelerationPeriod + 2)
            return 0m;
        
        // Calculate momentum at two different time windows
        var recentMomentum = CalculateMomentumForPeriod(current, history, 0, _steamConfig.AccelerationPeriod / 2);
        var olderMomentum = CalculateMomentumForPeriod(current, history, _steamConfig.AccelerationPeriod / 2, _steamConfig.AccelerationPeriod);
        
        // Acceleration = change in momentum
        return recentMomentum - olderMomentum;
    }
    
    private decimal CalculateMomentumForPeriod(
        RunnerSnapshot current,
        List<MarketSnapshot> history,
        int skipCount,
        int takeCount)
    {
        var currentPrice = current.LastPriceTraded ?? GetMidPrice(current);
        
        var historicalRunner = history.Skip(skipCount).Skip(takeCount - 1)
            .FirstOrDefault()?
            .Runners.FirstOrDefault(r => r.SelectionId.Value == current.SelectionId.Value);
        
        if (historicalRunner == null) return 0m;
        
        var oldPrice = historicalRunner.LastPriceTraded ?? GetMidPrice(historicalRunner);
        if (oldPrice == 0m) return 0m;
        
        return ((currentPrice - oldPrice) / oldPrice) * 100m;
    }
    
    /// <summary>
    /// Calculate shift in weight of money percentage.
    /// </summary>
    private decimal CalculateWeightOfMoneyShift(RunnerSnapshot current, List<MarketSnapshot> history)
    {
        var currentBackVolume = current.AvailableToBack?.Sum(p => p.Size) ?? 0m;
        var currentLayVolume = current.AvailableToLay?.Sum(p => p.Size) ?? 0m;
        var currentTotal = currentBackVolume + currentLayVolume;
        
        if (currentTotal == 0m) return 0m;
        var currentBackPercent = (currentBackVolume / currentTotal) * 100m;
        
        // Historical average
        var historicalBackPercents = new List<decimal>();
        foreach (var snapshot in history.Take(_steamConfig.WoMPeriod))
        {
            var historicalRunner = snapshot.Runners.FirstOrDefault(
                r => r.SelectionId.Value == current.SelectionId.Value);
            
            if (historicalRunner != null)
            {
                var backVol = historicalRunner.AvailableToBack?.Sum(p => p.Size) ?? 0m;
                var layVol = historicalRunner.AvailableToLay?.Sum(p => p.Size) ?? 0m;
                var total = backVol + layVol;
                
                if (total > 0m)
                    historicalBackPercents.Add((backVol / total) * 100m);
            }
        }
        
        if (!historicalBackPercents.Any()) return 0m;
        
        var avgBackPercent = historicalBackPercents.Average();
        return currentBackPercent - avgBackPercent;
    }
    
    /// <summary>
    /// Calculate market pressure (positive = bullish, negative = bearish).
    /// </summary>
    private decimal CalculateMarketPressure(RunnerSnapshot runner)
    {
        var backVolume = runner.AvailableToBack?.Take(3).Sum(p => p.Size) ?? 0m;
        var layVolume = runner.AvailableToLay?.Take(3).Sum(p => p.Size) ?? 0m;
        
        if (backVolume + layVolume == 0m) return 0m;
        
        // Return ratio: >1 = more back money (bullish), <1 = more lay money (bearish)
        return layVolume > 0m ? backVolume / layVolume : backVolume;
    }
    
    private decimal GetMidPrice(RunnerSnapshot runner)
    {
        var bestBack = runner.AvailableToBack?.FirstOrDefault()?.Price ?? 0m;
        var bestLay = runner.AvailableToLay?.FirstOrDefault()?.Price ?? 0m;
        
        if (bestBack == 0m && bestLay == 0m) return 0m;
        if (bestBack == 0m) return bestLay;
        if (bestLay == 0m) return bestBack;
        
        return (bestBack + bestLay) / 2m;
    }
    
    private double CalculateConfidence(
        decimal volumeSpike,
        decimal priceMovement,
        decimal acceleration,
        decimal womShift,
        decimal pressure)
    {
        // High volume + price move + acceleration = high confidence
        var volumeScore = Math.Min((double)volumeSpike / 5.0, 1.0); // 5x = max
        var priceScore = Math.Min(Math.Abs((double)priceMovement) / 10.0, 1.0); // 10% = max
        var accelScore = Math.Min(Math.Abs((double)acceleration) / 5.0, 1.0); // 5% = max
        var womScore = Math.Min(Math.Abs((double)womShift) / 20.0, 1.0); // 20% shift = max
        var pressureScore = Math.Min((double)pressure / 3.0, 1.0); // 3:1 ratio = max
        
        return (volumeScore * 0.35 + priceScore * 0.25 + accelScore * 0.2 + 
                womScore * 0.15 + pressureScore * 0.05);
    }
    
    private double CalculateExpectedROI(decimal priceMovement, decimal volumeSpike)
    {
        // Strong steam moves can continue - estimate based on momentum
        var baseROI = Math.Abs((double)priceMovement) * 0.5; // 50% of current movement
        var volumeBonus = ((double)volumeSpike - 1.0) * 0.2; // Bonus for volume
        
        return Math.Min(baseROI + volumeBonus, 10.0); // Cap at 10%
    }
    
    private RiskLevel DetermineRisk(decimal volumeSpike, decimal priceMovement, int secondsToStart)
    {
        // Very strong steam = lower risk (market knows something)
        if (volumeSpike > 5m && Math.Abs(priceMovement) > 5m)
            return RiskLevel.Low;
        
        if (volumeSpike > 3m && Math.Abs(priceMovement) > 3m)
            return RiskLevel.Medium;
        
        // Close to start = higher risk
        if (secondsToStart < 300)
            return RiskLevel.High;
        
        return RiskLevel.Medium;
    }
    
    private decimal CalculateStake(double expectedROI, double confidence, RiskLevel risk)
    {
        var baseStake = _steamConfig.BaseStake;
        
        // Increase stake for high confidence steam
        var confidenceMultiplier = (decimal)(1.0 + confidence);
        
        // Adjust by risk
        var riskMultiplier = risk switch
        {
            RiskLevel.Low => 2.0m,
            RiskLevel.Medium => 1.5m,
            RiskLevel.High => 1.0m,
            _ => 0.5m
        };
        
        return baseStake * confidenceMultiplier * riskMultiplier;
    }
    
    private decimal? CalculateTakeProfit(decimal currentPrice, decimal priceMovement, TradeAction direction)
    {
        // Take profit at 2x the current movement
        var targetMove = Math.Abs(priceMovement) * 2m / 100m;
        
        if (direction == TradeAction.Back)
            return Math.Max(1.01m, currentPrice * (1m - targetMove));
        else
            return currentPrice * (1m + targetMove);
    }
    
    private double CalculateSteamStrength(decimal volumeSpike, decimal priceMovement)
    {
        return (double)(volumeSpike * Math.Abs(priceMovement));
    }
}

public class SteamMoveConfiguration : StrategyConfiguration
{
    public int MinHistoryDepth { get; init; } = 10;
    public decimal MinVolumeSpikeMultiplier { get; init; } = 2.0m; // 2x average
    public decimal MinPriceMovement { get; init; } = 2.0m; // 2% price change
    public decimal MinAcceleration { get; init; } = 0.5m; // Momentum increasing
    public decimal MinWoMShift { get; init; } = 10m; // 10% shift in money
    public int VolumePeriod { get; init; } = 5;
    public int PriceMovementPeriod { get; init; } = 3;
    public int AccelerationPeriod { get; init; } = 6;
    public int WoMPeriod { get; init; } = 5;
    public double MinExpectedROI { get; init; } = 1.0; // 1% minimum
    public int SignalValiditySeconds { get; init; } = 20; // Very time-sensitive
    public decimal BaseStake { get; init; } = 100m; // £100 base (steam = strong signal)
}
