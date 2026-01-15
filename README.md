# AIBetting Solution

**Automated AI-powered betting system for Betfair markets**

## 🚀 Quick Start

```bash
# 1. Start infrastructure
cd docker
docker compose up -d

# 2. Run applications
dotnet run --project AIBettingExplorer
dotnet run --project AIBettingAnalyst  
dotnet run --project AIBettingExecutor

# 3. Access dashboards
# Grafana: http://localhost:3000 (admin/admin)
# Prometheus: http://localhost:9090
```

## 📚 Full Documentation

See **[docs/README.md](docs/README.md)** for complete documentation including:
- Architecture overview
- Configuration guide
- Monitoring setup
- Troubleshooting

## 📁 Project Structure

```
├── docker/              # Docker infrastructure (Prometheus, Grafana, Redis, PostgreSQL)
├── prometheus/          # Prometheus configuration and alert rules
├── grafana/             # Grafana dashboards and provisioning
├── docs/                # Complete documentation and class diagrams
├── AIBettingCore/       # Shared library
├── AIBettingExplorer/   # Data ingestion (port 5001)
├── AIBettingAnalyst/    # Signal generation (port 5002)
├── AIBettingExecutor/   # Order execution (port 5003)
├── AIBettingAccounting/ # Trade logging
└── AIBettingBlazorDashboard/  # Web UI (port 5000)
```

## 🎯 Key Features

- **Real-time data ingestion** from Betfair Stream API
- **Multi-strategy analysis** (Scalping, Steam Move, Value Bets, Surebets)
- **Automated order execution** with comprehensive risk management
- **Circuit breaker** protection against excessive losses
- **Full observability** with Prometheus metrics and Grafana dashboards
- **Blazor dashboard** for real-time visualization

## 📊 Monitoring

- **Prometheus**: http://localhost:9090
- **Grafana**: http://localhost:3000
- **Metrics endpoints**:
  - Explorer: http://localhost:5001/metrics
  - Analyst: http://localhost:5002/metrics
  - Executor: http://localhost:5003/metrics

## 🛡️ Risk Management

The system includes multi-layer risk controls:
- Circuit breaker (auto-halt on failures)
- Position limits per market/selection
- Daily loss limits
- Stake limits
- Real-time exposure tracking

## 🔧 Configuration

Each application uses `appsettings.json`. Key settings:

```json
{
  "Redis": {
    "ConnectionString": "localhost:16379"
  },
  "Executor": {
    "Risk": {
      "Enabled": true,
      "MaxStakePerOrder": 100.0,
      "MaxDailyLoss": 500.0
    },
    "Trading": {
      "UseMockBetfair": true  // Set false for production
    }
  }
}
```

## 📈 Architecture

```
Betfair API → Explorer → Redis → Analyst → Redis → Executor → Betfair API
                  ↓                  ↓              ↓
              Prometheus ← ─ ─ ─ ─ ─ ┴ ─ ─ ─ ─ ─ ─ ┘
                  ↓
              Grafana
```

## 🧪 Development Mode

Run with mock Betfair client for testing:

```json
{
  "Trading": {
    "UseMockBetfair": true,
    "EnablePaperTrading": true
  }
}
```

## 📄 License

[Your license]

## 🆘 Support

- Documentation: `docs/README.md`
- Class diagrams: `docs/diagrams/`
- Prometheus queries: `prometheus/README.md`

---

Built with .NET 10 • Prometheus • Grafana • Redis • PostgreSQL
