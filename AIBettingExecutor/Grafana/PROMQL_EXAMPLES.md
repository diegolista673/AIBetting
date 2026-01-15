# 📊 PromQL Query Examples for AIBetting Executor

## 🎯 Basic Metrics

### Current Values

```promql
# Active orders right now
aibetting_executor_active_orders

# Current account balance
aibetting_executor_account_balance

# Available balance
aibetting_executor_available_balance

# Current exposure
aibetting_executor_current_exposure

# Connection status (0=disconnected, 1=connected)
aibetting_executor_betfair_connection_status

# Circuit breaker status (0=open, 1=triggered)
aibetting_executor_circuit_breaker_status
```

---

## 📈 Rates & Trends

### Order Rates

```promql
# Orders placed per second (5min avg)
rate(aibetting_executor_orders_placed_total[5m])

# Orders placed per minute
rate(aibetting_executor_orders_placed_total[5m]) * 60

# Orders matched per hour
rate(aibetting_executor_orders_matched_total[1h]) * 3600

# Order success rate (matched / placed)
rate(aibetting_executor_orders_matched_total[5m]) / rate(aibetting_executor_orders_placed_total[5m])

# Order failure rate
rate(aibetting_executor_orders_failed_total[5m])

# Cancellation rate
rate(aibetting_executor_orders_cancelled_total[5m])
```

### Signal Rates

```promql
# Signals received per minute
rate(aibetting_executor_signals_received_total[5m]) * 60

# Signals rejected per minute
rate(aibetting_executor_signals_rejected_total[5m]) * 60

# Signal rejection ratio
rate(aibetting_executor_signals_rejected_total[5m]) / rate(aibetting_executor_signals_received_total[5m])

# Signals by strategy (per minute)
sum(rate(aibetting_executor_signals_received_total[5m]) * 60) by (strategy)
```

---

## ⚡ Latency Metrics

### Percentiles

```promql
# P50 (median) execution latency
histogram_quantile(0.50, rate(aibetting_executor_order_execution_latency_seconds_bucket[5m]))

# P95 execution latency
histogram_quantile(0.95, rate(aibetting_executor_order_execution_latency_seconds_bucket[5m]))

# P99 execution latency
histogram_quantile(0.99, rate(aibetting_executor_order_execution_latency_seconds_bucket[5m]))

# P50 Betfair API latency
histogram_quantile(0.50, rate(aibetting_executor_betfair_api_latency_seconds_bucket[5m]))

# P99 Betfair API latency
histogram_quantile(0.99, rate(aibetting_executor_betfair_api_latency_seconds_bucket[5m]))
```

### Average Latency

```promql
# Average execution latency
rate(aibetting_executor_order_execution_latency_seconds_sum[5m]) / rate(aibetting_executor_order_execution_latency_seconds_count[5m])

# Average API latency
rate(aibetting_executor_betfair_api_latency_seconds_sum[5m]) / rate(aibetting_executor_betfair_api_latency_seconds_count[5m])
```

---

## 💰 Financial Metrics

### Balance Tracking

```promql
# Balance change over 1 hour
aibetting_executor_account_balance - aibetting_executor_account_balance offset 1h

# Balance change percentage
((aibetting_executor_account_balance - aibetting_executor_account_balance offset 1h) / aibetting_executor_account_balance offset 1h) * 100

# Available balance as % of total
(aibetting_executor_available_balance / aibetting_executor_account_balance) * 100

# Exposure as % of balance
(aibetting_executor_current_exposure / aibetting_executor_account_balance) * 100
```

### Stake Metrics

```promql
# Total stake deployed
aibetting_executor_total_stake_deployed

# Stake deployment rate (per hour)
rate(aibetting_executor_total_stake_deployed[1h]) * 3600

# Average stake per order (last hour)
increase(aibetting_executor_total_stake_deployed[1h]) / increase(aibetting_executor_orders_placed_total[1h])
```

---

## 📊 Aggregations

### By Time Window

```promql
# Total orders in last 1 hour
increase(aibetting_executor_orders_placed_total[1h])

# Total orders in last 24 hours
increase(aibetting_executor_orders_placed_total[24h])

# Orders placed today (since midnight)
increase(aibetting_executor_orders_placed_total[24h] @ start())

# Peak orders per minute in last hour
max_over_time(rate(aibetting_executor_orders_placed_total[1m])[1h:1m]) * 60
```

### By Label

```promql
# Orders by side (Back vs Lay)
sum(increase(aibetting_executor_orders_placed_total[1h])) by (side)

# Signals by strategy
sum(increase(aibetting_executor_signals_received_total[1h])) by (strategy)

# Rejections by reason
sum(increase(aibetting_executor_signals_rejected_total[1h])) by (reason)

# Cancellations by reason
sum(increase(aibetting_executor_orders_cancelled_total[1h])) by (reason)

# API errors by type
sum(increase(aibetting_executor_betfair_api_errors_total[1h])) by (error_type)
```

---

## 🎯 Advanced Calculations

### Win Rate

```promql
# Order match rate (%)
(sum(increase(aibetting_executor_orders_matched_total[24h])) / sum(increase(aibetting_executor_orders_placed_total[24h]))) * 100

# Signal acceptance rate (%)
((sum(increase(aibetting_executor_signals_received_total[24h])) - sum(increase(aibetting_executor_signals_rejected_total[24h]))) / sum(increase(aibetting_executor_signals_received_total[24h]))) * 100
```

### Performance Comparison

```promql
# Current latency vs 1 hour ago
histogram_quantile(0.99, rate(aibetting_executor_order_execution_latency_seconds_bucket[5m])) / histogram_quantile(0.99, rate(aibetting_executor_order_execution_latency_seconds_bucket[5m] offset 1h))

# Order rate increase/decrease vs yesterday
rate(aibetting_executor_orders_placed_total[1h]) / rate(aibetting_executor_orders_placed_total[1h] offset 24h)
```

### Efficiency Metrics

```promql
# Orders per signal received
increase(aibetting_executor_orders_placed_total[1h]) / increase(aibetting_executor_signals_received_total[1h])

# Matched orders per active order
increase(aibetting_executor_orders_matched_total[1h]) / avg_over_time(aibetting_executor_active_orders[1h])

# API calls per order
increase(aibetting_executor_betfair_api_latency_seconds_count[1h]) / increase(aibetting_executor_orders_placed_total[1h])
```

---

## 🚨 Alert Conditions

### Thresholds

```promql
# High active orders (>50)
aibetting_executor_active_orders > 50

# Low balance (<500)
aibetting_executor_available_balance < 500

# High exposure (>450)
aibetting_executor_current_exposure > 450

# High latency (>200ms P99)
histogram_quantile(0.99, rate(aibetting_executor_order_execution_latency_seconds_bucket[5m])) > 0.2

# High failure rate (>10% of orders)
rate(aibetting_executor_orders_failed_total[5m]) / rate(aibetting_executor_orders_placed_total[5m]) > 0.1
```

### Anomaly Detection

```promql
# Latency spike (50% higher than usual)
histogram_quantile(0.99, rate(aibetting_executor_order_execution_latency_seconds_bucket[5m])) > 1.5 * histogram_quantile(0.99, rate(aibetting_executor_order_execution_latency_seconds_bucket[30m] offset 30m))

# Order rate drop (50% lower than usual)
rate(aibetting_executor_orders_placed_total[5m]) < 0.5 * rate(aibetting_executor_orders_placed_total[30m] offset 30m)

# Sudden rejection spike
deriv(aibetting_executor_signals_rejected_total[5m]) > 2
```

---

## 📈 Trending & Forecasting

### Growth Rates

```promql
# Daily growth rate of orders
(increase(aibetting_executor_orders_placed_total[24h]) - increase(aibetting_executor_orders_placed_total[24h] offset 24h)) / increase(aibetting_executor_orders_placed_total[24h] offset 24h)

# Weekly trend
avg_over_time(rate(aibetting_executor_orders_placed_total[1h])[7d:1h])
```

### Predictions (Linear Regression)

```promql
# Predict balance in 1 hour (simple linear extrapolation)
predict_linear(aibetting_executor_account_balance[30m], 3600)

# Predict exposure in next hour
predict_linear(aibetting_executor_current_exposure[30m], 3600)
```

---

## 🔍 Debugging Queries

### Service Health

```promql
# Services up/down
up{job=~"aibetting.*"}

# Scrape duration (how long Prometheus takes to scrape)
scrape_duration_seconds{job="aibetting-executor"}

# Last scrape success
scrape_samples_scraped{job="aibetting-executor"}
```

### Data Completeness

```promql
# Missing metrics (no data points in 5 min)
absent(aibetting_executor_active_orders)

# Stale metrics (no updates in 2 min)
time() - timestamp(aibetting_executor_active_orders) > 120
```

### Cardinality

```promql
# Number of unique strategies
count(count by (strategy) (aibetting_executor_signals_received_total))

# Number of unique rejection reasons
count(count by (reason) (aibetting_executor_signals_rejected_total))

# Total series for executor
count({__name__=~"aibetting_executor.*"})
```

---

## 🎨 Visualization Queries

### Heatmaps

```promql
# Latency distribution heatmap
sum(rate(aibetting_executor_order_execution_latency_seconds_bucket[5m])) by (le)
```

### Stacked Area Charts

```promql
# Balance components stacked
aibetting_executor_account_balance
aibetting_executor_available_balance
aibetting_executor_current_exposure
```

### Gauges

```promql
# Balance utilization gauge (0-100%)
(aibetting_executor_current_exposure / aibetting_executor_account_balance) * 100
```

---

## 💡 Business Intelligence Queries

### ROI Tracking

```promql
# Estimated ROI (if we had settled P&L metric)
# (total_profit / total_stake) * 100
# Note: Requires integration with Accounting service

# Order efficiency (matched/placed ratio)
(sum(increase(aibetting_executor_orders_matched_total[24h])) / sum(increase(aibetting_executor_orders_placed_total[24h]))) * 100
```

### Strategy Performance

```promql
# Signals per strategy (sorted)
topk(5, sum(increase(aibetting_executor_signals_received_total[24h])) by (strategy))

# Most rejected strategies
topk(5, sum(increase(aibetting_executor_signals_rejected_total[24h])) by (strategy))
```

### Peak Times

```promql
# Busiest hour of day
max_over_time(rate(aibetting_executor_orders_placed_total[1h])[24h:1h])

# Average orders by hour of day (requires recording rules or external processing)
```

---

## 🛠️ Operational Queries

### Capacity Planning

```promql
# Current load as % of peak
rate(aibetting_executor_orders_placed_total[5m]) / max_over_time(rate(aibetting_executor_orders_placed_total[1m])[7d:1m])

# Time until balance depleted (at current burn rate)
aibetting_executor_available_balance / rate(aibetting_executor_total_stake_deployed[1h])
```

### SLA Monitoring

```promql
# % of requests under 200ms (SLA compliance)
(sum(rate(aibetting_executor_order_execution_latency_seconds_bucket{le="0.2"}[5m])) / sum(rate(aibetting_executor_order_execution_latency_seconds_count[5m]))) * 100

# Uptime (connection status)
avg_over_time(aibetting_executor_betfair_connection_status[24h]) * 100
```

---

## 📝 Recording Rules (for Performance)

Save in `prometheus.yml` under `recording_rules:`:

```yaml
groups:
  - name: aibetting_executor_recordings
    interval: 1m
    rules:
      # Pre-compute common aggregations
      - record: aibetting:executor:orders_rate:1m
        expr: rate(aibetting_executor_orders_placed_total[1m]) * 60

      - record: aibetting:executor:latency_p99:5m
        expr: histogram_quantile(0.99, rate(aibetting_executor_order_execution_latency_seconds_bucket[5m]))

      - record: aibetting:executor:success_rate:5m
        expr: rate(aibetting_executor_orders_matched_total[5m]) / rate(aibetting_executor_orders_placed_total[5m])

      - record: aibetting:executor:exposure_pct:now
        expr: (aibetting_executor_current_exposure / aibetting_executor_account_balance) * 100
```

Then use in dashboards:
```promql
# Instead of complex query:
histogram_quantile(0.99, rate(aibetting_executor_order_execution_latency_seconds_bucket[5m]))

# Use pre-computed:
aibetting:executor:latency_p99:5m
```

---

## 🎓 Pro Tips

1. **Use `rate()` for counters**, not `increase()` in graphs (smoother)
2. **Use `irate()` for spike detection** (instant rate)
3. **Always specify time range** `[5m]` to avoid query errors
4. **Use `by (label)` for grouping**, `without (label)` to exclude
5. **Combine with `offset`** to compare time periods
6. **Use `label_replace()` to rename labels** for better readability
7. **Test queries in Prometheus UI first** before adding to Grafana

---

## 🔗 Query Examples in Grafana

### Panel Settings for Latency

```json
{
  "targets": [
    {
      "expr": "histogram_quantile(0.99, rate(aibetting_executor_order_execution_latency_seconds_bucket[5m]))",
      "legendFormat": "P99",
      "refId": "A"
    }
  ],
  "options": {
    "unit": "s",
    "decimals": 3,
    "thresholds": {
      "mode": "absolute",
      "steps": [
        {"color": "green", "value": null},
        {"color": "yellow", "value": 0.15},
        {"color": "red", "value": 0.2}
      ]
    }
  }
}
```

---

**💡 Bookmark this file! These queries cover 95% of monitoring needs for AIBetting Executor.**
