using System;
using System.Collections.Generic;

// Models for core domain
namespace AIBettingCore.Models
{
    /// <summary>
    /// Strongly typed identifier for a market.
    /// </summary>
    public record MarketId(string Value);

    /// <summary>
    /// Strongly typed identifier for a selection/runner.
    /// </summary>
    public record SelectionId(string Value);

    /// <summary>
    /// Represents a price level and its available size in the order book.
    /// </summary>
    public record PriceSize(decimal Price, decimal Size);

    /// <summary>
    /// Exchange side for trading operations.
    /// </summary>
    public enum Side { Back, Lay }

    /// <summary>
    /// Snapshot of a single runner at a point in time, including book depths.
    /// </summary>
    public class RunnerSnapshot
    {
        public required SelectionId SelectionId { get; init; }
        public required string RunnerName { get; init; }
        public decimal? LastPriceMatched { get; init; }
        public required IReadOnlyList<PriceSize> AvailableToBack { get; init; }
        public required IReadOnlyList<PriceSize> AvailableToLay { get; init; }
    }

    /// <summary>
    /// Snapshot of a market including runners, liquidity and timing data.
    /// </summary>
    public class MarketSnapshot
    {
        public required MarketId MarketId { get; init; }
        public required string EventName { get; init; }
        public required string EventType { get; init; }
        public required string MarketType { get; init; }
        public DateTimeOffset Timestamp { get; init; }
        public DateTimeOffset? StartTime { get; init; }
        public int? SecondsToStart { get; init; }
        public decimal? TotalMatched { get; init; }
        public required IReadOnlyList<RunnerSnapshot> Runners { get; init; }
    }
}
