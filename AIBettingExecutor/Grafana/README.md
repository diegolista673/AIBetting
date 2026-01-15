# 📊 Grafana Dashboard - AIBetting Executor

## 🎯 Overview

Dashboard completa per monitoraggio real-time dell'**AIBettingExecutor** con 22 pannelli visuali che tracciano:
- ✅ Stato connessione Betfair e circuit breaker
- 📈 Rate ordini (placed, matched, cancelled, failed)
- 💰 Balance account e exposure
- ⚡ Latency (signal→order, Betfair API)
- 🎯 Distribuzione segnali per strategia
- 🚨 Errori e rejection reasons

---

## 📚 **DOCUMENTAZIONE COMPLETA**

**Prima di iniziare, leggi questi documenti:**

1. **✅ [SOLUTION_SUMMARY.md](SOLUTION_SUMMARY.md)** - **INIZIA QUI!** Risoluzione completa errore Redis connection
2. **[STACK_RESTORATION_COMPLETE.md](STACK_RESTORATION_COMPLETE.md)** - Guida completa ripristino stack Docker
3. **[CONFIGURATION_CHANGES.md](CONFIGURATION_CHANGES.md)** - Dettagli modifiche configurazioni
4. **[README.md](README.md)** - Questo file - Setup Grafana dashboard

**Scripts PowerShell disponibili:**
- `verify-stack.ps1` - Verifica health check completa
- `test-connectivity.ps1` - Test Redis e PostgreSQL
- `test-e2e.ps1` - Test end-to-end completo
- `migration-summary.ps1` - Riepilogo migrazione

---

## 🚀 Quick Start

### Prerequisites

1. **Docker Stack** avviato con Redis, PostgreSQL, Prometheus, Grafana
2. **Configurazioni** aggiornate (vedi CONFIGURATION_CHANGES.md)

### Installation

#### Step 1: Avvia Stack Docker

```bash
cd AIBettingExecutor\Grafana
docker compose up -d
docker compose ps  # Verifica tutti running
```

Servizi disponibili:
- ✅ Prometheus: http://localhost:9090
- ✅ Grafana: http://localhost:3000 (admin/admin)
- ✅ Alertmanager: http://localhost:9093
- ✅ Redis: localhost:16379
- ✅ PostgreSQL: localhost:15432

#### Step 2: Verifica Prometheus

Accedi a http://localhost:9090/targets e verifica i target configurati.

#### Step 3: Configure Prometheus Data Source in Grafana

1. Login a Grafana: `http://localhost:3000`
2. Vai a **Configuration** → **Data Sources**
3. Click **Add data source**
4. Seleziona **Prometheus**
5. Configura:
   - **Name**: `Prometheus`
   - **URL**: `http://localhost:9090`
   - **Access**: `Server (default)`
6. Click **Save & Test**

#### Step 4: Import Dashboard

**Metodo 1: Import JSON**
1. Vai a **Dashboards** → **Import**
2. Click **Upload JSON file**
3. Seleziona `executor-dashboard.json`
4. Seleziona datasource **Prometheus**
5. Click **Import**

**Metodo 2: Copy-Paste**
1. Vai a **Dashboards** → **Import**
2. Copia contenuto di `executor-dashboard.json`
3. Incolla nel campo JSON
4. Click **Load**
5. Seleziona datasource **Prometheus**
6. Click **Import**

---

## 📊 Dashboard Panels

### 🔝 Top Row - System Health

| Panel | Metric | Description | Alert Threshold |
|-------|--------|-------------|-----------------|
| **🔗 Betfair Connection Status** | `aibetting_executor_betfair_connection_status` | 0=Disconnected, 1=Connected | Alert if 0 |
| **🔴 Circuit Breaker Status** | `aibetting_executor_circuit_breaker_status` | 0=Open, 1=Triggered | Alert if 1 |
| **📊 Active Orders** | `aibetting_executor_active_orders` | Ordini in tracking | Warning if >50 |
| **💰 Account Balance** | `aibetting_executor_account_balance` | Balance totale Betfair | Alert if <1000 |

### 📈 Order Metrics

| Panel | Query | Description |
|-------|-------|-------------|
| **Order Rate (per minute)** | `rate(aibetting_executor_orders_placed_total[5m]) * 60` | Ordini placed, matched, cancelled, failed al minuto |
| **Account Balances** | Multiple series | Balance, Available, Exposure stacked |

### ⚡ Latency Panels

| Panel | Percentiles | Target |
|-------|-------------|--------|
| **Order Execution Latency** | P50, P95, P99 | <200ms (P99) |
| **Betfair API Latency** | P50, P95, P99 | <500ms (P99) |

Queries:
```promql
# P99 Execution Latency
histogram_quantile(0.99, rate(aibetting_executor_order_execution_latency_seconds_bucket[5m]))

# P99 API Latency
histogram_quantile(0.99, rate(aibetting_executor_betfair_api_latency_seconds_bucket[5m]))
```

### 🥧 Distribution Panels (24h)

1. **Orders by Side** - Pie chart Back vs Lay
2. **Signals by Strategy** - Distribuzione per strategia (Surebet, Scalping, Steam Move, Value Bet)
3. **Cancellation Reasons** - Top cancellation reasons

### 📋 Tables

| Panel | Query | Description |
|-------|-------|-------------|
| **Top Signal Rejection Reasons** | `topk(10, sum(increase(...[1h])) by (reason))` | Top 10 motivi rejection ultima ora |
| **Betfair API Errors** | `topk(10, sum(increase(...[1h])) by (error_type))` | Top 10 errori API ultima ora |

### 📊 Stats Panels (24h)

Bottom row mostra totali 24h:
- **Orders Placed** - Total placed
- **Orders Matched** - Total matched
- **Orders Cancelled** - Total cancelled (warning if >10)
- **Orders Failed** - Total failed (alert if >1)
- **Signals Received** - Total signals
- **Signals Rejected** - Total rejected (warning if >50)

### 💵 Financial Metrics

| Panel | Metric | Alert |
|-------|--------|-------|
| **Total Stake Deployed** | Cumulative | - |
| **Available Balance** | Current available | Alert if <500 |
| **Current Exposure** | Live exposure | Warning >300, Alert >450 |

---

## 🎨 Color Coding

### Status Panels
- 🟢 **Green**: OK / Connected / Open
- 🔴 **Red**: Error / Disconnected / Triggered

### Thresholds
- **Balance**: Yellow <5000, Red <1000
- **Exposure**: Yellow >300, Red >450
- **Latency**: Yellow >150ms, Red >200ms (execution)
- **API Latency**: Yellow >400ms, Red >500ms

---

## 🔔 Alerting Rules

### Configurazione Alert Prometheus

Crea `alert-rules.yml`:

```yaml
groups:
  - name: aibetting_executor_alerts
    interval: 30s
    rules:
      # Critical: Betfair Disconnected
      - alert: BetfairDisconnected
        expr: aibetting_executor_betfair_connection_status == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "Betfair API disconnected"
          description: "Executor lost connection to Betfair API"

      # Critical: Circuit Breaker Triggered
      - alert: CircuitBreakerTriggered
        expr: aibetting_executor_circuit_breaker_status == 1
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "Circuit breaker activated"
          description: "Too many order failures - trading halted"

      # Warning: High Order Failure Rate
      - alert: HighOrderFailureRate
        expr: rate(aibetting_executor_orders_failed_total[5m]) > 0.1
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High order failure rate"
          description: "More than 6 orders failing per minute"

      # Warning: High Signal Rejection Rate
      - alert: HighSignalRejectionRate
        expr: rate(aibetting_executor_signals_rejected_total[5m]) / rate(aibetting_executor_signals_received_total[5m]) > 0.5
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High signal rejection rate"
          description: "More than 50% of signals being rejected"

      # Warning: High Execution Latency
      - alert: HighExecutionLatency
        expr: histogram_quantile(0.99, rate(aibetting_executor_order_execution_latency_seconds_bucket[5m])) > 0.2
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High order execution latency"
          description: "P99 latency > 200ms for 5 minutes"

      # Warning: Low Account Balance
      - alert: LowAccountBalance
        expr: aibetting_executor_available_balance < 500
        for: 1m
        labels:
          severity: warning
        annotations:
          summary: "Low account balance"
          description: "Available balance below £500"

      # Critical: Very Low Account Balance
      - alert: CriticalAccountBalance
        expr: aibetting_executor_available_balance < 100
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "Critical account balance"
          description: "Available balance below £100 - trading will halt"

      # Warning: High Exposure
      - alert: HighExposure
        expr: aibetting_executor_current_exposure > 450
        for: 2m
        labels:
          severity: warning
        annotations:
          summary: "High exposure"
          description: "Current exposure exceeds £450"
```

Aggiungi a `prometheus.yml`:
```yaml
rule_files:
  - "alert-rules.yml"

alerting:
  alertmanagers:
    - static_configs:
        - targets: ['localhost:9093']
```

### Grafana Alerts

1. **Circuit Breaker Alert**:
   - Panel: Circuit Breaker Status
   - Condition: `WHEN last() OF query(A, 1m) IS ABOVE 0`
   - Notification: Email/Slack immediato

2. **Disconnection Alert**:
   - Panel: Betfair Connection Status
   - Condition: `WHEN last() OF query(A, 1m) IS BELOW 1`
   - Notification: Email/Slack immediato

3. **High Latency Alert**:
   - Panel: Order Execution Latency
   - Condition: `WHEN p99 OF query(C, 5m) IS ABOVE 0.2`
   - Notification: Email dopo 5 minuti

---

## 📱 Dashboard Usage

### Monitoring Routine

#### Every 5 Minutes
- ✅ Check connection status (green)
- ✅ Check circuit breaker (not triggered)
- ✅ Verify active orders count reasonable

#### Every Hour
- 📊 Review order rate trends
- 💰 Check balance and exposure levels
- ⚡ Monitor latency percentiles
- 🚨 Review rejection reasons table

#### Daily
- 📈 Analyze 24h stats
- 🥧 Review strategy distribution
- 📋 Check top errors
- 💵 Verify P&L alignment with accounting

### Troubleshooting with Dashboard

#### 🔴 **Circuit Breaker Triggered**
1. Check **Top Signal Rejection Reasons** table
2. Review **Betfair API Errors** table
3. Check **Order Failure Rate** spike
4. Action: Fix root cause, then `redis-cli SET trading:enabled true`

#### ⚠️ **High Latency Spike**
1. Compare **Execution Latency** vs **API Latency**
2. If API latency high → Betfair issue
3. If execution latency high → Check Redis/CPU on Executor host
4. Review active orders count (may be overloaded)

#### 💰 **Dropping Balance**
1. Check **Orders Matched** (high volume?)
2. Review **Exposure** trend (locked in positions?)
3. Cross-reference with Accounting for P&L
4. Consider adjusting risk limits in `appsettings.json`

#### 🚫 **High Cancellation Rate**
1. Check **Cancellation Reasons** pie chart
2. If "timeout" dominant → Markets illiquid, adjust `UnmatchedOrderTimeoutSeconds`
3. If "risk_limit" → Exposure too high, check limits
4. If "signal_expired" → Analyst generating slow signals

---

## 🔗 Integration with Other Services

### Multi-Service Dashboard

Per view completa sistema, aggiungi panels da:

**AIBettingAnalyst** (porta 5002):
```promql
# Signals generated rate
rate(aibetting_analyst_signals_generated_total[5m])

# Strategy confidence
aibetting_analyst_strategy_avg_confidence
```

**AIBettingExplorer** (porta 5001):
```promql
# Price updates rate
rate(aibetting_price_updates_total[5m])

# Processing latency
aibetting_processing_latency_seconds
```

### Combined View Row

Aggiungi row "System Overview":
- Explorer: Price updates/min
- Analyst: Signals generated/min
- Executor: Orders placed/min
- Accounting: Trades logged/min

---

## 🎯 Dashboard Variables

La dashboard supporta variable `$DS_PROMETHEUS` per datasource selection.

### Custom Variables (opzionali)

Aggiungi in **Dashboard Settings** → **Variables**:

#### **Strategy Filter**
```yaml
Name: strategy
Type: Query
Query: label_values(aibetting_executor_signals_received_total, strategy)
Multi-value: true
Include All: true
```

Uso nei panels:
```promql
sum(rate(aibetting_executor_signals_received_total{strategy=~"$strategy"}[5m])) by (strategy)
```

#### **Time Range Quick Selects**
- Last 15 minutes
- Last 1 hour
- Last 6 hours (default)
- Last 24 hours
- Last 7 days

---

## 📸 Screenshots & Examples

### Normal Operation
- Connection status: 🟢 CONNECTED
- Circuit breaker: 🟢 OPEN
- Active orders: 3-10
- P99 latency: <150ms
- Balance: >£5,000

### Under Load
- Active orders: 20-40
- Order rate: 50-100/min
- P99 latency: 150-200ms
- Exposure: £200-300

### Alert Condition
- Connection status: 🔴 DISCONNECTED
- Circuit breaker: 🔴 TRIGGERED
- Failed orders spike
- High rejection rate

---

## 🔧 Customization

### Adding Custom Panels

Example: Win Rate Panel
```json
{
  "targets": [
    {
      "expr": "sum(increase(aibetting_executor_orders_matched_total[24h])) / sum(increase(aibetting_executor_orders_placed_total[24h])) * 100",
      "legendFormat": "Win Rate %"
    }
  ],
  "title": "Order Match Rate (24h)",
  "type": "stat"
}
```

### Threshold Adjustment

Edit panel → Field → Thresholds:
```json
{
  "mode": "absolute",
  "steps": [
    {"color": "green", "value": null},
    {"color": "yellow", "value": 300},
    {"color": "red", "value": 450}
  ]
}
```

---

## 📦 Export & Backup

### Backup Dashboard
```bash
# Via Grafana API
curl -H "Authorization: Bearer YOUR_API_KEY" \
  http://localhost:3000/api/dashboards/uid/aibetting-executor \
  | jq . > executor-dashboard-backup.json
```

### Version Control
Commit `executor-dashboard.json` to Git:
```bash
git add AIBettingExecutor/Grafana/executor-dashboard.json
git commit -m "Update Executor dashboard v1.1"
```

---

## 🆘 Troubleshooting

### Dashboard Not Loading
- ✅ Verify Prometheus datasource configured
- ✅ Check Prometheus is scraping Executor (`:9090/targets`)
- ✅ Verify Executor metrics endpoint: `curl localhost:5003/metrics`

### No Data in Panels
- ✅ Check time range (default: last 6h)
- ✅ Verify Executor is running: `ps aux | grep AIBettingExecutor`
- ✅ Check Prometheus scrape interval (15s default)

### Incorrect Values
- ✅ Verify metric names match Executor code (`ExecutorMetrics.cs`)
- ✅ Check label matching (case-sensitive)
- ✅ Review Prometheus query in panel (click title → Edit)

---

## 📚 Resources

- **Prometheus Docs**: https://prometheus.io/docs/
- **Grafana Docs**: https://grafana.com/docs/grafana/latest/
- **PromQL Guide**: https://prometheus.io/docs/prometheus/latest/querying/basics/
- **Grafana Alerting**: https://grafana.com/docs/grafana/latest/alerting/

---

## 🎉 Quick Commands

```bash
# Start Executor (will expose metrics on :5003)
cd AIBettingExecutor
dotnet run

# Query Prometheus for specific metric
curl http://localhost:9090/api/v1/query?query=aibetting_executor_active_orders

# Export dashboard
curl http://localhost:3000/api/dashboards/uid/aibetting-executor | jq .

# Test alert rules
promtool check rules alert-rules.yml

# View Prometheus targets
curl http://localhost:9090/api/v1/targets | jq .
```

---

**🚀 Happy Monitoring! Con questa dashboard hai controllo completo dell'Executor in tempo reale.**
