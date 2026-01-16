
using AIBettingCore.Models;
using AIBettingExecutor.Interfaces;
using Serilog;

namespace AIBettingExecutor.BetfairAPI;

public class MockBetfairClient : IBetfairClient
{
    private readonly Serilog.ILogger _log;
    public bool IsAuthenticated { get; private set; } = true;
    public string? SessionToken { get; private set; } = "mock-token";

    public MockBetfairClient(Serilog.ILogger? log)
    {
        _log = log ?? Log.ForContext<MockBetfairClient>();
    }

    public Task<bool> AuthenticateAsync(CancellationToken ct = default)
    {
        _log.Information("[MOCK] AuthenticateAsync chiamato");
        IsAuthenticated = true;
        SessionToken = "mock-token";
        return Task.FromResult(true);
    }

    public Task<OrderResult> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken ct = default)
    {
        _log.Information("[MOCK] PlaceOrderAsync: {MarketId} {SelectionId} stake={Stake} odds={Odds}", request.MarketId.Value, request.SelectionId.Value, request.Stake, request.Odds);
        var result = new OrderResult
        {
            OrderId = Guid.NewGuid().ToString("N"),
            Status = OrderStatus.Matched,
            MatchedSize = request.Stake,
            AveragePriceMatched = request.Odds,
            Message = "Ordine mock eseguito"
        };
        return Task.FromResult(result);
    }

    public Task<CancelOrderResult> CancelOrderAsync(string marketId, string orderId, CancellationToken ct = default)
    {
        _log.Information("[MOCK] CancelOrderAsync: {MarketId} {OrderId}", marketId, orderId);
        var result = new CancelOrderResult
        {
            OrderId = orderId,
            Success = true,
            Message = "Ordine mock cancellato"
        };
        return Task.FromResult(result);
    }

    public Task<OrderResult> UpdateOrderAsync(string marketId, string orderId, decimal? newStake = null, decimal? newOdds = null, CancellationToken ct = default)
    {
        _log.Information("[MOCK] UpdateOrderAsync: {MarketId} {OrderId} stake={Stake} odds={Odds}", marketId, orderId, newStake, newOdds);
        var result = new OrderResult
        {
            OrderId = orderId,
            Status = OrderStatus.Matched,
            MatchedSize = newStake,
            AveragePriceMatched = newOdds,
            Message = "Ordine mock aggiornato"
        };
        return Task.FromResult(result);
    }

    public Task<IEnumerable<CurrentOrder>> GetCurrentOrdersAsync(string marketId, CancellationToken ct = default)
    {
        _log.Information("[MOCK] GetCurrentOrdersAsync: {MarketId}", marketId);
        var items = new List<CurrentOrder>
        {
            new()
            {
                OrderId = Guid.NewGuid().ToString("N"),
                MarketId = new MarketId(marketId),
                SelectionId = new SelectionId("12345"),
                Side = TradeSide.Back,
                Status = OrderStatus.Matched,
                SizeRemaining = 0m,
                Price = 2.5m
            }
        };
        return Task.FromResult<IEnumerable<CurrentOrder>>(items);
    }

    public Task<AccountBalance> GetAccountBalanceAsync(CancellationToken ct = default)
    {
        _log.Information("[MOCK] GetAccountBalanceAsync");
        return Task.FromResult(new AccountBalance
        {
            AvailableToBet = 1000m,
            Exposure = 0m,
            Balance = 1000m,
            CurrencyCode = "EUR"
        });
    }
}
