# AIBettingAnalyst - Class Diagrams

## Architecture Overview

```mermaid
graph TB
    subgraph "AIBettingAnalyst - Signal Generation"
        Program[Program.cs]
        AnalystService[AnalystService]
        Orchestrator[StrategyOrchestrator]
        Strategies[Strategy Implementations]
        Analyzers[Market Analyzers]
    end
    
    Redis[(Redis)] --> AnalystService
    AnalystService --> Orchestrator
    Orchestrator --> Strategies
    Strategies --> Analyzers
    AnalystService --> Redis
    AnalystService --> Prometheus
    
    subgraph "Strategies"
        Scalping[ScalpingStrategy]
        SteamMove[SteamMoveStrategy]
        ValueBet[ValueBetStrategy]
        GreenUp[GreenUpStrategy]
    end
    
    subgraph "Analyzers"
        Surebet[SurebetDetector]
        WAP[WAPCalculator]
        WoM[WeightOfMoneyAnalyzer]
    end
```

## Main Analysis Flow

```mermaid
sequenceDiagram
    participant Redis
    participant Analyst as AnalystService
    participant Orchestrator
    participant Strategy
    participant Analyzer
    participant Executor
    
    Redis->>Analyst: Price update notification
    Analyst->>Analyst: Fetch MarketSnapshot
    Analyst->>Analyst: Update market history
    
    par Surebet Detection
        Analyst->>Analyzer: DetectOpportunities(snapshot)
        Analyzer-->>Analyst: Surebet signals
        Analyst->>Redis: Publish surebet signals
    and Strategy Analysis
        Analyst->>Orchestrator: AnalyzeMarketAsync(snapshot, context)
        Orchestrator->>Strategy: Analyze(snapshot, history)
        Strategy->>Analyzer: Calculate metrics
        Analyzer-->>Strategy: Analysis results
        Strategy-->>Orchestrator: Strategy signals
        Orchestrator->>Orchestrator: Filter & rank signals
        Orchestrator-->>Analyst: Top signals
        Analyst->>Redis: Publish strategy signals
    end
    
    Redis-->>Executor: Forward signals
```

## Strategy Orchestrator Pattern

```mermaid
classDiagram
    class StrategyOrchestrator {
        -List~IAnalysisStrategy~ _strategies
        -OrchestratorConfiguration _config
        +AnalyzeMarketAsync(snapshot, context) Task~List~StrategySignal~~
        -FilterByConfidence(signals) List~StrategySignal~
        -FilterByRisk(signals) List~StrategySignal~
        -ResolveConflicts(signals) List~StrategySignal~
        -RankByROI(signals) List~StrategySignal~
    }
    
    class IAnalysisStrategy {
        <<interface>>
        +Analyze(snapshot, context) Task~List~StrategySignal~~
        +StrategyName string
        +MinConfidence double
    }
    
    class ScalpingStrategy {
        -ScalpingConfiguration _config
        +Analyze(snapshot, context) Task~List~StrategySignal~~
        -CalculateMomentum(history) double
        -CalculateVelocity(history) double
        -CalculateLiquidityScore(runner) double
    }
    
    class SteamMoveStrategy {
        -SteamMoveConfiguration _config
        +Analyze(snapshot, context) Task~List~StrategySignal~~
        -DetectVolumeSp ike(history) bool
        -CalculatePriceMovement(history) double
        -CalculateAcceleration(history) double
    }
    
    class ValueBetStrategy {
        -ValueBetConfiguration _config
        +Analyze(snapshot, context) Task~List~StrategySignal~~
        -CalculateTrueOdds(runner) double
        -CalculateExpectedValue(odds, stake) double
        -CalculateKellyStake(value, bankroll) decimal
    }
    
    StrategyOrchestrator --> IAnalysisStrategy
    IAnalysisStrategy <|.. ScalpingStrategy
    IAnalysisStrategy <|.. SteamMoveStrategy
    IAnalysisStrategy <|.. ValueBetStrategy
```

## Analyzers

```mermaid
classDiagram
    class SurebetDetector {
        -decimal _minProfitPercent
        +DetectOpportunities(snapshot) List~SurebetOpportunity~
        -CalculateArbitrage(back, lay) decimal
        -CalculateStakes(back, lay, capital) (decimal, decimal)
    }
    
    class WAPCalculator {
        -int _maxLevels
        +Calculate(runner) WAPResult
        -CalculateWeightedAveragePrice(prices) decimal
        -CalculateLiquidityDepth(prices) decimal
    }
    
    class WeightOfMoneyAnalyzer {
        +Analyze(snapshot) List~WoMResult~
        -CalculateBackPercentage(runner, total) double
        -CalculateLayPercentage(runner, total) double
        -RankByVolume(results) List~WoMResult~
    }
    
    class WAPResult {
        +decimal BackWAP
        +decimal LayWAP
        +decimal Spread
        +decimal BackLiquidity
        +decimal LayLiquidity
    }
    
    class SurebetOpportunity {
        +string BackSelectionId
        +decimal BackOdds
        +decimal StakeBack
        +string LaySelectionId
        +decimal LayOdds
        +decimal StakeLay
        +decimal ProfitPercentage
    }
    
    WAPCalculator --> WAPResult
    SurebetDetector --> SurebetOpportunity
```

## Signal Generation Flow

```mermaid
sequenceDiagram
    participant Analyst
    participant Orchestrator
    participant Strategies
    participant Metrics
    participant Redis
    
    Analyst->>Orchestrator: AnalyzeMarketAsync(snapshot)
    
    loop Each Strategy
        Orchestrator->>Strategies: Analyze(snapshot, context)
        Strategies->>Strategies: Calculate indicators
        Strategies->>Strategies: Apply filters
        
        alt Signal conditions met
            Strategies-->>Orchestrator: StrategySignal
        else No opportunity
            Strategies-->>Orchestrator: null
        end
    end
    
    Orchestrator->>Orchestrator: FilterByConfidence()
    Orchestrator->>Orchestrator: FilterByRisk()
    Orchestrator->>Orchestrator: ResolveConflicts()
    Orchestrator->>Orchestrator: RankByROI()
    Orchestrator-->>Analyst: Top N signals
    
    loop Each Signal
        Analyst->>Metrics: Update signal metrics
        Analyst->>Redis: PublishStrategySignal()
    end
```

## Metrics Tracking

```mermaid
graph LR
    subgraph "Prometheus Metrics"
        Counter1[snapshots_processed_total]
        Counter2[signals_generated_total]
        Counter3[surebets_found_total]
        Counter4[signals_by_type_total]
        Gauge1[strategy_avg_confidence]
        Gauge2[last_signal_confidence]
        Gauge3[last_signal_roi]
        Histogram1[processing_latency_seconds]
    end
    
    AnalystService --> Counter1
    AnalystService --> Counter2
    AnalystService --> Counter3
    AnalystService --> Counter4
    AnalystService --> Gauge1
    AnalystService --> Gauge2
    AnalystService --> Gauge3
    AnalystService --> Histogram1
```

## Configuration Structure

```mermaid
classDiagram
    class ProStrategiesConfiguration {
        +bool Enabled
        +ScalpingConfig Scalping
        +SteamMoveConfig SteamMove
        +ValueBetConfig ValueBet
        +GreenUpConfig GreenUp
        +OrchestratorConfig Orchestrator
    }
    
    class ScalpingConfiguration {
        +bool Enabled
        +decimal MinLiquidity
        +int MinTimeToStart
        +double MinConfidence
        +double MinMomentumThreshold
        +double MinVelocityThreshold
        +int MomentumPeriod
        +decimal BaseStake
    }
    
    class OrchestratorConfiguration {
        +double MinConfidence
        +double MinExpectedROI
        +RiskLevel MaxRisk
        +int MaxSignalsPerAnalysis
        +double ConflictThreshold
    }
    
    ProStrategiesConfiguration --> ScalpingConfiguration
    ProStrategiesConfiguration --> OrchestratorConfiguration
```

## Key Features

### 1. **Multi-Strategy Analysis**
- Parallel strategy execution
- Configurable strategy weights
- Conflict resolution

### 2. **Market Analysis**
- Surebet detection
- WAP calculation
- Weight of Money analysis
- Historical trend analysis

### 3. **Signal Quality**
- Confidence scoring
- ROI estimation
- Risk level classification
- Signal validation

### 4. **Performance**
- Efficient market history tracking
- Limited depth (15 snapshots)
- Minimal latency
- Prometheus metrics export

## Dependencies

```mermaid
graph TD
    Analyst[AIBettingAnalyst]
    Analyst --> Core[AIBettingCore]
    Analyst --> Redis[StackExchange.Redis]
    Analyst --> Serilog
    Analyst --> Prometheus[prometheus-net]
    Analyst --> Config[Microsoft.Extensions.Configuration]
```
