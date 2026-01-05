using System.Threading;
using System.Threading.Tasks;
using AIBettingCore.Interfaces;
using AIBettingCore.Models;
using AIBettingAccounting.Data.Entities;

namespace AIBettingAccounting;

/// <summary>
/// Adapter to integrate AIBettingAccounting with core ITradeLogger contract.
/// </summary>
public class AccountingTradeLogger : ITradeLogger
{
    private readonly AIBettingAccountingService _service;

    public AccountingTradeLogger(AIBettingAccountingService service)
    {
        _service = service;
    }

    /// <summary>
    /// Map core TradeRecord into TradeEntity and persist using accounting service.
    /// </summary>
    public async Task LogAsync(TradeRecord trade, CancellationToken ct)
    {
        var entity = new TradeEntity
        {
            Id = trade.Id,
            Timestamp = trade.Timestamp.UtcDateTime,
            MarketId = trade.MarketId.Value,
            SelectionId = trade.SelectionId.Value,
            Stake = trade.Stake,
            Odds = trade.Odds,
            Type = trade.Side == TradeSide.Back ? "BACK" : "LAY",
            Status = trade.Status switch
            {
                OrderStatus.Pending => "PENDING",
                OrderStatus.Matched => "MATCHED",
                OrderStatus.Unmatched => "UNMATCHED",
                OrderStatus.Cancelled => "CANCELLED",
                _ => "PENDING"
            },
            ProfitLoss = trade.ProfitLoss,
            Commission = trade.Commission,
            NetProfit = trade.NetProfit,
        };

        await _service.LogTradeAsync(entity, ct);
    }
}
