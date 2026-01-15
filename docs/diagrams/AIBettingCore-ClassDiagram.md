# AIBettingCore - Class Diagrams

## Architecture Overview

```mermaid
graph TB
    subgraph "AIBettingCore - Shared Library"
        Models[Models]
        Interfaces[Interfaces]
        Services[Services]
        Config[Configuration]
        RedisKeys[RedisKeys]
    end
    
    Models --> |Used by| Services
    Interfaces --> |Implemented by| Services
    Config --> |Configures| Services
    RedisKeys --> |Used by| Services
    
    subgraph "Models"
        MarketSnapshot
        Runner
        PricePoint
        MarketId
        SelectionId
    end
    
    subgraph "Services"
        RedisRiskManager
        RedisPnLTracker
    end
    
    subgraph "Interfaces"
        IRiskManager
        IPnLTracker
    end
```

## Core Models Sequence

```mermaid
sequenceDiagram
    participant Explorer
    participant MarketSnapshot
    participant Runner
    participant PricePoint
    participant Redis
    
    Explorer->>MarketSnapshot: Create(marketId, runners)
    MarketSnapshot->>Runner: Add runner data
    Runner->>PricePoint: Create price points
    PricePoint->>PricePoint: Validate odds/liquidity
    MarketSnapshot->>Redis: Serialize & Store
    Redis-->>Explorer: Confirmation
```

## RedisRiskManager Flow

```mermaid
sequenceDiagram
    participant Executor
    participant RiskManager
    participant Redis
    participant CircuitBreaker
    
    Executor->>RiskManager: ValidateOrderRisk(signal, balance)
    RiskManager->>Redis: Get current exposure
    Redis-->>RiskManager: Exposure data
    RiskManager->>RiskManager: Calculate new exposure
    
    alt Exposure within limits
        RiskManager->>Redis: Update exposure
        RiskManager-->>Executor: Approved
    else Exposure exceeds limits
        RiskManager->>CircuitBreaker: Check status
        alt Circuit breaker open
            RiskManager-->>Executor: Rejected (within limits)
        else Circuit breaker triggered
            RiskManager-->>Executor: Rejected (circuit breaker)
        end
    end
```

## RedisPnLTracker Flow

```mermaid
sequenceDiagram
    participant Executor
    participant PnLTracker
    participant Redis
    participant TradeLogger
    
    Executor->>PnLTracker: RecordTrade(orderId, stake, odds, outcome)
    PnLTracker->>Redis: Get current P&L
    Redis-->>PnLTracker: Current totals
    
    PnLTracker->>PnLTracker: Calculate profit/loss
    PnLTracker->>Redis: Update total P&L
    PnLTracker->>Redis: Store trade details
    
    PnLTracker->>TradeLogger: Log trade
    TradeLogger->>Redis: Append to trade history
    PnLTracker-->>Executor: Confirmation
```

## Class Hierarchy

```mermaid
classDiagram
    class MarketSnapshot {
        +MarketId MarketId
        +string EventName
        +DateTime MarketStartTime
        +List~Runner~ Runners
        +decimal? TotalMatched
        +int? SecondsToStart
        +DateTime Timestamp
    }
    
    class Runner {
        +SelectionId SelectionId
        +string SelectionName
        +int Status
        +List~PricePoint~ BackPrices
        +List~PricePoint~ LayPrices
        +decimal TotalMatched
    }
    
    class PricePoint {
        +decimal Price
        +decimal Size
        +int Level
    }
    
    class IRiskManager {
        <<interface>>
        +ValidateOrderRisk(signal, balance) bool
        +RecordOrderPlaced(orderId, stake, exposure)
        +RecordOrderMatched(orderId)
        +GetCurrentExposure() decimal
        +ResetCircuitBreaker()
    }
    
    class RedisRiskManager {
        -IConnectionMultiplexer _redis
        -CircuitBreakerState _circuitBreaker
        +ValidateOrderRisk(signal, balance) bool
        +RecordOrderPlaced(orderId, stake, exposure)
        +GetCurrentExposure() decimal
    }
    
    class IPnLTracker {
        <<interface>>
        +RecordTrade(orderId, stake, odds, outcome) Task
        +GetCurrentPnL() decimal
        +GetTradeHistory(count) List~Trade~
    }
    
    class RedisPnLTracker {
        -IConnectionMultiplexer _redis
        +RecordTrade(orderId, stake, odds, outcome) Task
        +GetCurrentPnL() decimal
    }
    
    MarketSnapshot "1" *-- "many" Runner
    Runner "1" *-- "many" PricePoint
    IRiskManager <|.. RedisRiskManager
    IPnLTracker <|.. RedisPnLTracker
```

## Key Patterns

### 1. **Value Objects**
- `MarketId`, `SelectionId` - Immutable identifiers with validation
- `PricePoint` - Immutable price/liquidity pair

### 2. **Repository Pattern**
- `RedisRiskManager` - Risk data storage/retrieval
- `RedisPnLTracker` - P&L persistence

### 3. **Domain Services**
- Risk validation logic
- P&L calculation logic
- Circuit breaker state management

## Redis Key Structure

```
# Risk Management
risk:exposure:{marketId}           → Current exposure per market
risk:exposure:selection:{selId}    → Exposure per selection
risk:daily-loss                    → Cumulative daily loss
risk:circuit-breaker               → Circuit breaker state

# P&L Tracking
pnl:total                          → Total cumulative P&L
pnl:daily                          → Daily P&L
pnl:trades:{date}                  → Trade history by date
```

## Dependencies

```mermaid
graph LR
    AIBettingCore --> StackExchange.Redis
    AIBettingCore --> System.Text.Json
    
    Explorer --> AIBettingCore
    Analyst --> AIBettingCore
    Executor --> AIBettingCore
    Accounting --> AIBettingCore
```
