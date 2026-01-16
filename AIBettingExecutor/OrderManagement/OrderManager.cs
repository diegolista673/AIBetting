using AIBettingCore.Models;
using AIBettingExecutor.Interfaces;
using Serilog;
using System.Collections.Concurrent;

namespace AIBettingExecutor.OrderManagement;

/// <summary>
/// Manages order lifecycle, state tracking, and timeout monitoring.
/// </summary>
public class OrderManager
{
    private readonly IBetfairClient _betfairClient;
    private readonly Serilog.ILogger _logger;
    private readonly ConcurrentDictionary<string, ManagedOrder> _activeOrders;
    private readonly OrderManagerConfiguration _config;
    private readonly Timer _monitoringTimer;

    public OrderManager(
        IBetfairClient betfairClient,
        OrderManagerConfiguration config,
        Serilog.ILogger? logger = null)
    {
        _betfairClient = betfairClient;
        _config = config;
        _logger = logger ?? Log.ForContext<OrderManager>();
        _activeOrders = new ConcurrentDictionary<string, ManagedOrder>();

        // Start monitoring timer
        _monitoringTimer = new Timer(MonitorOrders, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Place an order and start tracking it.
    /// </summary>
    public async Task<OrderResult> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.Information("Placing order: {Side} {Stake}@{Odds} on {Selection}",
                request.Side, request.Stake, request.Odds, request.SelectionId.Value);

            var result = await _betfairClient.PlaceOrderAsync(request, ct);

            if (result.Status != OrderStatus.Cancelled)
            {
                var managedOrder = new ManagedOrder
                {
                    OrderId = result.OrderId,
                    MarketId = request.MarketId.Value,
                    SelectionId = request.SelectionId.Value,
                    Side = request.Side,
                    RequestedOdds = request.Odds,
                    RequestedStake = request.Stake,
                    Status = result.Status,
                    PlacedAt = DateTimeOffset.UtcNow,
                    LastCheckedAt = DateTimeOffset.UtcNow,
                    MatchedSize = result.MatchedSize ?? 0,
                    AveragePriceMatched = result.AveragePriceMatched,
                    CorrelationId = request.CorrelationId
                };

                _activeOrders[result.OrderId] = managedOrder;
                _logger.Information("Order {OrderId} tracked. Active orders: {Count}", result.OrderId, _activeOrders.Count);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error placing order");
            throw;
        }
    }

    /// <summary>
    /// Cancel an active order.
    /// </summary>
    public async Task<CancelOrderResult> CancelOrderAsync(string orderId, CancellationToken ct = default)
    {
        if (!_activeOrders.TryGetValue(orderId, out var order))
        {
            _logger.Warning("Order {OrderId} not found in active orders", orderId);
            return new CancelOrderResult
            {
                OrderId = orderId,
                Success = false,
                Message = "Order not found"
            };
        }

        try
        {
            var result = await _betfairClient.CancelOrderAsync(order.MarketId, orderId, ct);

            if (result.Success)
            {
                order.Status = OrderStatus.Cancelled;
                order.LastCheckedAt = DateTimeOffset.UtcNow;
                _activeOrders.TryRemove(orderId, out _);
                _logger.Information("Order {OrderId} cancelled and removed from tracking", orderId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error cancelling order {OrderId}", orderId);
            throw;
        }
    }

    /// <summary>
    /// Get all active orders.
    /// </summary>
    public IEnumerable<ManagedOrder> GetActiveOrders()
    {
        return _activeOrders.Values.ToList();
    }

    /// <summary>
    /// Get active orders for a specific market.
    /// </summary>
    public IEnumerable<ManagedOrder> GetActiveOrdersForMarket(string marketId)
    {
        return _activeOrders.Values.Where(o => o.MarketId == marketId).ToList();
    }

    /// <summary>
    /// Monitor active orders for timeouts and status updates.
    /// </summary>
    private async void MonitorOrders(object? state)
    {
        if (_activeOrders.IsEmpty)
            return;

        try
        {
            var now = DateTimeOffset.UtcNow;
            var ordersToCheck = _activeOrders.Values
                .Where(o => (now - o.LastCheckedAt).TotalSeconds >= _config.StatusCheckIntervalSeconds)
                .Take(10) // Check max 10 orders per iteration to avoid API rate limits
                .ToList();

            foreach (var order in ordersToCheck)
            {
                await CheckOrderStatusAsync(order);
            }

            // Cancel timed-out unmatched orders
            var timedOutOrders = _activeOrders.Values
                .Where(o => o.Status == OrderStatus.Pending &&
                           (now - o.PlacedAt).TotalSeconds > _config.UnmatchedOrderTimeoutSeconds)
                .ToList();

            foreach (var order in timedOutOrders)
            {
                _logger.Warning("Order {OrderId} timed out after {Seconds}s, cancelling",
                    order.OrderId, (now - order.PlacedAt).TotalSeconds);
                await CancelOrderAsync(order.OrderId);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error monitoring orders");
        }
    }

    /// <summary>
    /// Check status of a specific order.
    /// </summary>
    private async Task CheckOrderStatusAsync(ManagedOrder order)
    {
        try
        {
            var currentOrders = await _betfairClient.GetCurrentOrdersAsync(order.MarketId);
            var currentOrder = currentOrders.FirstOrDefault(o => o.OrderId == order.OrderId);

            order.LastCheckedAt = DateTimeOffset.UtcNow;

            if (currentOrder == null)
            {
                // Order not found - assume matched or cancelled
                _logger.Information("Order {OrderId} no longer in current orders, assuming completed", order.OrderId);
                order.Status = OrderStatus.Matched;
                _activeOrders.TryRemove(order.OrderId, out _);
                return;
            }

            // Update order status
            var previousStatus = order.Status;
            order.Status = currentOrder.Status;
            order.MatchedSize = order.RequestedStake - currentOrder.SizeRemaining;

            if (previousStatus != order.Status)
            {
                _logger.Information("Order {OrderId} status changed: {OldStatus} -> {NewStatus}",
                    order.OrderId, previousStatus, order.Status);
            }

            // Remove fully matched orders
            if (order.Status == OrderStatus.Matched && currentOrder.SizeRemaining == 0)
            {
                _logger.Information("Order {OrderId} fully matched, removing from tracking", order.OrderId);
                _activeOrders.TryRemove(order.OrderId, out _);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error checking order {OrderId} status", order.OrderId);
        }
    }

    public void Dispose()
    {
        _monitoringTimer?.Dispose();
    }
}

/// <summary>
/// Managed order with tracking metadata.
/// </summary>
public class ManagedOrder
{
    public required string OrderId { get; init; }
    public required string MarketId { get; init; }
    public required string SelectionId { get; init; }
    public TradeSide Side { get; init; }
    public decimal RequestedOdds { get; init; }
    public decimal RequestedStake { get; init; }
    public OrderStatus Status { get; set; }
    public DateTimeOffset PlacedAt { get; init; }
    public DateTimeOffset LastCheckedAt { get; set; }
    public decimal MatchedSize { get; set; }
    public decimal? AveragePriceMatched { get; set; }
    public string? CorrelationId { get; init; }
}

/// <summary>
/// Configuration for order manager.
/// </summary>
public class OrderManagerConfiguration
{
    /// <summary>
    /// Timeout for unmatched orders in seconds.
    /// </summary>
    public int UnmatchedOrderTimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Interval for checking order status in seconds.
    /// </summary>
    public int StatusCheckIntervalSeconds { get; init; } = 5;

    /// <summary>
    /// Maximum number of orders to check per monitoring cycle.
    /// </summary>
    public int MaxOrdersPerCheck { get; init; } = 10;
}
