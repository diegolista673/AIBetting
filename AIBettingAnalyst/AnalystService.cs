using AIBettingCore.Models;
using AIBettingAnalyst.Models;
using AIBettingAnalyst.Analyzers;
using AIBettingAnalyst.Strategies;
using AIBettingAnalyst.Configuration;
using StackExchange.Redis;
using System.Text.Json;
using Serilog;
using Prometheus;
using Microsoft.Extensions.Configuration;

namespace AIBettingAnalyst;

/// <summary>
/// Main Analyst service that subscribes to price updates from Redis,
/// performs analysis, and publishes trading signals.
/// </summary>
public class AnalystService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SurebetDetector _surebetDetector;
    private readonly WAPCalculator _wapCalculator;
    private readonly WeightOfMoneyAnalyzer _womAnalyzer;
    private readonly StrategyOrchestrator _orchestrator;

    private readonly Dictionary<string, List<MarketSnapshot>> _marketHistory = new();
    private const int MaxHistoryDepth = 15; // Keep last 15 snapshots per market

    private readonly List<MarketSnapshot> _historicalSnapshots = new();

    // Prometheus metrics
    private static readonly Counter SnapshotsProcessed = Metrics.WithCustomRegistry(Metrics.DefaultRegistry).CreateCounter(
        "aibetting_analyst_snapshots_processed_total",
        "Total market snapshots processed by Analyst"
    );

    private static readonly Counter SignalsGenerated = Metrics.WithCustomRegistry(Metrics.DefaultRegistry).CreateCounter(
        "aibetting_analyst_signals_generated_total",
        "Total trading signals generated",
        new CounterConfiguration { LabelNames = new[] { "strategy" } }
    );

    private static readonly Counter SurebetsFound = Metrics.WithCustomRegistry(Metrics.DefaultRegistry).CreateCounter(
        "aibetting_analyst_surebets_found_total",
        "Total surebet opportunities detected"
    );

    private static readonly Histogram ProcessingLatency = Metrics.WithCustomRegistry(Metrics.DefaultRegistry).CreateHistogram(
        "aibetting_analyst_processing_latency_seconds",
        "Time to analyze a market snapshot"
    );

    private static readonly Gauge AverageExpectedROI = Metrics.WithCustomRegistry(Metrics.DefaultRegistry).CreateGauge(
        "aibetting_analyst_average_expected_roi",
        "Average expected ROI of generated signals"
    );

    private long _totalSignalsGenerated = 0;
    private double _sumExpectedROI = 0;

    public AnalystService(
        IConnectionMultiplexer redis,
        IConfiguration configuration,
        decimal minSurebetProfit = 0.5m,
        int wapLevels = 3)
    {
        _redis = redis;
        _jsonOptions = new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        
        _surebetDetector = new SurebetDetector(minProfitPercent: minSurebetProfit);
        _wapCalculator = new WAPCalculator(maxLevels: wapLevels);
        _womAnalyzer = new WeightOfMoneyAnalyzer();
        
        // Initialize PRO strategies from configuration
        var proConfig = configuration.GetSection("Analyst:ProStrategies").Get<ProStrategiesConfiguration>() 
            ?? new ProStrategiesConfiguration();
        
        _orchestrator = InitializeStrategies(proConfig);

        Log.Information("📊 Analyst Service initialized");
        Log.Information("   Min surebet profit: {Profit}%", minSurebetProfit);
        Log.Information("   WAP levels: {Levels}", wapLevels);
        Log.Information("   Pro Strategies: {Status}", proConfig.Enabled ? "ENABLED" : "DISABLED");
    }
    
    /// <summary>
    /// Initialize strategies from configuration.
    /// </summary>
    private StrategyOrchestrator InitializeStrategies(ProStrategiesConfiguration config)
    {
        var strategies = new List<IAnalysisStrategy>();
        
        if (!config.Enabled)
        {
            Log.Information("Pro Strategies are disabled in configuration");
            return new StrategyOrchestrator(strategies, new OrchestratorConfiguration());
        }
        
        // Scalping Strategy
        if (config.Scalping.Enabled)
        {
            var scalpConfig = new ScalpingConfiguration
            {
                Enabled = config.Scalping.Enabled,
                MinLiquidity = config.Scalping.MinLiquidity,
                MinTimeToStart = config.Scalping.MinTimeToStart,
                MinOdds = config.Scalping.MinOdds,
                MaxOdds = config.Scalping.MaxOdds,
                MinConfidence = config.Scalping.MinConfidence,
                MinHistoryDepth = config.Scalping.MinHistoryDepth,
                MinMomentumThreshold = config.Scalping.MinMomentumThreshold,
                MinVelocityThreshold = config.Scalping.MinVelocityThreshold,
                MinLiquidityScore = config.Scalping.MinLiquidityScore,
                MaxSpread = config.Scalping.MaxSpread,
                MomentumPeriod = config.Scalping.MomentumPeriod,
                MinExpectedROI = config.Scalping.MinExpectedROI,
                StopLossTicks = config.Scalping.StopLossTicks,
                TakeProfitTicks = config.Scalping.TakeProfitTicks,
                SignalValiditySeconds = config.Scalping.SignalValiditySeconds,
                BaseStake = config.Scalping.BaseStake
            };
            strategies.Add(new ScalpingStrategy(scalpConfig));
            Log.Information("✅ Scalping Strategy enabled");
        }
        
        // Steam Move Strategy
        if (config.SteamMove.Enabled)
        {
            var steamConfig = new SteamMoveConfiguration
            {
                Enabled = config.SteamMove.Enabled,
                MinLiquidity = config.SteamMove.MinLiquidity,
                MinTimeToStart = config.SteamMove.MinTimeToStart,
                MinOdds = config.SteamMove.MinOdds,
                MaxOdds = config.SteamMove.MaxOdds,
                MinConfidence = config.SteamMove.MinConfidence,
                MinHistoryDepth = config.SteamMove.MinHistoryDepth,
                MinVolumeSpikeMultiplier = config.SteamMove.MinVolumeSpikeMultiplier,
                MinPriceMovement = config.SteamMove.MinPriceMovement,
                MinAcceleration = config.SteamMove.MinAcceleration,
                MinWoMShift = config.SteamMove.MinWoMShift,
                VolumePeriod = config.SteamMove.VolumePeriod,
                PriceMovementPeriod = config.SteamMove.PriceMovementPeriod,
                AccelerationPeriod = config.SteamMove.AccelerationPeriod,
                WoMPeriod = config.SteamMove.WoMPeriod,
                MinExpectedROI = config.SteamMove.MinExpectedROI,
                SignalValiditySeconds = config.SteamMove.SignalValiditySeconds,
                BaseStake = config.SteamMove.BaseStake
            };
            strategies.Add(new SteamMoveStrategy(steamConfig));
            Log.Information("✅ Steam Move Strategy enabled");
        }
        
        // Green-Up Strategy
        if (config.GreenUp.Enabled)
        {
            var greenConfig = new GreenUpConfiguration
            {
                Enabled = config.GreenUp.Enabled,
                MinLiquidity = config.GreenUp.MinLiquidity,
                MinTimeToStart = config.GreenUp.MinTimeToStart,
                MinOdds = config.GreenUp.MinOdds,
                MaxOdds = config.GreenUp.MaxOdds,
                MinConfidence = config.GreenUp.MinConfidence,
                MinPriceImprovement = config.GreenUp.MinPriceImprovement,
                MinProfitThreshold = config.GreenUp.MinProfitThreshold
            };
            strategies.Add(new GreenUpStrategy(greenConfig));
            Log.Information("✅ Green-Up Strategy enabled");
        }
        
        // Value Bet Strategy
        if (config.ValueBet.Enabled)
        {
            var valueConfig = new ValueBetConfiguration
            {
                Enabled = config.ValueBet.Enabled,
                MinLiquidity = config.ValueBet.MinLiquidity,
                MinTimeToStart = config.ValueBet.MinTimeToStart,
                MinOdds = config.ValueBet.MinOdds,
                MaxOdds = config.ValueBet.MaxOdds,
                MinConfidence = config.ValueBet.MinConfidence,
                MinValuePercentage = config.ValueBet.MinValuePercentage,
                MinExpectedValue = config.ValueBet.MinExpectedValue,
                KellyFraction = config.ValueBet.KellyFraction,
                MaxStake = config.ValueBet.MaxStake
            };
            strategies.Add(new ValueBetStrategy(valueConfig));
            Log.Information("✅ Value Bet Strategy enabled");
        }
        
        // Parse MaxRisk enum
        var maxRisk = Enum.TryParse<RiskLevel>(config.Orchestrator.MaxRisk, out var risk) 
            ? risk 
            : RiskLevel.High;
        
        // Orchestrator Configuration
        var orchestratorConfig = new OrchestratorConfiguration
        {
            MinConfidence = config.Orchestrator.MinConfidence,
            MinExpectedROI = config.Orchestrator.MinExpectedROI,
            MaxRisk = maxRisk,
            MaxSignalsPerAnalysis = config.Orchestrator.MaxSignalsPerAnalysis,
            ConflictThreshold = config.Orchestrator.ConflictThreshold
        };
        
        return new StrategyOrchestrator(strategies, orchestratorConfig);
    }

    /// <summary>
    /// Start subscribing to price updates and analyzing markets.
    /// </summary>
    public async Task RunAsync(CancellationToken ct)
    {
        var subscriber = _redis.GetSubscriber();
        var channel = new RedisChannel("channel:price-updates", RedisChannel.PatternMode.Literal);

        Log.Information("🔔 Subscribing to Redis channel: {Channel}", channel);

        await subscriber.SubscribeAsync(channel, async (redisChannel, message) =>
        {
            if (ct.IsCancellationRequested)
            {
                return;
            }

            try
            {
                using (ProcessingLatency.NewTimer())
                {
                    await ProcessPriceUpdate(message!.ToString());
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing price update");
            }
        });

        Log.Information("✅ Analyst active - monitoring price updates");
        Log.Information("💡 Press Ctrl+C to stop");

        // Keep running until cancelled
        try
        {
            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (TaskCanceledException)
        {
            Log.Information("Analyst stopping...");
        }

        await subscriber.UnsubscribeAsync(channel);
        Log.Information("Analyst stopped");
    }

    /// <summary>
    /// Process a single price update message.
    /// </summary>
    private async Task ProcessPriceUpdate(string message)
    {
        // Parse notification (contains marketId)
        var notification = JsonSerializer.Deserialize<PriceUpdateNotification>(message, _jsonOptions);
        if (notification == null)
        {
            return;
        }

        // Extract market ID (handle both string and object format)
        string marketIdValue = notification.MarketId.Value;
        
        // Fetch full snapshot from Redis
        var db = _redis.GetDatabase();
        var snapshotKey = $"prices:{marketIdValue}:{notification.Timestamp:O}";
        var snapshotJson = await db.StringGetAsync(snapshotKey);

        if (snapshotJson.IsNullOrEmpty)
        {
            Log.Warning("Snapshot not found for key: {Key}", snapshotKey);
            return;
        }

        var snapshot = JsonSerializer.Deserialize<MarketSnapshot>(snapshotJson.ToString(), _jsonOptions);
        if (snapshot == null)
        {
            return;
        }

        SnapshotsProcessed.Inc();

        // // Track market history (limited by MaxHistoryDepth)
        // if (!_marketHistory.ContainsKey(snapshot.MarketId.Value))
        // {
        //     _marketHistory[snapshot.MarketId.Value] = new List<MarketSnapshot>();
        // }
        // var history = _marketHistory[snapshot.MarketId.Value];
        // history.Add(snapshot);
        // if (history.Count > MaxHistoryDepth)
        // {
        //     history.RemoveAt(0); // Remove oldest snapshot
        // }

        // Perform analysis
        await AnalyzeMarket(snapshot);

        // Log progress every 50 snapshots
        if (_totalSignalsGenerated > 0 && _totalSignalsGenerated % 50 == 0)
        {
            var avgROI = _sumExpectedROI / _totalSignalsGenerated;
            Log.Information("📈 Analysis stats: {Snapshots} processed, {Signals} signals, avg ROI: {ROI:F2}%",
                SnapshotsProcessed.Value,
                _totalSignalsGenerated,
                avgROI);
        }
    }

    /// <summary>
    /// Analyze market snapshot and generate trading signals if opportunities found.
    /// </summary>
    private async Task AnalyzeMarket(MarketSnapshot snapshot)
    {
        Log.Debug("Analyzing market: {EventName} ({MarketId})", snapshot.EventName, snapshot.MarketId.Value);

        // Store snapshot in history
        var marketId = snapshot.MarketId.Value;
        if (!_marketHistory.ContainsKey(marketId))
        {
            _marketHistory[marketId] = new List<MarketSnapshot>();
        }
        
        _marketHistory[marketId].Insert(0, snapshot); // Insert at beginning (most recent first)
        
        // Keep only last N snapshots
        if (_marketHistory[marketId].Count > MaxHistoryDepth)
        {
            _marketHistory[marketId].RemoveAt(_marketHistory[marketId].Count - 1);
        }

        // 1. Calculate WAP for all runners
        var wapResults = new Dictionary<string, WAPResult>();
        foreach (var runner in snapshot.Runners)
        {
            wapResults[runner.SelectionId.Value] = _wapCalculator.Calculate(runner);
        }

        // 2. Analyze Weight of Money
        var womResults = _womAnalyzer.Analyze(snapshot);
        
        // Log WoM distribution
        if (womResults.Any())
        {
            var favorite = womResults.First();
            Log.Debug("WoM: {Selection} has {Percentage:F1}% of back volume",
                favorite.SelectionName, favorite.BackPercentage);
        }

        // 3. Detect surebets
        var surebets = _surebetDetector.DetectOpportunities(snapshot);
        
        if (surebets.Any())
        {
            SurebetsFound.Inc(surebets.Count);
            
            foreach (var surebet in surebets)
            {
                await PublishTradingSignal(snapshot, surebet, wapResults[surebet.BackSelectionId]);
            }
        }

        // 4. Analyze market with PRO strategies orchestrator
        var historicalSnapshots = _marketHistory.GetValueOrDefault(marketId) ?? new List<MarketSnapshot>();
        var context = new AnalysisContext {
            HistoricalSnapshots = historicalSnapshots,
            MarketAge = (int)CalculateMarketAge(snapshot),
            Timestamp = DateTimeOffset.UtcNow
        };

        var proSignals = await _orchestrator.AnalyzeMarketAsync(snapshot, context);
        foreach (var signal in proSignals) 
        {
            await PublishStrategySignal(signal);
        }
    }

    /// <summary>
    /// Publish trading signal to Redis for Executor to consume.
    /// </summary>
    private async Task PublishTradingSignal(
        MarketSnapshot snapshot,
        SurebetOpportunity surebet,
        WAPResult wap)
    {
        var signal = new AnalystSignal
        {
            MarketId = snapshot.MarketId.Value,
            Strategy = "surebet",
            Timestamp = DateTimeOffset.UtcNow,
            Confidence = surebet.ProfitPercentage > 1.0 ? 0.9 : 0.7,
            ExpectedROI = surebet.ProfitPercentage,
            
            BackSelectionId = surebet.BackSelectionId,
            BackSelectionName = surebet.BackSelectionName,
            BackOdds = surebet.BackOdds,
            StakeBack = surebet.StakeBack,
            
            LaySelectionId = surebet.LaySelectionId,
            LaySelectionName = surebet.LaySelectionName,
            LayOdds = surebet.LayOdds,
            StakeLay = surebet.StakeLay,
            
            TotalMatched = snapshot.TotalMatched ?? 0,
            SecondsToStart = snapshot.SecondsToStart ?? 0,
            
            BackWAP = wap.BackWAP,
            LayWAP = wap.LayWAP,
            Spread = wap.LayWAP - wap.BackWAP,
            
            Reason = $"Arbitrage opportunity: Back {surebet.BackOdds} / Lay {surebet.LayOdds}"
        };

        // Publish to Redis
        var db = _redis.GetDatabase();
        var publisher = _redis.GetSubscriber();
        
        var signalJson = JsonSerializer.Serialize(signal, _jsonOptions);
        
        // Store signal
        var signalKey = $"signals:{signal.MarketId}:{signal.Timestamp:O}";
        await db.StringSetAsync(signalKey, signalJson, TimeSpan.FromHours(1));
        
        // Publish notification
        await publisher.PublishAsync(
            new RedisChannel("channel:trading-signals", RedisChannel.PatternMode.Literal),
            signalJson
        );

        // Update metrics
        SignalsGenerated.WithLabels("surebet").Inc();
        _totalSignalsGenerated++;
        _sumExpectedROI += signal.ExpectedROI;
        AverageExpectedROI.Set(_sumExpectedROI / _totalSignalsGenerated);

        Log.Information("✅ SIGNAL PUBLISHED: {Strategy} on {Selection} - Expected ROI: {ROI:F2}%",
            signal.Strategy,
            signal.BackSelectionName,
            signal.ExpectedROI);
    }

    /// <summary>
    /// Publish strategy signal to Redis for Executor to consume.
    /// </summary>
    private async Task PublishStrategySignal(StrategySignal signal)
    {
        var db = _redis.GetDatabase();
        var publisher = _redis.GetSubscriber();
        
        var signalJson = JsonSerializer.Serialize(signal, _jsonOptions);
        
        // Store signal
        var signalKey = $"strategy-signals:{signal.MarketId}:{signal.Timestamp:O}";
        await db.StringSetAsync(signalKey, signalJson, TimeSpan.FromHours(1));
        
        // Publish notification
        await publisher.PublishAsync(
            new RedisChannel("channel:strategy-signals", RedisChannel.PatternMode.Literal),
            signalJson
        );

        // Update metrics
        SignalsGenerated.WithLabels(signal.Strategy).Inc();
        _totalSignalsGenerated++;
        _sumExpectedROI += signal.ExpectedROI;
        AverageExpectedROI.Set(_sumExpectedROI / _totalSignalsGenerated);

        Log.Information("✅ STRATEGY SIGNAL: {Strategy} {SignalType} on {Selection} - ROI: {ROI:F2}%, Confidence: {Conf:F2}",
            signal.Strategy,
            signal.SignalType,
            signal.PrimarySelection?.SelectionName ?? "N/A",
            signal.ExpectedROI,
            signal.Confidence);
    }

    /// <summary>
    /// Calculate market age in seconds.
    /// </summary>
    private double CalculateMarketAge(MarketSnapshot snapshot)
    {
        var now = DateTimeOffset.UtcNow;
        var marketStart = snapshot.Timestamp;
        return (now - marketStart).TotalSeconds;
    }
}

/// <summary>
/// Notification message published on channel:price-updates.
/// Supports both string and object format for MarketId to handle Explorer's serialization.
/// </summary>
internal class PriceUpdateNotification
{
    [System.Text.Json.Serialization.JsonPropertyName("marketId")]
    public MarketIdWrapper MarketId { get; init; } = null!;
    
    public DateTimeOffset Timestamp { get; init; }
    public decimal TotalMatched { get; init; }
    public int RunnersCount { get; init; }
}

/// <summary>
/// Wrapper to handle both string and object format for MarketId.
/// </summary>
internal class MarketIdWrapper
{
    public string Value { get; init; } = string.Empty;
    
    // Custom converter to handle { "value": "1.200000000" } or "1.200000000"
    public static implicit operator string(MarketIdWrapper wrapper) => wrapper?.Value ?? string.Empty;
}
