using AIBettingCore.Interfaces;
using AIBettingCore.Models;
using Serilog;

namespace AIBettingExecutor.RiskManagement;

/// <summary>
/// Risk validator that wraps IRiskManager for executor-specific validation.
/// </summary>
public class RiskValidator
{
    private readonly IRiskManager _riskManager;
    private readonly ILogger _logger;
    private readonly RiskValidatorConfiguration _config;

    public RiskValidator(
        IRiskManager riskManager,
        RiskValidatorConfiguration config,
        ILogger? logger = null)
    {
        _riskManager = riskManager;
        _config = config;
        _logger = logger ?? Log.ForContext<RiskValidator>();
    }

    /// <summary>
    /// Validate order against risk rules before execution.
    /// </summary>
    public async Task<RiskValidationResult> ValidateOrderAsync(PlaceOrderRequest request, CancellationToken ct = default)
    {
        try
        {
            // Check if circuit breaker should activate
            if (await _riskManager.ShouldTriggerCircuitBreakerAsync(ct))
            {
                _logger.Warning("🚨 CIRCUIT BREAKER ACTIVATED - Rejecting all orders");
                return RiskValidationResult.Invalid("Circuit breaker activated due to excessive failures");
            }

            // Get current risk limits
            var limits = await _riskManager.GetRiskLimitsAsync(ct);

            // Validate stake against limits
            if (request.Stake > limits.MaxStakePerOrder)
            {
                _logger.Warning("Order stake {Stake} exceeds max stake per order {Max}",
                    request.Stake, limits.MaxStakePerOrder);
                return RiskValidationResult.Invalid($"Stake exceeds maximum allowed: {limits.MaxStakePerOrder}");
            }

            // Validate through risk manager
            var result = await _riskManager.ValidateOrderAsync(request, ct);

            if (!result.IsValid)
            {
                _logger.Warning("Order validation failed: {Reason}", result.RejectionReason);
            }
            else
            {
                _logger.Debug("Order validation passed. Projected exposure: {Exposure}", result.ProjectedExposure);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error validating order");
            return RiskValidationResult.Invalid($"Validation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Record successful order execution.
    /// </summary>
    public async Task RecordOrderExecutedAsync(
        PlaceOrderRequest request,
        OrderResult result,
        CancellationToken ct = default)
    {
        try
        {
            if (result.Status == OrderStatus.Matched || result.Status == OrderStatus.Pending)
            {
                await _riskManager.UpdateExposureAsync(
                    request.MarketId,
                    request.SelectionId,
                    request.Side,
                    request.Stake,
                    request.Odds,
                    ct);

                _logger.Information("Updated exposure for {Market}/{Selection}", 
                    request.MarketId.Value, request.SelectionId.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error recording order execution");
        }
    }

    /// <summary>
    /// Record failed order attempt.
    /// </summary>
    public async Task RecordOrderFailedAsync(string orderId, string reason, CancellationToken ct = default)
    {
        try
        {
            await _riskManager.RecordFailedOrderAsync(orderId, reason, ct);
            _logger.Warning("Recorded failed order: {OrderId} - {Reason}", orderId, reason);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error recording order failure");
        }
    }

    /// <summary>
    /// Get current exposure for market.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, decimal>> GetMarketExposureAsync(
        MarketId marketId,
        CancellationToken ct = default)
    {
        try
        {
            return await _riskManager.GetMarketExposureAsync(marketId, ct);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting market exposure");
            return new Dictionary<string, decimal>();
        }
    }
}

/// <summary>
/// Configuration for risk validator.
/// </summary>
public class RiskValidatorConfiguration
{
    /// <summary>
    /// Enable risk validation (if false, all orders pass validation).
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Enable circuit breaker checks.
    /// </summary>
    public bool CircuitBreakerEnabled { get; init; } = true;

    /// <summary>
    /// Log all validation results (verbose).
    /// </summary>
    public bool VerboseLogging { get; init; } = false;
}
