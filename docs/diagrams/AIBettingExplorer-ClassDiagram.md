# AIBettingExplorer - Class Diagrams

## Architecture Overview

```mermaid
graph TB
    subgraph "AIBettingExplorer - Data Ingestion"
        Program[Program.cs]
        ExplorerService[ExplorerService]
        StreamClient[BetfairMarketStreamClient]
        CacheBus[RedisCacheBus]
        InMemoryCache[InMemoryCacheBus]
    end
    
    Program --> ExplorerService
    ExplorerService --> StreamClient
    ExplorerService --> CacheBus
    ExplorerService --> InMemoryCache
    CacheBus --> Redis[(Redis)]
    StreamClient --> Betfair[Betfair Stream API]
    
    subgraph "Output"
        Redis --> Analyst[AIBettingAnalyst]
        Redis --> Monitoring[Prometheus Metrics]
    end
```

## Main Data Flow

```mermaid
sequenceDiagram
    participant Betfair as Betfair Stream API
    participant StreamClient
    participant ExplorerService
    participant CacheBus
    participant Redis
    participant Analyst
    participant Prometheus
    
    Betfair->>StreamClient: Market data stream
    StreamClient->>ExplorerService: OnMarketUpdate(data)
    
    ExplorerService->>ExplorerService: Parse & Create MarketSnapshot
    ExplorerService->>ExplorerService: Track metrics
    
    par Publish to Redis
        ExplorerService->>CacheBus: PublishPriceAsync(snapshot)
        CacheBus->>Redis: PUBLISH channel:price-updates
        CacheBus->>Redis: SET prices:{marketId}:{timestamp}
    and Export Metrics
        ExplorerService->>Prometheus: Update counters/histograms
    end
    
    Redis-->>Analyst: Subscribe to price updates
    Prometheus-->>Grafana: Scrape metrics
```

## ExplorerService Class Diagram

```mermaid
classDiagram
    class ExplorerService {
        -IMarketStreamClient _stream
        -ICacheBus _bus
        -Counter PriceUpdates
        -Histogram ProcessingLatency
        -Gauge ActiveMarkets
        +RunAsync(CancellationToken) Task
        -ProcessMarketUpdate(data) Task
        -CreateSnapshot(data) MarketSnapshot
    }
    
    class IMarketStreamClient {
        <<interface>>
        +ConnectAsync() Task
        +SubscribeToMarkets(marketIds) Task
        +OnMarketChange event
        +DisconnectAsync() Task
    }
    
    class BetfairMarketStreamClient {
        -WebSocket _ws
        -string _sessionToken
        -string _appKey
        +ConnectAsync() Task
        +SubscribeToMarkets(marketIds) Task
        -HandleMessage(message) Task
        -ParseMarketChange(json) MarketChange
    }
    
    class ICacheBus {
        <<interface>>
        +PublishPriceAsync(snapshot) Task
        +GetLatestPrice(marketId) Task~MarketSnapshot~
    }
    
    class RedisCacheBus {
        -IConnectionMultiplexer _redis
        -JsonSerializerOptions _jsonOptions
        +PublishPriceAsync(snapshot) Task
        +GetLatestPrice(marketId) Task~MarketSnapshot~
    }
    
    class InMemoryCacheBus {
        -ConcurrentDictionary~string, MarketSnapshot~ _cache
        +PublishPriceAsync(snapshot) Task
        +GetLatestPrice(marketId) Task~MarketSnapshot~
    }
    
    ExplorerService --> IMarketStreamClient
    ExplorerService --> ICacheBus
    IMarketStreamClient <|.. BetfairMarketStreamClient
    ICacheBus <|.. RedisCacheBus
    ICacheBus <|.. InMemoryCacheBus
```

## Betfair Stream Client Flow

```mermaid
sequenceDiagram
    participant App as Application
    participant Client as BetfairMarketStreamClient
    participant WebSocket
    participant Betfair as Betfair API
    
    App->>Client: ConnectAsync()
    Client->>Betfair: HTTP POST /api/login
    Betfair-->>Client: SessionToken
    
    Client->>WebSocket: Connect(wss://stream-api.betfair.com)
    WebSocket-->>Client: Connected
    
    Client->>Betfair: Authentication message
    Betfair-->>Client: Authentication confirmed
    
    App->>Client: SubscribeToMarkets([marketIds])
    Client->>Betfair: Market subscription message
    
    loop Market Data Stream
        Betfair->>Client: Market change message
        Client->>Client: ParseMarketChange(json)
        Client->>App: Trigger OnMarketChange event
    end
    
    App->>Client: DisconnectAsync()
    Client->>WebSocket: Close connection
```

## RedisCacheBus Implementation

```mermaid
sequenceDiagram
    participant Explorer as ExplorerService
    participant CacheBus as RedisCacheBus
    participant Redis
    participant Subscriber as Redis Subscriber
    
    Explorer->>CacheBus: PublishPriceAsync(snapshot)
    
    par Store snapshot
        CacheBus->>Redis: SET prices:{marketId}:{timestamp}
        Note over Redis: TTL: 1 hour
    and Publish notification
        CacheBus->>CacheBus: Create notification JSON
        CacheBus->>Redis: PUBLISH channel:price-updates
    end
    
    Redis-->>Subscriber: Notification received
    CacheBus-->>Explorer: Confirmation
```

## Metrics Tracking

```mermaid
graph LR
    subgraph "Prometheus Metrics"
        Counter1[aibetting_price_updates_total]
        Histogram1[aibetting_processing_latency_seconds]
        Gauge1[aibetting_active_markets]
        Counter2[aibetting_startup_test]
    end
    
    ExplorerService --> Counter1
    ExplorerService --> Histogram1
    ExplorerService --> Gauge1
    Program --> Counter2
    
    Counter1 --> Prometheus
    Histogram1 --> Prometheus
    Gauge1 --> Prometheus
    Counter2 --> Prometheus
    
    Prometheus --> Grafana
```

## Configuration

```mermaid
classDiagram
    class Program {
        +Main(args) void
        -LoadConfiguration() IConfiguration
        -InitializeLogging() void
        -StartMetricsServer() void
    }
    
    class Configuration {
        +Redis:ConnectionString string
        +Betfair:AppKey string
        +Betfair:SessionToken string
        +Betfair:StreamApiUrl string
    }
    
    Program --> Configuration
    Program --> ExplorerService
    Program --> KestrelMetricServer
```

## Error Handling

```mermaid
sequenceDiagram
    participant Client as StreamClient
    participant Explorer as ExplorerService
    participant Redis
    participant Logger as Serilog
    
    Client->>Explorer: Market data received
    
    alt Processing Success
        Explorer->>Redis: Publish price
        Explorer->>Logger: Log.Information
    else Redis Connection Error
        Explorer->>Logger: Log.Error(exception)
        Explorer->>Explorer: Retry logic
    else Parse Error
        Explorer->>Logger: Log.Warning
        Explorer->>Explorer: Skip invalid data
    end
```

## Key Features

### 1. **Real-time Stream Processing**
- WebSocket connection to Betfair
- Event-driven market data handling
- Low-latency price updates

### 2. **Dual Cache Strategy**
- Redis for distributed caching
- In-memory fallback option
- TTL-based expiration

### 3. **Observability**
- Structured logging (Serilog)
- Prometheus metrics export
- Health check endpoint

### 4. **Resilience**
- Automatic reconnection
- Error recovery
- Graceful shutdown

## Data Model

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
    
    class PriceUpdateNotification {
        +MarketId MarketId
        +DateTimeOffset Timestamp
        +decimal TotalMatched
        +int RunnersCount
    }
    
    MarketSnapshot "1" *-- "many" Runner
    MarketSnapshot --> PriceUpdateNotification: Creates
```

## Deployment

```
Explorer (Port 5001)
├── /metrics → Prometheus metrics endpoint
├── Redis Connection → localhost:16379
└── Betfair Stream → wss://stream-api.betfair.com
```

## Dependencies

```mermaid
graph TD
    Explorer[AIBettingExplorer]
    Explorer --> Core[AIBettingCore]
    Explorer --> Redis[StackExchange.Redis]
    Explorer --> Serilog
    Explorer --> Prometheus[prometheus-net]
    Explorer --> Json[System.Text.Json]
```
