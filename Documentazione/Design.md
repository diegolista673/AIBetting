classDiagram
class MarketId {+Value: string}
class SelectionId {+Value: string}
class PriceSize {+Price: decimal\n+Size: decimal}
class RunnerSnapshot {
  +SelectionId: SelectionId
  +RunnerName: string
  +LastPriceMatched: decimal?
  +AvailableToBack: IReadOnlyList<PriceSize>
  +AvailableToLay: IReadOnlyList<PriceSize>
}
class MarketSnapshot {
  +MarketId: MarketId
  +EventName: string
  +EventType: string
  +MarketType: string
  +Timestamp: DateTimeOffset
  +StartTime: DateTimeOffset?
  +SecondsToStart: int?
  +TotalMatched: decimal?
  +Runners: IReadOnlyList<RunnerSnapshot>
}
class TradingSignal {
  +MarketId: MarketId
  +SelectionId: SelectionId
  +Type: SignalType
  +Odds: decimal
  +Stake: decimal
  +Timestamp: DateTimeOffset
  +Reason: string?
}
class PlaceOrderRequest {
  +MarketId: MarketId
  +SelectionId: SelectionId
  +Side: TradeSide
  +Type: OrderType
  +Odds: decimal
  +Stake: decimal
  +CorrelationId: string?
}
class OrderResult{
    +OrderId: string
    +Status: OrderStatus
    +MatchedSize: decimal?
    +AveragePriceMatched: decimal?
    +Message: string?
}
class CancelOrderResult {
    +OrderId: string
    +Success: bool
    +Message: string?
}
class CurrentOrder {
    +OrderId: string
    +MarketId: MarketId
    +SelectionId: SelectionId
    +Side: TradeSide
    +Status: OrderStatus
    +SizeRemaining: decimal
    +Price: decimal
}
class TradeRecord {
    +Id: Guid
    +Timestamp: DateTimeOffset
    +MarketId: MarketId
    +SelectionId: SelectionId
    +Stake: decimal
    +Odds: decimal
    +Side: TradeSide
    +Status: OrderStatus
    +ProfitLoss: decimal?
    +Commission: decimal
    +NetProfit: decimal?
}


class IMarketStreamClient {
  +ConnectAsync(ct): Task
  +DisconnectAsync(ct): Task
  +ReadSnapshotsAsync(ct): IAsyncEnumerable<MarketSnapshot>
}
class ICacheBus {
  +PublishSignalAsync(signal, ct): Task
  +PublishPriceAsync(snapshot, ct): Task
  +SetTradingEnabledAsync(enabled, ct): Task
  +GetTradingEnabledAsync(ct): Task<bool>
}
class IOrderExecutor {
  +PlaceAsync(request, ct): Task<OrderResult>
  +CancelAsync(orderId, ct): Task<CancelOrderResult>
  +ListAsync(marketId, ct): Task<IReadOnlyList<CurrentOrder>>
}
class ITradeLogger {+LogAsync(trade, ct): Task}

class ExplorerService {
  -stream: IMarketStreamClient
  -bus: ICacheBus
  +RunAsync(ct): Task
}
class AnalystService {
  -bus: ICacheBus
  +RunAsync(ct): Task
}
class ExecutorService {
  -orders: IOrderExecutor
  -bus: ICacheBus
  -logger: ITradeLogger
  +RunAsync(ct): Task
}
class WatchdogService {
  -bus: ICacheBus
  +RunAsync(ct): Task
}


class TradeEntity {
  +Id: Guid
  +Timestamp: DateTime
  +MarketId: string
  +SelectionId: string
  +Stake: decimal
  +Odds: decimal
  +Type: string
  +Status: string
  +ProfitLoss: decimal?
  +Commission: decimal
  +NetProfit: decimal?
  +CreatedAt: DateTime
}
class DailySummaryEntity {
  +Id: int\n+Date: DateOnly
  +TotalTrades: int
  +WinningTrades: int
  +GrossProfit: decimal
  +TotalCommission: decimal
  +NetProfit: decimal
  +ROI: decimal
}
class AIBettingAccountingService {
    +LogTradeAsync(trade, ct): Task
    +GetNetProfitAsync(date, ct): Task<decimal>
}
class IAccountingRepository {
    +LogTradeAsync(trade, ct): Task
    +GetNetProfitAsync(date, ct): Task<decimal>
}
class AccountingRepository {
    +LogTradeAsync(trade, ct): Task
    +GetNetProfitAsync(date, ct): Task<decimal>}
class AccountingTradeLogger {
    +LogAsync(trade, ct): Task
}

RunnerSnapshot --> PriceSize
MarketSnapshot --> RunnerSnapshot
TradeRecord --> MarketId
TradeRecord --> SelectionId
ExplorerService --> IMarketStreamClient
ExplorerService --> ICacheBus
AnalystService --> ICacheBus
ExecutorService --> IOrderExecutor
ExecutorService --> ITradeLogger
ExecutorService --> ICacheBus
WatchdogService --> ICacheBus
AccountingTradeLogger ..> ITradeLogger

```

## Roadmap di implementazione

Fase 1 – Fondazioni
- Completare `AIBettingCore` (modelli, interfacce, chiavi, costanti).
- Completare `AIBettingAccounting`: DbContext, entità, repository, adapter `ITradeLogger`.
- Preparare migrazioni EF Core e connection string.

Fase 2 – Explorer (ingest)
- Implementare `IMarketStreamClient` (Betfair Stream WebSocket).
- `ExplorerService`: mappare stream → `MarketSnapshot`, pubblicare su Redis (`ICacheBus`).
- Test integrazione con mock Betfair e carico (NBomber/K6).

Fase 3 – Analyst (segnali)
- Implementare lettura Redis (`ICacheBus`), feature WAP/WoM/Spread.
- `AnalystService`: regole base Surebet/Scalping → `TradingSignal` su canale dedicato.
- Telemetria e soglie di debounce.

Fase 4 – Executor (ordini)
- Implementare `IOrderExecutor`: login SSO cert `.pfx`, place/cancel/list via JSON-RPC.
- Gestione idempotenza, retry/backoff, stati matched/unmatched.
- Uso `ITradeLogger` per persistenza in Accounting.

Fase 5 – Watchdog & resilienza
- `WatchdogService`: monitor latenze, kill-switch per mercato/servizio.
- Circuit breaker su REST/Redis; strategie di riconnessione stream.

Fase 6 – Dashboard
- Blazor dashboard: profitti, ordini, latenze, flag trading-enabled.
- OpenTelemetry: tracing e metriche per hop.

Fase 7 – ML & backtesting
- Pipeline ML.NET: momentum/trend, dataset storico, separazione training/serving.
- Backtesting e drift monitoring, versioning dei modelli.

Fase 8 – CI/CD & deploy
- Pipeline CI: unit, integrazione, load tests; copertura.
- Docker Compose, segreti, IP whitelisting.
- Tuning performance: timeouts, hedging, batching minimo.

## Dipendenze principali
- .NET 10 / C# 14
- Redis (bus cache/pub-sub)
- PostgreSQL (Accounting EF Core)
- Betfair APIs (Stream, JSON-RPC)
- ML.NET per analisi

## Contratti e chiavi Redis
- Keys: `prices:{marketId}:{selectionId}`, `signals:{marketId}:{selectionId}`, `orders:{orderId}`
- Flags: `flag:trading-enabled`
- Channels: `channel:price-updates`, `channel:trading-signals`, `channel:order-updates`, `channel:kill-switch`
