using AIBettingCore.Interfaces;
using AIBettingCore.Models;
using AIBettingCore.Services;
using AIBettingExecutor.Accounting;
using AIBettingExecutor.BetfairAPI;
using AIBettingExecutor.Configuration;
using AIBettingExecutor.Interfaces;
using AIBettingExecutor.Metrics;
using AIBettingExecutor.OrderManagement;
using AIBettingExecutor.RiskManagement;
using AIBettingExecutor.SignalProcessing;
using Serilog;
using StackExchange.Redis;

namespace AIBettingExecutor;

/// <summary>
/// Main executor service that subscribes to trading signals,
/// validates risk, places orders, and tracks execution.
/// </summary>
public class ExecutorService
{
    private readonly IBetfairClient _betfairClient;
    private readonly OrderManager _orderManager;
    private readonly SignalProcessor _signalProcessor;
    private readonly RiskValidator _riskValidator;
    private readonly TradeLogger _tradeLogger;
    private readonly ILogger _logger;
    private readonly ExecutorConfiguration _config;

    private Timer? _balanceUpdateTimer;
    private Timer? _orderReconciliationTimer;

    public ExecutorService(
        IBetfairClient betfairClient,
        OrderManager orderManager,
        SignalProcessor signalProcessor,
        RiskValidator riskValidator,
        TradeLogger tradeLogger,
        ExecutorConfiguration config,
        ILogger? logger = null)
    {
        _betfairClient = betfairClient;
        _orderManager = orderManager;
        _signalProcessor = signalProcessor;
        _riskValidator = riskValidator;
        _tradeLogger = tradeLogger;
        _config = config;
        _logger = logger ?? Log.ForContext<ExecutorService>();

        // Wire signal processor events
        _signalProcessor.OnSignalReceived += HandleSignalAsync;
    }

    /// <summary>
    /// Start executor service.
    /// </summary>
    public async Task RunAsync(CancellationToken ct)
    {
        _logger.Information("🚀 Executor Service starting...");

        try
        {
            // Step 1: Authenticate with Betfair
            _logger.Information("Authenticating with Betfair API...");
            var authenticated = await _betfairClient.AuthenticateAsync(ct);

            if (!authenticated)
            {
                _logger.Error("❌ Failed to authenticate with Betfair API");
                ExecutorMetrics.BetfairConnectionStatus.Set(0);
                return;
            }

            _logger.Information("✅ Betfair authentication successful");
            ExecutorMetrics.BetfairConnectionStatus.Set(1);

            // Step 2: Get initial account balance
            await UpdateAccountBalanceAsync(ct);

            // Step 3: Start signal processor
            _logger.Information("Starting signal processor...");
            await _signalProcessor.StartAsync(ct);

            // Step 4: Start background tasks
            StartBackgroundTasks();

            _logger.Information("✅ Executor active - monitoring for trading signals");
            _logger.Information("💡 Press Ctrl+C to stop");

            // Keep running until cancelled
            try
            {
                await Task.Delay(Timeout.Infinite, ct);
            }
            catch (TaskCanceledException)
            {
                _logger.Information("Executor stopping...");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Fatal error in executor service");
            throw;
        }
        finally
        {
            await ShutdownAsync();
        }
    }

    /// <summary>
    /// Handle incoming trading signal.
    /// </summary>
    private async Task HandleSignalAsync(PlaceOrderRequest request)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.Information("📥 Signal received: {Side} {Stake}@{Odds} on {Selection}",
                request.Side, request.Stake, request.Odds, request.SelectionId.Value);

            ExecutorMetrics.SignalsReceived.WithLabels(request.CorrelationId ?? "unknown").Inc();

            // Paper trading mode - log but don't execute
            if (_config.Trading.EnablePaperTrading)
            {
                _logger.Information("📄 PAPER TRADING MODE - Signal logged but not executed");
                return;
            }

            // Step 1: Risk validation
            var validation = await _riskValidator.ValidateOrderAsync(request);

            if (!validation.IsValid)
            {
                _logger.Warning("❌ Signal rejected by risk validation: {Reason}", validation.RejectionReason);
                ExecutorMetrics.SignalsRejected.WithLabels(validation.RejectionReason ?? "unknown").Inc();
                return;
            }

            // Step 2: Place order
            _logger.Information("✅ Risk validation passed, placing order...");

            OrderResult result;
            result = await _orderManager.PlaceOrderAsync(request);

            sw.Stop();
            ExecutorMetrics.OrderExecutionLatency.Observe(sw.Elapsed.TotalSeconds);

            // Step 3: Handle result
            if (result.Status == OrderStatus.Cancelled)
            {
                _logger.Error("❌ Order failed: {Message}", result.Message);
                ExecutorMetrics.OrdersFailed.WithLabels(result.Message ?? "unknown").Inc();
                await _riskValidator.RecordOrderFailedAsync(result.OrderId, result.Message ?? "unknown");
                return;
            }

            _logger.Information("✅ Order placed: {OrderId}, Status: {Status}", result.OrderId, result.Status);
            ExecutorMetrics.OrdersPlaced.WithLabels(request.Side.ToString(), "unknown").Inc();
            ExecutorMetrics.TotalStakeDeployed.Inc(Convert.ToDouble(request.Stake));

            // Step 4: Record execution
            await _riskValidator.RecordOrderExecutedAsync(request, result);

            // Update metrics
            UpdateActiveOrdersMetric();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error handling signal");
            ExecutorMetrics.OrdersFailed.WithLabels("exception").Inc();
        }
    }

    /// <summary>
    /// Start background monitoring tasks.
    /// </summary>
    private void StartBackgroundTasks()
    {
        // Update account balance every 60 seconds
        _balanceUpdateTimer = new Timer(async _ =>
        {
            try
            {
                await UpdateAccountBalanceAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error updating account balance");
            }
        }, null, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));

        // Reconcile matched orders every 10 seconds
        _orderReconciliationTimer = new Timer(async _ =>
        {
            try
            {
                await ReconcileMatchedOrdersAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error reconciling orders");
            }
        }, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

        _logger.Information("Background tasks started");
    }

    /// <summary>
    /// Update account balance from Betfair.
    /// </summary>
    private async Task UpdateAccountBalanceAsync(CancellationToken ct)
    {
        try
        {
            var balance = await _betfairClient.GetAccountBalanceAsync(ct);

            ExecutorMetrics.AccountBalance.Set(Convert.ToDouble(balance.Balance));
            ExecutorMetrics.AvailableBalance.Set(Convert.ToDouble(balance.AvailableToBet));
            ExecutorMetrics.CurrentExposure.Set(Convert.ToDouble(balance.Exposure));

            _logger.Debug("Account balance updated: Available={Available}, Exposure={Exposure}",
                balance.AvailableToBet, balance.Exposure);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting account balance");
            ExecutorMetrics.BetfairApiErrors.WithLabels("get_balance").Inc();
        }
    }

    /// <summary>
    /// Check for matched orders and log them to accounting.
    /// </summary>
    private async Task ReconcileMatchedOrdersAsync()
    {
        var activeOrders = _orderManager.GetActiveOrders().ToList();

        foreach (var order in activeOrders)
        {
            if (order.Status == OrderStatus.Matched && order.MatchedSize > 0)
            {
                _logger.Information("✅ Order {OrderId} fully matched: {Size}@{Price}",
                    order.OrderId, order.MatchedSize, order.AveragePriceMatched);

                ExecutorMetrics.OrdersMatched.WithLabels(order.Side.ToString()).Inc();

                // Log to accounting
                await _tradeLogger.LogTradeAsync(order);
            }
        }

        UpdateActiveOrdersMetric();
    }

    /// <summary>
    /// Update active orders metric.
    /// </summary>
    private void UpdateActiveOrdersMetric()
    {
        var activeCount = _orderManager.GetActiveOrders().Count();
        ExecutorMetrics.ActiveOrders.Set(activeCount);
    }

    /// <summary>
    /// Shutdown executor gracefully.
    /// </summary>
    private async Task ShutdownAsync()
    {
        _logger.Information("Shutting down executor...");

        // Stop timers
        _balanceUpdateTimer?.Dispose();
        _orderReconciliationTimer?.Dispose();

        // Cancel any remaining active orders
        var activeOrders = _orderManager.GetActiveOrders().ToList();
        if (activeOrders.Any())
        {
            _logger.Warning("Cancelling {Count} active orders", activeOrders.Count);

            foreach (var order in activeOrders)
            {
                try
                {
                    await _orderManager.CancelOrderAsync(order.OrderId);
                    ExecutorMetrics.OrdersCancelled.WithLabels("shutdown").Inc();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error cancelling order {OrderId} during shutdown", order.OrderId);
                }
            }
        }

        _logger.Information("Executor stopped");
    }
}
