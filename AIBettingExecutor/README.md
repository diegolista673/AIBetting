# 🎯 AIBettingExecutor

**Automated trading execution engine for Betfair Exchange**

## 📖 Overview

AIBettingExecutor is the **execution layer** of the AIBetting system. It subscribes to trading signals from the Analyst, 
validates risk constraints, places orders on Betfair Exchange, monitors order states, and logs trades for accounting.

## 🏗️ Architecture

### Components

```
┌─────────────────────────────────────────────────────────────┐
│                     ExecutorService                         │
│  (Main orchestrator - signal handling & lifecycle)          │
└───────────┬──────────────────────────────────┬──────────────┘
            │                                  │
┌───────────▼──────────┐           ┌──────────▼───────────────┐
│  SignalProcessor     │           │  BetfairClient           │
│  (Redis subscription)│           │  (API & Authentication)  │
└───────────┬──────────┘           └──────────┬───────────────┘
            │                                  │
┌───────────▼──────────┐           ┌──────────▼───────────────┐
│  RiskValidator       │           │  OrderManager            │
│  (Exposure & limits) │           │  (State tracking)        │
└───────────┬──────────┘           └──────────┬───────────────┘
            │                                  │
            └──────────────┬───────────────────┘
                           │
                  ┌────────▼─────────┐
                  │  TradeLogger     │
                  │  (Accounting)    │
                  └──────────────────┘
```

### Key Features

✅ **Signal Subscription** - Listens to Redis channels for surebet and strategy signals  
✅ **Risk Management** - Validates orders against exposure limits and circuit breaker  
✅ **Betfair Integration** - SSL certificate authentication and REST API calls  
✅ **Order State Machine** - Tracks Pending → Matched/Cancelled with timeout handling  
✅ **Trade Logging** - Persists executed trades to Redis for accounting  
✅ **Prometheus Metrics** - Comprehensive monitoring of orders, latency, and risk  
✅ **Paper Trading Mode** - Test signals without real order execution  

---

## 🚀 Quick Start

### Prerequisites

1. **Betfair Account** with API access
2. **SSL Certificate** (`.pfx` format) for authentication
3. **Redis** instance running (for signals and state)
4. **Analyst Service** running (to generate signals)

### Configuration

Edit `appsettings.json`:

```json
{
  "Executor": {
    "Betfair": {
      "AppKey": "YOUR_BETFAIR_APP_KEY",
      "CertificatePath": "certs/betfair-client.pfx",
      "CertificatePassword": "YOUR_CERT_PASSWORD"
    },
    "Trading": {
      "EnablePaperTrading": true  // Set to false for live trading
    }
  }
}
```

### Running

```bash
cd AIBettingExecutor
dotnet run
```

Expected output:
```
🚀 AIBetting Executor starting
=================================
Configuration loaded:
  Betfair AppKey: YourAppKey
  Certificate: certs/betfair-client.pfx
  Risk Management: ENABLED
  Paper Trading: ENABLED
📊 Prometheus metrics server started on port 5003
✅ Redis connected
Authenticating with Betfair API...
✅ Betfair authentication successful
✅ Executor active - monitoring for trading signals
💡 Press Ctrl+C to stop
```

---

## ⚙️ Configuration Reference

### Betfair Settings

| Parameter | Description | Default |
|-----------|-------------|---------|
| `AppKey` | Betfair API application key | *Required* |
| `CertificatePath` | Path to `.pfx` certificate | *Required* |
| `CertificatePassword` | Certificate password | *Required* |
| `ApiTimeoutSeconds` | HTTP request timeout | 30 |
| `ReauthenticationIntervalHours` | Session renewal interval | 6 |

### Risk Management

| Parameter | Description | Default |
|-----------|-------------|---------|
| `Enabled` | Enable risk validation | `true` |
| `CircuitBreakerEnabled` | Enable circuit breaker | `true` |
| `MaxStakePerOrder` | Maximum stake per order | 100.0 |
| `MaxExposurePerMarket` | Max exposure per market | 500.0 |
| `MaxExposurePerSelection` | Max exposure per selection | 200.0 |
| `MaxDailyLoss` | Daily loss limit | 500.0 |
| `MaxRiskPerTradePercent` | Risk per trade (0.02 = 2%) | 0.02 |

### Order Manager

| Parameter | Description | Default |
|-----------|-------------|---------|
| `UnmatchedOrderTimeoutSeconds` | Cancel unmatched orders after | 30 |
| `StatusCheckIntervalSeconds` | Check order status every | 5 |
| `MaxOrdersPerCheck` | Max orders to check per cycle | 10 |

### Trading

| Parameter | Description | Default |
|-----------|-------------|---------|
| `EnablePaperTrading` | Test mode (no real orders) | `true` |
| `CommissionRate` | Betfair commission rate | 0.05 (5%) |
| `MinOdds` | Minimum allowed odds | 1.01 |
| `MaxOdds` | Maximum allowed odds | 1000.0 |
| `MinStake` | Minimum stake amount | 2.0 |

---

## 📊 Prometheus Metrics

Executor exposes metrics on `http://localhost:5003/metrics`

### Orders

- `aibetting_executor_orders_placed_total{side,market_type}` - Total orders placed
- `aibetting_executor_orders_matched_total{side}` - Total matched orders
- `aibetting_executor_orders_cancelled_total{reason}` - Total cancelled orders
- `aibetting_executor_orders_failed_total{reason}` - Total failed orders
- `aibetting_executor_active_orders` - Current active orders

### Signals

- `aibetting_executor_signals_received_total{strategy}` - Signals received
- `aibetting_executor_signals_rejected_total{reason}` - Signals rejected by risk

### Execution

- `aibetting_executor_order_execution_latency_seconds` - Signal to order latency
- `aibetting_executor_order_match_latency_seconds` - Order to match latency

### Risk

- `aibetting_executor_current_exposure` - Total exposure across markets
- `aibetting_executor_risk_violations_total{violation_type}` - Risk limit violations
- `aibetting_executor_circuit_breaker_status` - Circuit breaker state (0/1)

### Betfair API

- `aibetting_executor_betfair_connection_status` - Connection status (0/1)
- `aibetting_executor_betfair_api_errors_total{error_type}` - API errors
- `aibetting_executor_betfair_api_latency_seconds` - API request latency

### Account

- `aibetting_executor_account_balance` - Current balance
- `aibetting_executor_available_balance` - Available to bet
- `aibetting_executor_total_stake_deployed` - Total stake deployed

---

## 🔒 Risk Management

### Validation Rules

1. **Stake Limits** - Order stake ≤ `MaxStakePerOrder`
2. **Bankroll Risk** - Order risk ≤ `MaxRiskPerTradePercent` × Bankroll
3. **Selection Exposure** - Per-selection exposure ≤ `MaxExposurePerSelection`
4. **Market Exposure** - Per-market exposure ≤ `MaxExposurePerMarket`
5. **Daily Loss Limit** - Daily P&L > -`MaxDailyLoss`
6. **Circuit Breaker** - Activated after N consecutive failures
7. **Trading Enabled** - Global kill-switch via Redis

### Circuit Breaker

Automatically triggers after:
- 5 consecutive order failures within 10 minutes
- Manual activation via Redis key: `trading:enabled = false`

**When triggered:**
- All incoming signals rejected
- No new orders placed
- Active orders remain monitored

**To reset:**
```bash
redis-cli SET trading:enabled true
```

---

## 📝 Signal Processing

### Supported Signal Types

#### 1. Surebet Signals (Legacy)
Channel: `channel:trading-signals`

```json
{
  "marketId": "1.200000000",
  "strategy": "surebet",
  "backSelectionId": "123456",
  "backOdds": 2.5,
  "stakeBack": 50.0,
  "laySelectionId": "654321",
  "layOdds": 2.6,
  "stakeLay": 48.0,
  "expectedROI": 1.5,
  "confidence": 0.9
}
```

#### 2. Strategy Signals (PRO)
Channel: `channel:strategy-signals`

```json
{
  "signalId": "abc-123",
  "marketId": "1.200000000",
  "strategy": "ScalpingStrategy",
  "signalType": "SCALP_LONG",
  "confidence": 0.75,
  "expectedROI": 2.3,
  "risk": "Medium",
  "primarySelection": {
    "selectionId": "123456",
    "selectionName": "Horse A",
    "recommendedOdds": 3.5,
    "stake": 50.0,
    "betType": "Back"
  },
  "validityWindow": 30
}
```

### Signal Flow

```
Signal Published → SignalProcessor → RiskValidator → BetfairClient → OrderManager → TradeLogger
                                            ↓
                                     (If rejected)
                                    Metrics Updated
```

---

## 🔧 Order State Machine

```
PENDING
   ↓
   ├─→ MATCHED (fully matched) → TradeLogger → Accounting
   ├─→ UNMATCHED (partial/no match after timeout) → CANCELLED
   └─→ CANCELLED (API error, risk rejection, manual cancel)
```

### Monitoring Timer
- Checks order status every 5 seconds
- Cancels unmatched orders after 30 seconds
- Updates Prometheus metrics in real-time

---

## 🧪 Testing

### Paper Trading Mode

Safe testing without real orders:

```json
{
  "Trading": {
    "EnablePaperTrading": true
  }
}
```

**What happens:**
- Signals received and validated
- Risk checks performed
- NO orders sent to Betfair
- All metrics updated
- Logs show: `📄 PAPER TRADING MODE - Signal logged but not executed`

### Manual Signal Testing

Publish test signal to Redis:

```bash
redis-cli PUBLISH channel:strategy-signals '{
  "signalId": "test-123",
  "marketId": "1.999999",
  "strategy": "TestStrategy",
  "signalType": "TEST",
  "confidence": 0.8,
  "expectedROI": 1.0,
  "risk": "Low",
  "primarySelection": {
    "selectionId": "99999",
    "selectionName": "Test Selection",
    "recommendedOdds": 2.0,
    "stake": 10.0,
    "betType": "Back"
  },
  "validityWindow": 60
}'
```

---

## 🛠️ Troubleshooting

### Common Issues

#### ❌ "Failed to authenticate with Betfair API"

**Cause:** Invalid certificate or credentials  
**Solution:**
1. Verify certificate path exists
2. Check certificate password
3. Ensure certificate is in `.pfx` format
4. Verify AppKey is valid for your account

#### ❌ "Signal rejected by risk validation"

**Cause:** Order exceeds risk limits  
**Check:**
```bash
redis-cli HGETALL risk:limits
redis-cli GET trading:enabled
```

**Solution:**
1. Adjust risk limits in `appsettings.json`
2. Check circuit breaker status
3. Verify bankroll is sufficient

#### ❌ "Order failed: INVALID_BET_SIZE"

**Cause:** Stake too small or exceeds balance  
**Solution:**
1. Check `MinStake` configuration (min 2.0 for most markets)
2. Verify account balance via Betfair website
3. Check exposure limits

### Debug Logging

Enable verbose logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "AIBettingExecutor": "Debug"
    }
  }
}
```

Logs location: `logs/executor-YYYYMMDD.log`

---

## 📦 Dependencies

- **.NET 10** - Runtime
- **StackExchange.Redis** - Redis client
- **Prometheus-net** - Metrics exporter
- **Serilog** - Structured logging
- **System.Text.Json** - JSON serialization

---

## 🔗 Integration Points

### Upstream
- **AIBettingAnalyst** - Provides trading signals via Redis

### Downstream
- **AIBettingAccounting** - Receives trade logs for persistence
- **Betfair Exchange API** - Order execution
- **Redis** - State storage and pub/sub

### Monitoring
- **Prometheus** - Scrapes metrics endpoint
- **Grafana** - Visualizes metrics (optional)

---

## 🚨 Production Checklist

Before going live:

- [ ] Disable paper trading: `EnablePaperTrading: false`
- [ ] Configure valid Betfair credentials
- [ ] Install SSL certificate in correct path
- [ ] Set appropriate risk limits for your bankroll
- [ ] Test with small stakes first (e.g., £2-5)
- [ ] Configure circuit breaker thresholds
- [ ] Set up Prometheus alerting
- [ ] Monitor logs for 24h in paper mode
- [ ] Verify accounting integration
- [ ] Have manual kill-switch ready: `redis-cli SET trading:enabled false`

---

## 📈 Performance

**Latency Targets:**
- Signal to order: < 200ms (99th percentile)
- Order placement to Betfair: < 500ms
- Risk validation: < 50ms

**Capacity:**
- Supports 100+ concurrent active orders
- Handles 1000+ signals/hour
- Redis pub/sub < 10ms latency

---

## 🆘 Support & Monitoring

### Health Check

```bash
# Check if executor is running
curl http://localhost:5003/metrics | grep aibetting_executor_betfair_connection_status

# Expected: aibetting_executor_betfair_connection_status 1
```

### Manual Override

```bash
# Emergency stop (circuit breaker)
redis-cli SET trading:enabled false

# Resume trading
redis-cli SET trading:enabled true

# Check current exposure
redis-cli HGETALL risk:exposure:1.200000000

# View active orders count
redis-cli GET executor:active_orders_count
```

---

## 📄 License

Part of AIBetting system - All rights reserved

---

## 👥 Contributing

This is a production trading system. Code changes should be:
1. Thoroughly tested in paper trading mode
2. Reviewed for risk management implications
3. Performance tested under load
4. Documented with clear comments

---

**⚠️ DISCLAIMER: Trading on betting exchanges carries financial risk. This software is provided as-is without any guarantees. Always test thoroughly and never risk more than you can afford to lose.**
