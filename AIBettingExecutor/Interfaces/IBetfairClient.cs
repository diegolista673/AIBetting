using AIBettingCore.Models;

namespace AIBettingExecutor.Interfaces;

/// <summary>
/// Interface for interacting with Betfair Exchange API.
/// </summary>
public interface IBetfairClient
{
    /// <summary>
    /// Authenticate with Betfair API using SSL certificate.
    /// </summary>
    Task<bool> AuthenticateAsync(CancellationToken ct = default);

    /// <summary>
    /// Place an order on the exchange.
    /// </summary>
    Task<OrderResult> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken ct = default);

    /// <summary>
    /// Cancel an existing order.
    /// </summary>
    Task<CancelOrderResult> CancelOrderAsync(string marketId, string orderId, CancellationToken ct = default);

    /// <summary>
    /// Update an existing order (modify stake or odds).
    /// </summary>
    Task<OrderResult> UpdateOrderAsync(string marketId, string orderId, decimal? newStake = null, decimal? newOdds = null, CancellationToken ct = default);

    /// <summary>
    /// Get current orders for a specific market.
    /// </summary>
    Task<IEnumerable<CurrentOrder>> GetCurrentOrdersAsync(string marketId, CancellationToken ct = default);

    /// <summary>
    /// Get account balance and available funds.
    /// </summary>
    Task<AccountBalance> GetAccountBalanceAsync(CancellationToken ct = default);

    /// <summary>
    /// Check if client is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Get current session token.
    /// </summary>
    string? SessionToken { get; }
}

/// <summary>
/// Account balance information.
/// </summary>
public record AccountBalance
{
    public decimal AvailableToBet { get; init; }
    public decimal Exposure { get; init; }
    public decimal Balance { get; init; }
    public string CurrencyCode { get; init; } = "EUR";
}
