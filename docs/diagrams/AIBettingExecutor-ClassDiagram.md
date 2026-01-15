# AIBettingExecutor - Class Diagrams

## Architecture Overview

```mermaid
graph TB
    subgraph "AIBettingExecutor - Order Execution"
        Program[Program.cs]
        ExecutorService[ExecutorService]
        SignalProcessor[SignalProcessor]
        OrderManager[OrderManager]
        RiskValidator[RiskValidator]
        TradeLogger[TradeLogger]
        BetfairClient[BetfairClient/MockBetfairClient]
    end
    
    Redis[(Redis)] --> SignalProcessor
    SignalProcessor --> ExecutorService
    ExecutorService --> RiskValidator
    ExecutorService --> OrderManager
    OrderManager --> BetfairClient
    ExecutorService --> TradeLogger
    TradeLogger --> Redis
    BetfairClient --> Betfair[Betfair API]
    ExecutorService --> Prometheus
```

## Main Execution Flow

```mermaid
sequenceDiagram
    participant Redis
    participant SignalProc as SignalProcessor
    participant Executor as ExecutorService
    participant RiskVal as RiskValidator
    participant OrderMgr as OrderManager
    participant Betfair as BetfairClient
    participant Logger as TradeLogger
    
    Redis->>SignalProc: Trading signal received
    SignalProc->>Executor: ProcessSignalAsync(signal)
    
    Executor->>RiskVal: ValidateSignal(signal)
    
    alt Signal Valid
        RiskVal-->>Executor: Approved
        
        Executor->>OrderMgr: PlaceOrderAsync(order)
        OrderMgr->>Betfair: placeOrders API call
        
        alt Order Placed
            Betfair-->>OrderMgr: Order confirmation
            OrderMgr-->>Executor: Order placed
            Executor->>Logger: LogTrade(order)
            Executor->>Prometheus: Update metrics
        else Order Failed
            Betfair-->>OrderMgr: Error response
            OrderMgr-->>Executor: Order failed
            Executor->>Prometheus: Increment failure counter
        end
    else Signal Rejected
        RiskVal-->>Executor: Rejected (reason)
        Executor->>Prometheus: Increment rejection counter
    end
```

## ExecutorService Class Diagram

```mermaid
classDiagram
    class ExecutorService {
        -IBetfairClient _betfairClient
        -IOrderManager _orderManager
        -ISignalProcessor _signalProcessor
        -IRiskValidator _riskValidator
        -ITradeLogger _tradeLogger
        -ExecutorConfiguration _config
        +RunAsync(CancellationToken) Task
        -ProcessSignalAsync(signal) Task
        -ValidateSignal(signal) bool
        -CreateOrder(signal) BetfairOrder
    }
    
    class ISignalProcessor {
        <<interface>>
        +SubscribeToSignals(callback) Task
        +GetPendingSignals() Task~List~Signal~~
        +AcknowledgeSignal(signalId) Task
    }
    
    class SignalProcessor {
        -IConnectionMultiplexer _redis
        -SignalProcessorConfiguration _config
        -bool _subscribeToSurebetSignals
        -bool _subscribeToStrategySignals
        +SubscribeToSignals(callback) Task
        -ValidateSignalAge(signal) bool
    }
    
    class IRiskValidator {
        <<interface>>
        +ValidateSignal(signal, balance) (bool, string)
        +IsCircuitBreakerTriggered() bool
        +RecordOrderResult(success) void
    }
    
    class RiskValidator {
        -IRiskManager _riskManager
        -RiskValidatorConfiguration _config
        -CircuitBreakerState _circuitBreaker
        +ValidateSignal(signal, balance) (bool, string)
        -CheckStakeLimits(stake) bool
        -CheckExposureLimits(exposure) bool
        -CheckDailyLoss() bool
    }
    
    ExecutorService --> ISignalProcessor
    ExecutorService --> IRiskValidator
    ExecutorService --> IOrderManager
    ExecutorService --> ITradeLogger
    ISignalProcessor <|.. SignalProcessor
    IRiskValidator <|.. RiskValidator
```

## Order Manager Flow

```mermaid
sequenceDiagram
    participant Executor
    participant OrderMgr as OrderManager
    participant Betfair as BetfairClient
    participant Redis
    
    Executor->>OrderMgr: PlaceOrderAsync(order)
    
    OrderMgr->>OrderMgr: Validate order
    OrderMgr->>Betfair: placeOrders([order])
    
    alt API Success
        Betfair-->>OrderMgr: PlaceExecutionReport
        OrderMgr->>Redis: Store order status
        OrderMgr->>OrderMgr: Start tracking order
        OrderMgr-->>Executor: Order placed (betId)
        
        loop Order Tracking
            OrderMgr->>Betfair: listCurrentOrders
            
            alt Order Matched
                Betfair-->>OrderMgr: MATCHED status
                OrderMgr->>Redis: Update order
                OrderMgr->>Executor: OnOrderMatched event
            else Order Unmatched (timeout)
                OrderMgr->>Betfair: cancelOrders
                OrderMgr->>Executor: OnOrderCancelled event
            end
        end
    else API Error
        Betfair-->>OrderMgr: Error response
        OrderMgr-->>Executor: Order failed
    end
```

## Risk Validation Flow

```mermaid
sequenceDiagram
    participant Executor
    participant RiskVal as RiskValidator
    participant RiskMgr as RiskManager
    participant Redis
    
    Executor->>RiskVal: ValidateSignal(signal, balance)
    
    alt Risk validation enabled
        RiskVal->>RiskVal: Check circuit breaker
        
        alt Circuit breaker triggered
            RiskVal-->>Executor: Rejected (circuit breaker)
        else Circuit breaker open
            RiskVal->>RiskVal: CheckStakeLimits(signal.stake)
            
            alt Stake within limits
                RiskVal->>RiskMgr: GetCurrentExposure()
                Redis-->>RiskMgr: Exposure data
                RiskMgr-->>RiskVal: Current exposure
                
                RiskVal->>RiskVal: Calculate new exposure
                
                alt Exposure within limits
                    RiskVal->>RiskMgr: CheckDailyLoss()
                    
                    alt Daily loss OK
                        RiskVal-->>Executor: Approved
                    else Daily loss limit reached
                        RiskVal-->>Executor: Rejected (daily loss)
                    end
                else Exposure exceeds limit
                    RiskVal-->>Executor: Rejected (exposure)
                end
            else Stake too high
                RiskVal-->>Executor: Rejected (stake limit)
            end
        end
    else Risk validation disabled
        RiskVal-->>Executor: Approved
    end
```

## Circuit Breaker Pattern

```mermaid
stateDiagram-v2
    [*] --> Open
    Open --> Triggered: Failure threshold reached
    Triggered --> Open: Reset (manual/timeout)
    
    state Open {
        [*] --> Monitoring
        Monitoring --> Monitoring: Order success
        Monitoring --> Evaluating: Order failure
        Evaluating --> Monitoring: Below threshold
        Evaluating --> [*]: Above threshold
    }
    
    state Triggered {
        [*] --> Halted
        Halted --> Halted: All orders blocked
        Halted --> [*]: Reset requested
    }
```

## Trade Logger

```mermaid
classDiagram
    class ITradeLogger {
        <<interface>>
        +LogTrade(trade) Task
        +GetRecentTrades(count) Task~List~Trade~~
        +GetTradeStatistics() Task~TradeStats~
    }
    
    class TradeLogger {
        -IConnectionMultiplexer _redis
        -decimal _commissionRate
        +LogTrade(trade) Task
        -CalculateNetProfit(stake, odds, outcome) decimal
        -StoreInRedis(trade) Task
        -UpdateDailyStats(trade) Task
    }
    
    class Trade {
        +string OrderId
        +string MarketId
        +string SelectionId
        +OrderSide Side
        +decimal Stake
        +decimal Odds
        +OrderStatus Status
        +decimal NetProfit
        +DateTime Timestamp
    }
    
    ITradeLogger <|.. TradeLogger
    TradeLogger --> Trade
```

## Betfair Client Abstraction

```mermaid
classDiagram
    class IBetfairClient {
        <<interface>>
        +PlaceOrdersAsync(orders) Task~PlaceExecutionReport~
        +CancelOrdersAsync(betIds) Task~CancelExecutionReport~
        +ListCurrentOrdersAsync(marketId) Task~List~CurrentOrder~~
        +GetAccountFundsAsync() Task~decimal~
        +IsConnected() bool
    }
    
    class BetfairClient {
        -HttpClient _httpClient
        -string _appKey
        -X509Certificate2 _certificate
        -string _sessionToken
        +PlaceOrdersAsync(orders) Task~PlaceExecutionReport~
        -AuthenticateAsync() Task
        -CallBetfairAPIAsync(endpoint, request) Task~T~
    }
    
    class MockBetfairClient {
        -Dictionary~string, Order~ _orders
        -decimal _mockBalance
        +PlaceOrdersAsync(orders) Task~PlaceExecutionReport~
        +SimulateOrderMatch(betId) void
        +SimulateOrderFailure(betId) void
    }
    
    IBetfairClient <|.. BetfairClient
    IBetfairClient <|.. MockBetfairClient
```

## Metrics Export

```mermaid
graph LR
    subgraph "Prometheus Metrics"
        Counter1[orders_placed_total]
        Counter2[orders_matched_total]
        Counter3[orders_cancelled_total]
        Counter4[orders_failed_total]
        Counter5[signals_received_total]
        Counter6[signals_rejected_total]
        Gauge1[circuit_breaker_status]
        Gauge2[account_balance]
        Gauge3[current_exposure]
        Histogram1[order_execution_latency_seconds]
        Histogram2[betfair_api_latency_seconds]
    end
    
    ExecutorService --> Counter1
    ExecutorService --> Counter2
    ExecutorService --> Counter3
    ExecutorService --> Counter4
    ExecutorService --> Counter5
    ExecutorService --> Counter6
    ExecutorService --> Gauge1
    ExecutorService --> Gauge2
    ExecutorService --> Gauge3
    ExecutorService --> Histogram1
    OrderManager --> Histogram2
```

## Configuration

```mermaid
classDiagram
    class ExecutorConfiguration {
        +BetfairConfig Betfair
        +RedisConfig Redis
        +OrderManagerConfig OrderManager
        +SignalProcessorConfig SignalProcessor
        +RiskConfig Risk
        +TradingConfig Trading
        +int PrometheusMetricsPort
    }
    
    class RiskConfig {
        +bool Enabled
        +bool CircuitBreakerEnabled
        +decimal MaxStakePerOrder
        +decimal MaxExposurePerMarket
        +decimal MaxExposurePerSelection
        +decimal MaxDailyLoss
        +int CircuitBreakerFailureThreshold
        +int CircuitBreakerWindowMinutes
    }
    
    class TradingConfig {
        +decimal CommissionRate
        +decimal MinOdds
        +decimal MaxOdds
        +decimal MinStake
        +bool EnablePaperTrading
        +bool UseMockBetfair
    }
    
    ExecutorConfiguration --> RiskConfig
    ExecutorConfiguration --> TradingConfig
```

## Key Features

### 1. **Risk Management**
- Multi-level validation (stake, exposure, daily loss)
- Circuit breaker pattern
- Configurable limits
- Real-time exposure tracking

### 2. **Order Management**
- Automated order placement
- Order status tracking
- Timeout handling
- Cancellation support

### 3. **Dual Mode Operation**
- Production mode (real Betfair API)
- Mock mode (testing without real trades)
- Paper trading support

### 4. **Observability**
- Comprehensive metrics
- Trade logging
- Error tracking
- Performance monitoring

## Dependencies

```mermaid
graph TD
    Executor[AIBettingExecutor]
    Executor --> Core[AIBettingCore]
    Executor --> Redis[StackExchange.Redis]
    Executor --> Serilog
    Executor --> Prometheus[prometheus-net]
    Executor --> Http[System.Net.Http]
    Executor --> Crypto[System.Security.Cryptography]
```
