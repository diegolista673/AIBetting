using AIBettingAnalyst.Models;
using AIBettingCore.Models;
using Serilog;
using StackExchange.Redis;
using System.Text.Json;

namespace AIBettingExecutor.SignalProcessing;

/// <summary>
/// Subscribes to trading signals from Redis and converts them to order requests.
/// </summary>
public class SignalProcessor
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SignalProcessorConfiguration _config;

    public event Func<PlaceOrderRequest, Task>? OnSignalReceived;
    public event Func<StrategySignal, Task>? OnStrategySignalReceived;

    public SignalProcessor(
        IConnectionMultiplexer redis,
        SignalProcessorConfiguration config,
        ILogger? logger = null)
    {
        _redis = redis;
        _config = config;
        _logger = logger ?? Log.ForContext<SignalProcessor>();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Start subscribing to trading signals from Redis.
    /// </summary>
    public async Task StartAsync(CancellationToken ct)
    {
        var subscriber = _redis.GetSubscriber();

        // Subscribe to legacy surebet signals
        if (_config.SubscribeToSurebetSignals)
        {
            var surebetChannel = new RedisChannel("channel:trading-signals", RedisChannel.PatternMode.Literal);
            await subscriber.SubscribeAsync(surebetChannel, async (channel, message) =>
            {
                if (ct.IsCancellationRequested) return;
                await ProcessAnalystSignalAsync(message!.ToString());
            });
            _logger.Information("✅ Subscribed to: {Channel}", surebetChannel);
        }

        // Subscribe to PRO strategy signals
        if (_config.SubscribeToStrategySignals)
        {
            var strategyChannel = new RedisChannel("channel:strategy-signals", RedisChannel.PatternMode.Literal);
            await subscriber.SubscribeAsync(strategyChannel, async (channel, message) =>
            {
                if (ct.IsCancellationRequested) return;
                await ProcessStrategySignalAsync(message!.ToString());
            });
            _logger.Information("✅ Subscribed to: {Channel}", strategyChannel);
        }

        _logger.Information("🔔 Signal processor active - monitoring for trading signals");
    }

    /// <summary>
    /// Process legacy analyst signal (surebet).
    /// </summary>
    private async Task ProcessAnalystSignalAsync(string message)
    {
        try
        {
            var signal = JsonSerializer.Deserialize<AnalystSignal>(message, _jsonOptions);
            if (signal == null)
            {
                _logger.Warning("Failed to deserialize analyst signal");
                return;
            }

            _logger.Information("📥 Received SUREBET signal: {Strategy} on {Selection}",
                signal.Strategy, signal.BackSelectionName);

            // Convert to PlaceOrderRequest for BACK leg
            if (!string.IsNullOrEmpty(signal.BackSelectionId))
            {
                var backOrder = new PlaceOrderRequest
                {
                    MarketId = new MarketId(signal.MarketId),
                    SelectionId = new SelectionId(signal.BackSelectionId),
                    Side = TradeSide.Back,
                    Odds = signal.BackOdds,
                    Stake = signal.StakeBack,
                    CorrelationId = $"surebet-{signal.Timestamp:yyyyMMddHHmmss}"
                };

                await NotifySignalReceived(backOrder);
            }

            // Convert to PlaceOrderRequest for LAY leg
            if (!string.IsNullOrEmpty(signal.LaySelectionId))
            {
                var layOrder = new PlaceOrderRequest
                {
                    MarketId = new MarketId(signal.MarketId),
                    SelectionId = new SelectionId(signal.LaySelectionId),
                    Side = TradeSide.Lay,
                    Odds = signal.LayOdds,
                    Stake = signal.StakeLay,
                    CorrelationId = $"surebet-{signal.Timestamp:yyyyMMddHHmmss}"
                };

                await NotifySignalReceived(layOrder);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing analyst signal");
        }
    }

    /// <summary>
    /// Process strategy signal (PRO strategies).
    /// </summary>
    private async Task ProcessStrategySignalAsync(string message)
    {
        try
        {
            var signal = JsonSerializer.Deserialize<StrategySignal>(message, _jsonOptions);
            if (signal == null)
            {
                _logger.Warning("Failed to deserialize strategy signal");
                return;
            }

            _logger.Information("📥 Received STRATEGY signal: {Strategy} {Type} on {Selection} - Confidence: {Conf:F2}, ROI: {ROI:F2}%",
                signal.Strategy, signal.SignalType,
                signal.PrimarySelection?.SelectionName ?? "N/A",
                signal.Confidence, signal.ExpectedROI);

            // Check if signal is still valid
            var age = (DateTimeOffset.UtcNow - signal.Timestamp).TotalSeconds;
            if (age > signal.ValidityWindow)
            {
                _logger.Warning("Signal {SignalId} expired (age: {Age}s, validity: {Validity}s)",
                    signal.SignalId, age, signal.ValidityWindow);
                return;
            }

            // Notify strategy signal handlers
            if (OnStrategySignalReceived != null)
            {
                await OnStrategySignalReceived.Invoke(signal);
            }

            // Convert primary selection to order
            if (signal.PrimarySelection != null)
            {
                var primaryOrder = ConvertToOrderRequest(signal, signal.PrimarySelection);
                await NotifySignalReceived(primaryOrder);
            }

            // Convert secondary selection (for hedge/arbitrage)
            if (signal.SecondarySelection != null)
            {
                var secondaryOrder = ConvertToOrderRequest(signal, signal.SecondarySelection);
                await NotifySignalReceived(secondaryOrder);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing strategy signal");
        }
    }

    /// <summary>
    /// Convert strategy signal to order request.
    /// </summary>
    private PlaceOrderRequest ConvertToOrderRequest(StrategySignal signal, SelectionSignal selection)
    {
        return new PlaceOrderRequest
        {
            MarketId = new MarketId(signal.MarketId),
            SelectionId = new SelectionId(selection.SelectionId),
            Side = selection.BetType == BetType.Back ? TradeSide.Back : TradeSide.Lay,
            Odds = selection.RecommendedOdds,
            Stake = selection.Stake,
            CorrelationId = $"{signal.Strategy}-{signal.SignalId}"
        };
    }

    private async Task NotifySignalReceived(PlaceOrderRequest order)
    {
        if (OnSignalReceived != null)
        {
            await OnSignalReceived.Invoke(order);
        }
    }
}

/// <summary>
/// Configuration for signal processor.
/// </summary>
public class SignalProcessorConfiguration
{
    /// <summary>
    /// Subscribe to legacy surebet signals on channel:trading-signals.
    /// </summary>
    public bool SubscribeToSurebetSignals { get; init; } = true;

    /// <summary>
    /// Subscribe to PRO strategy signals on channel:strategy-signals.
    /// </summary>
    public bool SubscribeToStrategySignals { get; init; } = true;

    /// <summary>
    /// Maximum age of signal to process (seconds).
    /// </summary>
    public int MaxSignalAgeSeconds { get; init; } = 60;
}
