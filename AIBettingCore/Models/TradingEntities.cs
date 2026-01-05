using System;
using System.Collections.Generic;

namespace AIBettingCore.Models
{
    /// <summary>
    /// Supported order types for exchange placement.
    /// </summary>
    public enum OrderType { Limit }

    /// <summary>
    /// Lifecycle status of an order within the system/exchange.
    /// </summary>
    public enum OrderStatus { Pending, Matched, Unmatched, Cancelled }

    /// <summary>
    /// Side of the trade: back or lay.
    /// </summary>
    public enum TradeSide { Back, Lay }

    /// <summary>
    /// Request DTO to place an order on the exchange.
    /// </summary>
    public class PlaceOrderRequest
    {
        /// <summary>
        /// The market identifier for the order.
        /// </summary>
        public required MarketId MarketId { get; init; }

        /// <summary>
        /// The selection identifier within the market for the order.
        /// </summary>
        public required SelectionId SelectionId { get; init; }

        /// <summary>
        /// The side of the trade, either back or lay.
        /// </summary>
        public TradeSide Side { get; init; }

        /// <summary>
        /// The type of order, default is Limit.
        /// </summary>
        public OrderType Type { get; init; } = OrderType.Limit;

        /// <summary>
        /// The odds at which to place the order.
        /// </summary>
        public decimal Odds { get; init; }

        /// <summary>
        /// The stake or size of the order.
        /// </summary>
        public decimal Stake { get; init; }

        /// <summary>
        /// Optional correlation identifier for tracking.
        /// </summary>
        public string? CorrelationId { get; init; }
    }

    /// <summary>
    /// Result of a place order operation returned by the executor.
    /// </summary>
    public class OrderResult
    {
        /// <summary>
        /// The unique identifier of the order.
        /// </summary>
        public required string OrderId { get; init; }

        /// <summary>
        /// The current status of the order.
        /// </summary>
        public OrderStatus Status { get; init; }

        /// <summary>
        /// The size that has been matched so far.
        /// </summary>
        public decimal? MatchedSize { get; init; }

        /// <summary>
        /// The average price at which the order has been matched.
        /// </summary>
        public decimal? AveragePriceMatched { get; init; }

        /// <summary>
        /// Optional message providing additional information about the order.
        /// </summary>
        public string? Message { get; init; }
    }

    /// <summary>
    /// Result of a cancel operation for a specific order.
    /// </summary>
    public class CancelOrderResult
    {
        /// <summary>
        /// The unique identifier of the order.
        /// </summary>
        public required string OrderId { get; init; }

        /// <summary>
        /// Indicates if the cancel operation was successful.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// Optional message providing additional information about the cancel operation.
        /// </summary>
        public string? Message { get; init; }
    }

    /// <summary>
    /// Snapshot of a currently active order on the exchange.
    /// </summary>
    public class CurrentOrder
    {
        /// <summary>
        /// The unique identifier of the order.
        /// </summary>
        public required string OrderId { get; init; }

        /// <summary>
        /// The market identifier for the order.
        /// </summary>
        public required MarketId MarketId { get; init; }

        /// <summary>
        /// The selection identifier within the market for the order.
        /// </summary>
        public required SelectionId SelectionId { get; init; }

        /// <summary>
        /// The side of the trade, either back or lay.
        /// </summary>
        public TradeSide Side { get; init; }

        /// <summary>
        /// The current status of the order.
        /// </summary>
        public OrderStatus Status { get; init; }

        /// <summary>
        /// The remaining size that is yet to be matched.
        /// </summary>
        public decimal SizeRemaining { get; init; }

        /// <summary>
        /// The price at which the order was or will be matched.
        /// </summary>
        public decimal Price { get; init; }
    }

    /// <summary>
    /// Persistent record of a trade for accounting and ROI calculations.
    /// </summary>
    public class TradeRecord
    {
        /// <summary>
        /// Unique identifier for the trade record.
        /// </summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>
        /// The timestamp when the trade was executed.
        /// </summary>
        public DateTimeOffset Timestamp { get; init; }

        /// <summary>
        /// The market identifier where the trade took place.
        /// </summary>
        public required MarketId MarketId { get; init; }

        /// <summary>
        /// The selection identifier within the market for the trade.
        /// </summary>
        public required SelectionId SelectionId { get; init; }

        /// <summary>
        /// The stake or size of the trade.
        /// </summary>
        public decimal Stake { get; init; }

        /// <summary>
        /// The odds at which the trade was executed.
        /// </summary>
        public decimal Odds { get; init; }

        /// <summary>
        /// The side of the trade, either back or lay.
        /// </summary>
        public TradeSide Side { get; init; }

        /// <summary>
        /// The status of the order associated with the trade.
        /// </summary>
        public OrderStatus Status { get; init; }

        /// <summary>
        /// The profit or loss from the trade, if settled.
        /// </summary>
        public decimal? ProfitLoss { get; init; }

        /// <summary>
        /// The commission charged for the trade.
        /// </summary>
        public decimal Commission { get; init; }

        /// <summary>
        /// The net profit from the trade after deducting commission.
        /// </summary>
        public decimal? NetProfit { get; init; }
    }
}
