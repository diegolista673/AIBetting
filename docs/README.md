# AIBetting Solution

**Automated AI-powered betting system for Betfair markets with real-time monitoring and risk management.**

## 📋 Overview

AIBetting is a complete trading automation platform consisting of:
- **Real-time data ingestion** from Betfair Stream API
- **Multi-strategy analysis** with AI/ML components
- **Automated order execution** with comprehensive risk management
- **Full observability** with Prometheus/Grafana monitoring
- **Blazor dashboard** for real-time system visualization

## 🏗️ Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                     AIBetting Platform                        │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌────────────┐    ┌────────────┐    ┌────────────┐        │
│  │  Explorer  │───▶│  Analyst   │───▶│  Executor  │        │
│  │            │    │            │    │            │        │
│  │ Data       │    │ Signal     │    │ Order      │        │
│  │ Ingestion  │    │ Generation │    │ Execution  │        │
│  └────────────┘    └────────────┘    └────────────┘        │
│       │                  │                  │               │
│       └──────────────────┼──────────────────┘               │
│                          ▼                                   │
│                   ┌─────────────┐                           │
│                   │    Redis    │                           │
│                   │  (Message   │                           │
│                   │   Bus +     │                           │
│                   │   Cache)    │                           │
│                   └─────────────┘                           │
│                          │                                   │
│       ┌──────────────────┼──────────────────┐              │
│       ▼                  ▼                  ▼              │
│  ┌────────────┐    ┌────────────┐    ┌────────────┐      │
│  │ Prometheus │    │PostgreSQL  │    │  Grafana   │      │
│  │  Metrics   │    │Accounting  │    │Dashboards  │      │
│  └────────────┘    └────────────┘    └────────────┘      │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## 📦 Projects

### Core Components

| Project | Purpose | Port |
|---------|---------|------|
| **AIBettingCore** | Shared models, interfaces, services | - |
| **AIBettingExplorer** | Betfair data ingestion | 5001 |
| **AIBettingAnalyst** | Signal generation & strategies | 5002 |
| **AIBettingExecutor** | Order execution & risk management | 5003 |
| **AIBettingAccounting** | Trade logging & P&L tracking | - |
| **AIBettingBlazorDashboard** | Web UI for monitoring | 5000 |

### Infrastructure

| Service | Purpose | Port |
|---------|---------|------|
| **Redis** | Message bus & caching | 16379 |
| **PostgreSQL** | Trade & accounting database | 15432 |
| **Prometheus** | Metrics collection | 9090 |
| **Grafana** | Visualization & dashboards | 3000 |
| **Alertmanager** | Alert routing | 9093 |

## 🚀 Quick Start

### Prerequisites
- .NET 10 SDK
- Docker Desktop
- Redis (included in Docker stack)
- PostgreSQL (included in Docker stack)

### 1. Start Infrastructure

```bash
cd docker
docker compose up -d
```

This starts: Prometheus, Grafana, Redis, PostgreSQL, and all exporters.

### 2. Configure Applications

All applications use `appsettings.json`. Update Redis/PostgreSQL connection strings if needed:

```json
{
  "Redis": {
    "ConnectionString": "localhost:16379"
  }
}
```

### 3. Run Applications

**Option A: Run individually**
```bash
# Terminal 1
cd AIBettingExplorer
dotnet run

# Terminal 2
cd AIBettingAnalyst
dotnet run

# Terminal 3
cd AIBettingExecutor
dotnet run
```

**Option B: Use Visual Studio**
- Set multiple startup projects
- Select Explorer, Analyst, Executor
- Press F5

### 4. Access Dashboards

- **Grafana**: http://localhost:3000 (admin/admin)
- **Prometheus**: http://localhost:9090
- **Blazor Dashboard**: http://localhost:5000 (when running)

## 📊 Monitoring

### Grafana Dashboards

Pre-configured dashboards available in `grafana/dashboards/`:
- **AIBetting Analyst** - Signal generation metrics
- **AIBetting Executor** - Order execution performance (to be created)
- **Infrastructure** - Redis, PostgreSQL, system metrics

### Key Metrics

**Analyst:**
- `aibetting_analyst_snapshots_processed_total` - Market snapshots analyzed
- `aibetting_analyst_signals_generated_total` - Signals generated by strategy
- `aibetting_analyst_strategy_avg_confidence` - Strategy confidence levels

**Executor:**
- `aibetting_executor_orders_placed_total` - Orders placed
- `aibetting_executor_orders_matched_total` - Orders matched
- `aibetting_executor_circuit_breaker_status` - Circuit breaker state
- `aibetting_executor_account_balance` - Current balance

**Explorer:**
- `aibetting_price_updates_total` - Price updates received
- `aibetting_processing_latency_seconds` - Data processing latency

See `prometheus/README.md` for complete query reference.

## 🎯 Strategies

The Analyst implements multiple trading strategies:

### 1. Scalping Strategy
- Quick in-and-out trades based on momentum
- Targets high-liquidity markets
- Min confidence: 0.6

### 2. Steam Move Strategy
- Detects rapid price movements (steam)
- Volume spike detection
- Min confidence: 0.7

### 3. Value Bet Strategy
- Identifies mispriced odds
- Kelly Criterion staking
- Min confidence: 0.6

### 4. Surebet Detection
- Arbitrage opportunities
- Back/Lay price discrepancies
- Automatic stake calculation

## 🛡️ Risk Management

The Executor implements multi-layer risk controls:

### Circuit Breaker
- Automatically halts trading after X failures
- Configurable threshold and window
- Manual reset required

### Position Limits
- Max stake per order
- Max exposure per market
- Max exposure per selection
- Max daily loss

### Validation Pipeline
1. Signal age check
2. Stake limit validation
3. Exposure limit check
4. Daily loss verification
5. Circuit breaker state

## 📁 Project Structure

```
AIBettingSolution/
├── docker/                           # Docker infrastructure
│   └── docker-compose.yml            # All services definition
├── prometheus/                       # Prometheus configuration
│   ├── prometheus.yml                # Scrape config
│   ├── alert-rules.yml               # Alert definitions
│   ├── alertmanager.yml              # Alert routing
│   └── README.md                     # Query reference
├── grafana/                          # Grafana configuration
│   ├── provisioning/
│   │   ├── datasources/              # Auto-configured datasources
│   │   └── dashboards/               # Dashboard provisioning
│   └── dashboards/                   # Dashboard JSON files
├── docs/                             # Documentation
│   ├── diagrams/                     # Architecture diagrams
│   │   ├── AIBettingCore-ClassDiagram.md
│   │   ├── AIBettingExplorer-ClassDiagram.md
│   │   ├── AIBettingAnalyst-ClassDiagram.md
│   │   └── AIBettingExecutor-ClassDiagram.md
│   └── README.md                     # This file
├── AIBettingCore/                    # Shared library
├── AIBettingExplorer/                # Data ingestion
├── AIBettingAnalyst/                 # Signal generation
├── AIBettingExecutor/                # Order execution
├── AIBettingAccounting/              # Trade logging
└── AIBettingBlazorDashboard/         # Web UI
```

## 🔧 Configuration

### Environment Variables

```bash
# Redis
REDIS_CONNECTION_STRING=localhost:16379

# PostgreSQL
POSTGRES_CONNECTION_STRING=Host=localhost;Port=15432;...

# Betfair
BETFAIR_APP_KEY=your_app_key
BETFAIR_SESSION_TOKEN=your_token
```

### appsettings.json

Each application has its own configuration file. Key settings:

**Executor (`AIBettingExecutor/appsettings.json`):**
```json
{
  "Executor": {
    "Risk": {
      "Enabled": true,
      "CircuitBreakerEnabled": true,
      "MaxStakePerOrder": 100.0,
      "MaxExposurePerMarket": 500.0,
      "MaxDailyLoss": 500.0
    },
    "Trading": {
      "EnablePaperTrading": true,
      "UseMockBetfair": true
    }
  }
}
```

## 📚 Documentation

- **Architecture Diagrams**: `docs/diagrams/` - Class diagrams with Mermaid
- **Prometheus Queries**: `prometheus/README.md` - Useful PromQL queries
- **API Documentation**: Each project has inline XML comments

## 🧪 Testing

### Mock Mode
Run Executor with `UseMockBetfair: true` for testing without real Betfair API:

```json
{
  "Trading": {
    "UseMockBetfair": true,
    "EnablePaperTrading": true
  }
}
```

### Load Testing
Use the included scripts to simulate price updates:

```bash
# Publish test price update to Redis
docker exec aibetting-redis redis-cli PUBLISH "channel:price-updates" '{...}'
```

## 🐛 Troubleshooting

### Common Issues

**1. Redis connection timeout**
- Check Docker container is running: `docker ps | findstr redis`
- Verify port 16379 is accessible
- Check `appsettings.json` uses correct port

**2. Prometheus targets DOWN**
- Verify applications are running and exposing metrics
- Check firewall isn't blocking ports 5001, 5002, 5003
- Restart Prometheus: `docker restart aibetting-prometheus-v2`

**3. Grafana shows "No data"**
- Verify Prometheus datasource configured
- Check applications are generating data
- Adjust time range in Grafana

See project-specific README files for more troubleshooting tips.

## 📈 Performance

- **Explorer**: <10ms latency per price update
- **Analyst**: ~50ms per market analysis
- **Executor**: <200ms order execution (P99)
- **System**: Handles 1000+ markets concurrently

## 🔒 Security

**WARNING**: This is a development/testing setup. For production:
- Change all default passwords
- Enable Redis authentication
- Use SSL/TLS for PostgreSQL
- Implement API authentication
- Use secrets management (Azure Key Vault, HashiCorp Vault)
- Enable network isolation

## 📄 License

[Your license here]

## 👥 Contributing

[Contributing guidelines]

## 🆘 Support

For issues or questions:
1. Check documentation in `docs/`
2. Review Prometheus metrics and Grafana dashboards
3. Check application logs in `logs/` directories
4. Create an issue on GitHub

---

**Built with .NET 10, Prometheus, Grafana, Redis, PostgreSQL, and ❤️**
