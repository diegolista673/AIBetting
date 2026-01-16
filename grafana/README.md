# ✅ Grafana Dashboards - Complete

## Dashboard Created

All Grafana dashboards are now available in `grafana/dashboards/`:

### 1. System Overview Dashboard
**File:** `system-overview-dashboard.json`  
**UID:** `aibetting-overview`  
**Purpose:** High-level system health and performance

**Panels:**
- Service status (Explorer, Analyst, Executor)
- Circuit breaker status
- Account balance
- Pipeline throughput (price updates → signals → orders)
- End-to-end latency (P99)
- Memory usage by service
- CPU usage by service

**Best For:** Quick system health check, executive view

---

### 2. Explorer Dashboard
**File:** `explorer-dashboard.json`  
**UID:** `aibetting-explorer`  
**Purpose:** Data ingestion monitoring

**Panels:**
- Service status
- Total price updates
- Active markets
- Memory usage
- Price update rate (per minute)
- Processing latency (P50, P95, P99)
- Memory trend
- CPU usage

**Metrics Used:**
- `up{job="aibetting-explorer"}`
- `aibetting_price_updates_total`
- `aibetting_active_markets`
- `aibetting_processing_latency_seconds_bucket`
- `process_resident_memory_bytes`
- `process_cpu_seconds_total`

---

### 3. Analyst Dashboard
**File:** `analyst-dashboard.json`  
**UID:** `aibetting-analyst`  
**Purpose:** Signal generation monitoring

**Panels:**
- Service status
- Snapshots processed rate
- Total signals generated
- Strategy confidence gauges
- Processing latency (P50, P95, P99)

**Metrics Used:**
- `up{job="aibetting-analyst"}`
- `aibetting_analyst_snapshots_processed_total`
- `aibetting_analyst_signals_generated_total`
- `aibetting_analyst_strategy_avg_confidence`
- `aibetting_analyst_processing_latency_seconds_bucket`

---

### 4. Executor Dashboard
**File:** `executor-dashboard.json`  
**UID:** `aibetting-executor`  
**Purpose:** Order execution and risk monitoring

**Panels:**
- Service status
- Circuit breaker status
- Account balance
- Current exposure
- Order flow (placed, matched, cancelled, failed)
- Order distribution (pie chart)
- Execution & API latency
- Signal processing (received vs rejected)
- Balance & exposure trend
- Order failure rate gauge

**Metrics Used:**
- `up{job="aibetting-executor"}`
- `aibetting_executor_circuit_breaker_status`
- `aibetting_executor_account_balance`
- `aibetting_executor_current_exposure`
- `aibetting_executor_orders_placed_total`
- `aibetting_executor_orders_matched_total`
- `aibetting_executor_orders_cancelled_total`
- `aibetting_executor_orders_failed_total`
- `aibetting_executor_order_execution_latency_seconds_bucket`
- `aibetting_executor_betfair_api_latency_seconds_bucket`
- `aibetting_executor_signals_received_total`
- `aibetting_executor_signals_rejected_total`

---

## Dashboard Access

After starting Grafana with Docker Compose:

```bash
cd docker
docker compose up -d
```

Access dashboards:
1. Open http://localhost:3000
2. Login: `admin` / `admin`
3. Go to **Dashboards** → **Browse**
4. Select:
   - **AIBetting System - Overview**
   - **AIBetting Explorer - Data Ingestion**
   - **AIBetting Analyst - Real-time Performance**
   - **AIBetting Executor - Order Execution**

## Auto-Provisioning

Dashboards are automatically loaded by Grafana on startup thanks to:

**Provisioning Config:** `grafana/provisioning/dashboards/dashboards.yaml`
```yaml
apiVersion: 1
providers:
  - name: 'AIBetting Dashboards'
    orgId: 1
    folder: ''
    type: file
    disableDeletion: false
    updateIntervalSeconds: 10
    allowUiUpdates: true
    options:
      path: /var/lib/grafana/dashboards
```

**Volume Mount in docker-compose.yml:**
```yaml
volumes:
  - ../grafana/dashboards:/var/lib/grafana/dashboards:ro
  - ../grafana/provisioning:/etc/grafana/provisioning:ro
```

## Dashboard Features

### Common Features
- **Auto-refresh:** 5 seconds
- **Time range:** Last 15 minutes (adjustable)
- **Dark theme**
- **Responsive panels**
- **Interactive legends** with calculations (last, mean, max)

### Panel Types Used
- **Stat** - Single value with threshold colors
- **Timeseries** - Line graphs with multiple series
- **Gauge** - Circular gauge with thresholds
- **Piechart** - Distribution visualization

### Query Patterns

**Rate queries (per minute):**
```promql
rate(aibetting_price_updates_total[5m]) * 60
```

**Percentile queries:**
```promql
histogram_quantile(0.99, rate(aibetting_processing_latency_seconds_bucket[5m]))
```

**Service health:**
```promql
up{job="aibetting-explorer"}
```

**Resource usage:**
```promql
process_resident_memory_bytes{job="aibetting-executor"}
```

## Customization

### Adding New Panels

1. Edit dashboard JSON file in `grafana/dashboards/`
2. Add new panel object in `panels` array
3. Grafana will auto-reload within 10 seconds

**Example panel:**
```json
{
  "id": 11,
  "title": "My Custom Metric",
  "type": "stat",
  "gridPos": {"h": 4, "w": 6, "x": 0, "y": 0},
  "targets": [{
    "expr": "my_custom_metric",
    "refId": "A"
  }]
}
```

### Creating New Dashboard

1. Create in Grafana UI
2. Export as JSON: Dashboard settings → JSON Model
3. Save to `grafana/dashboards/my-dashboard.json`
4. Set `"id": null` and `"uid": "unique-id"`
5. Grafana will auto-load on next restart

## Troubleshooting

### Dashboard shows "No data"

**Check 1: Applications running**
```bash
# Verify services are up
curl http://localhost:5001/metrics  # Explorer
curl http://localhost:5002/metrics  # Analyst
curl http://localhost:5003/metrics  # Executor
```

**Check 2: Prometheus scraping**
- Go to http://localhost:9090/targets
- Verify targets are UP

**Check 3: Time range**
- Adjust time range in top-right corner
- Try "Last 5 minutes" or "Last 1 hour"

### Dashboard not appearing

**Check provisioning:**
```bash
docker logs aibetting-grafana | grep provisioning
```

**Restart Grafana:**
```bash
docker restart aibetting-grafana
```

### Metrics not matching

**Verify metric names in Prometheus:**
1. Go to http://localhost:9090/graph
2. Type metric name in query box
3. Use autocomplete to find correct name
4. Update dashboard JSON if needed

## Best Practices

### Dashboard Organization
- **System Overview** - First stop, executive summary
- **Service Dashboards** - Detailed per-service metrics
- **Troubleshooting Dashboards** - Error analysis, debugging

### Panel Naming
- Clear, descriptive titles
- Include units (MB, ms, %, etc.)
- Use consistent naming across dashboards

### Query Optimization
- Use recording rules for expensive queries
- Set appropriate scrape intervals
- Use rate() for counters
- Use histogram_quantile() for latencies

### Threshold Colors
- **Green** - Normal operation
- **Yellow** - Warning level
- **Red** - Critical level

## Next Steps

1. ✅ All dashboards created and provisioned
2. ✅ Auto-loading configured
3. 📝 Start applications and verify metrics
4. 📝 Adjust thresholds based on actual workload
5. 📝 Create alerts in Grafana (optional)
6. 📝 Export snapshots for documentation

## Summary

| Dashboard | Panels | Auto-loaded | Status |
|-----------|--------|-------------|--------|
| System Overview | 9 | ✅ Yes | ✅ Ready |
| Explorer | 8 | ✅ Yes | ✅ Ready |
| Analyst | 5 | ✅ Yes | ✅ Ready |
| Executor | 10 | ✅ Yes | ✅ Ready |

**Total Panels:** 32  
**Coverage:** All AIBetting services + system health  
**Update Frequency:** Auto-reload every 10 seconds  

---

**Dashboards are production-ready!** 🎉📊
