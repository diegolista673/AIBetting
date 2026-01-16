using AIBettingCore.Models;
using System.Threading;
using System.Threading.Tasks;

namespace AIBettingCore.Interfaces
{
    /// <summary>
    /// Risk management service to validate orders against exposure limits, 
    /// bankroll constraints, and circuit breaker logic.
    /// </summary>
    public interface IRiskManager
    {
        /// <summary>
        /// Validates if an order can be placed given current exposure and risk limits.
        /// </summary>
        /// <param name="request">The order request to validate.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Validation result indicating if order is allowed and reason if rejected.</returns>
        Task<RiskValidationResult> ValidateOrderAsync(PlaceOrderRequest request, CancellationToken ct);

        /// <summary>
        /// Updates exposure for a market after an order is matched or cancelled.
        /// </summary>
        /// <param name="marketId">Market identifier.</param>
        /// <param name="selectionId">Selection identifier.</param>
        /// <param name="side">Trade side (Back/Lay).</param>
        /// <param name="stake">Stake amount.</param>
        /// <param name="odds">Order odds.</param>
        /// <param name="ct">Cancellation token.</param>
        Task UpdateExposureAsync(MarketId marketId, SelectionId selectionId, TradeSide side, decimal stake, decimal odds, CancellationToken ct);

        /// <summary>
        /// Gets current exposure for a specific market.
        /// </summary>
        /// <param name="marketId">Market identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Dictionary of selection ID to exposure amount.</returns>
        Task<IReadOnlyDictionary<string, decimal>> GetMarketExposureAsync(MarketId marketId, CancellationToken ct);

        /// <summary>
        /// Records a failed order attempt for circuit breaker tracking.
        /// </summary>
        /// <param name="orderId">Order identifier.</param>
        /// <param name="reason">Failure reason.</param>
        /// <param name="ct">Cancellation token.</param>
        Task RecordFailedOrderAsync(string orderId, string reason, CancellationToken ct);

        /// <summary>
        /// Checks if circuit breaker should be triggered based on recent failures.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>True if circuit breaker should activate, false otherwise.</returns>
        Task<bool> ShouldTriggerCircuitBreakerAsync(CancellationToken ct);

        /// <summary>
        /// Gets current risk limits including bankroll, max exposure, and daily loss limit.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Current risk limits configuration.</returns>
        Task<RiskLimits> GetRiskLimitsAsync(CancellationToken ct);

        /// <summary>
        /// Updates risk limits (typically done by administrator or watchdog).
        /// </summary>
        /// <param name="limits">New risk limits.</param>
        /// <param name="ct">Cancellation token.</param>
        Task UpdateRiskLimitsAsync(RiskLimits limits, CancellationToken ct);

        /// <summary>
        /// Checks if the circuit breaker is currently triggered.
        /// </summary>
        /// <returns>True if circuit breaker is triggered, false otherwise.</returns>
        Task<bool> IsCircuitBreakerTriggeredAsync();

        /// <summary>
        /// Manually resets the circuit breaker to allow trading to resume.
        /// </summary>
        Task ResetCircuitBreakerAsync();
    }

    /// <summary>
    /// Result of order validation against risk rules.
    /// </summary>
    public record RiskValidationResult
    {
        /// <summary>
        /// Indicates if the order passes all risk checks.
        /// </summary>
        public required bool IsValid { get; init; }

        /// <summary>
        /// Reason for rejection if validation failed.
        /// </summary>
        public string? RejectionReason { get; init; }

        /// <summary>
        /// Current exposure that would result after this order.
        /// </summary>
        public decimal ProjectedExposure { get; init; }

        /// <summary>
        /// Factory method for successful validation.
        /// </summary>
        public static RiskValidationResult Valid(decimal projectedExposure) => new()
        {
            IsValid = true,
            ProjectedExposure = projectedExposure
        };

        /// <summary>
        /// Factory method for failed validation.
        /// </summary>
        public static RiskValidationResult Invalid(string reason, decimal projectedExposure = 0) => new()
        {
            IsValid = false,
            RejectionReason = reason,
            ProjectedExposure = projectedExposure
        };
    }

    /// <summary>
    /// Risk limits configuration for the trading system.
    /// </summary>
    public record RiskLimits
    {
        /// <summary>
        /// Current available bankroll in account currency.
        /// </summary>
        public decimal Bankroll { get; init; }

        /// <summary>
        /// Maximum exposure allowed per single market.
        /// </summary>
        public decimal MaxExposurePerMarket { get; init; }

        /// <summary>
        /// Maximum exposure allowed per single selection.
        /// </summary>
        public decimal MaxExposurePerSelection { get; init; }

        /// <summary>
        /// Maximum stake allowed per single order.
        /// </summary>
        public decimal MaxStakePerOrder { get; init; }

        /// <summary>
        /// Maximum allowed daily loss before trading halt.
        /// </summary>
        public decimal MaxDailyLoss { get; init; }

        /// <summary>
        /// Percentage of bankroll that can be risked per trade (0.01 = 1%).
        /// </summary>
        public decimal MaxRiskPerTradePercent { get; init; }

        /// <summary>
        /// Default conservative risk limits.
        /// </summary>
        public static RiskLimits Default => new()
        {
            Bankroll = 10000m,
            MaxExposurePerMarket = 500m,
            MaxExposurePerSelection = 200m,
            MaxStakePerOrder = 100m,
            MaxDailyLoss = 500m,
            MaxRiskPerTradePercent = 0.02m // 2%
        };
    }
}
