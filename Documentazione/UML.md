@startuml
skinparam classAttributeIconSize 0
hide empty members

package AIBettingCore {
  class MarketId { +Value: string }
  class SelectionId { +Value: string }
  class PriceSize { +Price: decimal +Size: decimal }
  class RunnerSnapshot {
    +SelectionId: SelectionId
    +RunnerName: string
    +LastPriceMatched: decimal
    +AvailableToBack: PriceSize[]
    +AvailableToLay: PriceSize[]
  }
  class MarketSnapshot {
    +MarketId: MarketId
    +EventName: string
    +EventType: string
    +MarketType: string
    +Timestamp: datetime
    +StartTime: datetime
    +SecondsToStart: int
    +TotalMatched: decimal
    +Runners: RunnerSnapshot[]
  }
  class TradingSignal {
    +MarketId: MarketId
    +SelectionId: SelectionId
    +Type: string
    +Odds: decimal
    +Stake: decimal
    +Timestamp: datetime
    +Reason: string
  }
  class PlaceOrderRequest {
    +MarketId: MarketId
    +SelectionId: SelectionId
    +Side: string
    +Type: string
    +Odds: decimal
    +Stake: decimal
    +CorrelationId: string
  }
  class OrderResult {
    +OrderId: string
    +Status: string
    +MatchedSize: decimal
    +AveragePriceMatched: decimal
    +Message: string
  }
  class CancelOrderResult { +OrderId: string +Success: bool +Message: string }
  class CurrentOrder {
    +OrderId: string +MarketId: MarketId +SelectionId: SelectionId
    +Side: string +Status: string +SizeRemaining: decimal +Price: decimal
  }
  class TradeRecord {
    +Id: Guid +Timestamp: datetime +MarketId: MarketId +SelectionId: SelectionId
    +Stake: decimal +Odds: decimal +Side: string +Status: string
    +ProfitLoss: decimal +Commission: decimal +NetProfit: decimal
  }

  interface IMarketStreamClient {
    +ConnectAsync(ct): Task
    +DisconnectAsync(ct): Task
    +ReadSnapshotsAsync(ct): IAsyncEnumerable<MarketSnapshot>
  }
  interface ICacheBus {
    +PublishSignalAsync(signal, ct): Task
    +PublishPriceAsync(snapshot, ct): Task
    +SetTradingEnabledAsync(enabled, ct): Task
    +GetTradingEnabledAsync(ct): Task<bool>
  }
  interface IOrderExecutor {
    +PlaceAsync(request, ct): Task<OrderResult>
    +CancelAsync(orderId, ct): Task<CancelOrderResult>
    +ListAsync(marketId, ct): Task<List<CurrentOrder>>
  }
  interface ITradeLogger { +LogAsync(trade, ct): Task }
}

package AIBettingExplorer {
  class ExplorerService { +RunAsync(ct): Task }
}

package AIBettingAnalyst {
  class AnalystService { +RunAsync(ct): Task }
}

package AIBettingExecutor {
  class ExecutorService { +RunAsync(ct): Task }
}

package AIBettingWatchdog {
  class WatchdogService { +RunAsync(ct): Task }
}

package AIBettingAccounting {
  class TradingDbContext
  class TradeEntity {
    +Id: Guid +Timestamp: datetime +MarketId: string +SelectionId: string
    +Stake: decimal +Odds: decimal +Type: string +Status: string
    +ProfitLoss: decimal +Commission: decimal +NetProfit: decimal
    +CreatedAt: datetime
  }
  class DailySummaryEntity {
    +Id: int +Date: date +TotalTrades: int +WinningTrades: int
    +GrossProfit: decimal +TotalCommission: decimal +NetProfit: decimal +ROI: decimal
  }
  class AIBettingAccountingService {
    +LogTradeAsync(trade, ct): Task
    +GetNetProfitAsync(date, ct): Task<decimal>
  }
  interface IAccountingRepository {
    +LogTradeAsync(trade, ct): Task
    +GetNetProfitAsync(date, ct): Task<decimal>
  }
  class AccountingRepository {
    +LogTradeAsync(trade, ct): Task
    +GetNetProfitAsync(date, ct): Task<decimal>
  }
  class AccountingTradeLogger { +LogAsync(trade, ct): Task }
}

AIBettingExecutor.ExecutorService --> AIBettingCore.IOrderExecutor
AIBettingExecutor.ExecutorService --> AIBettingCore.ITradeLogger
AIBettingExecutor.ExecutorService --> AIBettingCore.ICacheBus
AIBettingExplorer.ExplorerService --> AIBettingCore.IMarketStreamClient
AIBettingExplorer.ExplorerService --> AIBettingCore.ICacheBus
AIBettingAnalyst.AnalystService --> AIBettingCore.ICacheBus
AIBettingWatchdog.WatchdogService --> AIBettingCore.ICacheBus
AIBettingAccounting.AccountingTradeLogger ..|> AIBettingCore.ITradeLogger

AIBettingCore.RunnerSnapshot --> AIBettingCore.PriceSize
AIBettingCore.MarketSnapshot --> AIBettingCore.RunnerSnapshot
AIBettingCore.TradeRecord --> AIBettingCore.MarketId
AIBettingCore.TradeRecord --> AIBettingCore.SelectionId
@enduml
