namespace AIBettingAnalyst.Configuration;

/// <summary>
/// Configuration for Pro strategies feature.
/// </summary>
public class ProStrategiesConfiguration
{
    public bool Enabled { get; init; } = true;
    public ScalpingConfig Scalping { get; init; } = new();
    public SteamMoveConfig SteamMove { get; init; } = new();
    public GreenUpConfig GreenUp { get; init; } = new();
    public ValueBetConfig ValueBet { get; init; } = new();
    public OrchestratorConfig Orchestrator { get; init; } = new();
}

public class ScalpingConfig
{
    public bool Enabled { get; init; } = true;
    public decimal MinLiquidity { get; init; } = 1000m;
    public int MinTimeToStart { get; init; } = 300;
    public decimal MinOdds { get; init; } = 1.1m;
    public decimal MaxOdds { get; init; } = 100m;
    public double MinConfidence { get; init; } = 0.6;
    public int MinHistoryDepth { get; init; } = 5;
    public decimal MinMomentumThreshold { get; init; } = 0.5m;
    public decimal MinVelocityThreshold { get; init; } = 0.1m;
    public decimal MinLiquidityScore { get; init; } = 0.5m;
    public decimal MaxSpread { get; init; } = 0.05m;
    public int MomentumPeriod { get; init; } = 5;
    public double MinExpectedROI { get; init; } = 0.3;
    public int StopLossTicks { get; init; } = 2;
    public int TakeProfitTicks { get; init; } = 3;
    public int SignalValiditySeconds { get; init; } = 30;
    public decimal BaseStake { get; init; } = 50m;
}

public class SteamMoveConfig
{
    public bool Enabled { get; init; } = true;
    public decimal MinLiquidity { get; init; } = 5000m;
    public int MinTimeToStart { get; init; } = 300;
    public decimal MinOdds { get; init; } = 1.1m;
    public decimal MaxOdds { get; init; } = 100m;
    public double MinConfidence { get; init; } = 0.7;
    public int MinHistoryDepth { get; init; } = 10;
    public decimal MinVolumeSpikeMultiplier { get; init; } = 2.0m;
    public decimal MinPriceMovement { get; init; } = 2.0m;
    public decimal MinAcceleration { get; init; } = 0.5m;
    public decimal MinWoMShift { get; init; } = 10m;
    public int VolumePeriod { get; init; } = 5;
    public int PriceMovementPeriod { get; init; } = 3;
    public int AccelerationPeriod { get; init; } = 6;
    public int WoMPeriod { get; init; } = 5;
    public double MinExpectedROI { get; init; } = 1.0;
    public int SignalValiditySeconds { get; init; } = 20;
    public decimal BaseStake { get; init; } = 100m;
}

public class GreenUpConfig
{
    public bool Enabled { get; init; } = false;
    public decimal MinLiquidity { get; init; } = 1000m;
    public int MinTimeToStart { get; init; } = 60;
    public decimal MinOdds { get; init; } = 1.1m;
    public decimal MaxOdds { get; init; } = 100m;
    public double MinConfidence { get; init; } = 0.6;
    public decimal MinPriceImprovement { get; init; } = 3.0m;
    public decimal MinProfitThreshold { get; init; } = 1.0m;
}

public class ValueBetConfig
{
    public bool Enabled { get; init; } = true;
    public decimal MinLiquidity { get; init; } = 2000m;
    public int MinTimeToStart { get; init; } = 600;
    public decimal MinOdds { get; init; } = 1.5m;
    public decimal MaxOdds { get; init; } = 50m;
    public double MinConfidence { get; init; } = 0.6;
    public decimal MinValuePercentage { get; init; } = 5.0m;
    public decimal MinExpectedValue { get; init; } = 0.05m;
    public decimal KellyFraction { get; init; } = 0.25m;
    public decimal MaxStake { get; init; } = 100m;
}

public class OrchestratorConfig
{
    public double MinConfidence { get; init; } = 0.6;
    public double MinExpectedROI { get; init; } = 0.3;
    public string MaxRisk { get; init; } = "High";
    public int MaxSignalsPerAnalysis { get; init; } = 5;
    public double ConflictThreshold { get; init; } = 10.0;
}
