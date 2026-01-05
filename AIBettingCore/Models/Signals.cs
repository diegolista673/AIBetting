using System;
using System.Collections.Generic;

namespace AIBettingCore.Models
{
    /// <summary>
    /// Types of trading signals emitted by analysis service.
    /// </summary>
    public enum SignalType { Back, Lay, ClosePosition, KillSwitch }

    /// <summary>
    /// Trading instruction produced by the analyst based on market conditions.
    /// </summary>
    public class TradingSignal
    {
        public required MarketId MarketId { get; init; }
        public required SelectionId SelectionId { get; init; }
        public SignalType Type { get; init; }
        public decimal Odds { get; init; }
        public decimal Stake { get; init; }
        public DateTimeOffset Timestamp { get; init; }
        public string? Reason { get; init; }
    }
}
