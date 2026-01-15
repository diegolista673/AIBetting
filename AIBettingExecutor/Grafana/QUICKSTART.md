# 🚀 Quick Start Guide - Grafana Monitoring Stack

## ⚡ Fast Setup (5 minutes)

### Option 1: Docker Compose (Recommended)

```bash
# Navigate to Grafana directory
cd AIBettingExecutor/Grafana

# Start all monitoring services
docker-compose up -d

# Check status
docker-compose ps

# View logs
docker-compose logs -f grafana
```

**Access Points:**
- Grafana: http://localhost:3000 (admin/admin)
- Prometheus: http://localhost:9090
- Alertmanager: http://localhost:9093

### Option 2: Manual Setup

#### 1. Start Prometheus
```bash
# Download Prometheus
wget https://github.com/prometheus/prometheus/releases/download/v2.45.0/prometheus-2.45.0.windows-amd64.zip
unzip prometheus-2.45.0.windows-amd64.zip
cd prometheus-2.45.0.windows-amd64

# Copy config
copy ..\AIBettingExecutor\Grafana\prometheus.yml .
copy ..\AIBettingExecutor\Grafana\alert-rules.yml .

# Start
prometheus.exe --config.file=prometheus.yml
```

#### 2. Start Grafana
```bash
# Download Grafana
# https://grafana.com/grafana/download

# Windows (via installer or portable)
grafana-server.exe

# Linux
sudo systemctl start grafana-server
```

#### 3. Import Dashboard
1. Open http://localhost:3000
2. Login (admin/admin)
3. Add Prometheus datasource
4. Import `executor-dashboard.json`

---

## 🎯 Verification Checklist

### ✅ Step 1: Services Running

```bash
# Check Executor metrics endpoint
curl http://localhost:5003/metrics | grep aibetting_executor

# Check Prometheus targets
curl http://localhost:9090/api/v1/targets | jq '.data.activeTargets[] | {job: .labels.job, health: .health}'

# Expected output:
# {
#   "job": "aibetting-executor",
#   "health": "up"
# }
```

### ✅ Step 2: Metrics Flowing

```bash
# Query specific metric
curl 'http://localhost:9090/api/v1/query?query=aibetting_executor_active_orders' | jq .

# Should return current value, e.g.:
# {
#   "status": "success",
#   "data": {
#     "result": [
#       {
#         "value": [1704000000, "3"]
#       }
#     ]
#   }
# }
```

### ✅ Step 3: Dashboard Visible

1. Open Grafana: http://localhost:3000
2. Navigate to **Dashboards** → **AIBetting Executor Dashboard**
3. Verify panels loading (not "No data")
4. Check connection status panel shows green

---

## 🔧 Common Issues

### ❌ Problem: "No data" in Grafana panels

**Solution:**
```bash
# 1. Check Executor is running
ps aux | grep AIBettingExecutor

# 2. Verify metrics endpoint
curl http://localhost:5003/metrics

# 3. Check Prometheus scraping
curl http://localhost:9090/api/v1/targets | jq '.data.activeTargets[] | select(.labels.job=="aibetting-executor")'

# 4. Verify time range (default: last 6h)
# Change to "Last 15 minutes" in Grafana
```

### ❌ Problem: Prometheus can't reach Executor

**Solution (prometheus.yml):**
```yaml
# Change from:
- targets: ['localhost:5003']

# To (if Executor on different machine):
- targets: ['192.168.1.100:5003']

# Or use Docker host.docker.internal:
- targets: ['host.docker.internal:5003']
```

### ❌ Problem: Dashboard import fails

**Solution:**
1. Copy raw JSON from `executor-dashboard.json`
2. In Grafana: **Dashboards** → **Import**
3. Paste JSON directly
4. Select Prometheus datasource
5. Click **Import**

### ❌ Problem: Alerts not firing

**Check Alertmanager:**
```bash
# View current alerts
curl http://localhost:9093/api/v2/alerts | jq .

# Check Alertmanager config
curl http://localhost:9093/api/v1/status | jq .
```

**Verify alert rules:**
```bash
# Test rules syntax
promtool check rules alert-rules.yml

# Reload Prometheus config
curl -X POST http://localhost:9090/-/reload
```

---

## 📊 Quick Queries (for testing)

### Prometheus Browser (http://localhost:9090)

```promql
# Check if metrics exist
aibetting_executor_active_orders

# Order rate (last 5 min)
rate(aibetting_executor_orders_placed_total[5m]) * 60

# P99 latency
histogram_quantile(0.99, rate(aibetting_executor_order_execution_latency_seconds_bucket[5m]))

# Balance history
aibetting_executor_account_balance

# Orders by side (24h)
sum(increase(aibetting_executor_orders_placed_total[24h])) by (side)
```

---

## 🎨 Customization

### Add New Panel to Dashboard

1. **Edit Dashboard**: Click ⚙️ icon → **Edit**
2. **Add Panel**: Click **Add** → **Visualization**
3. **Query**:
   ```promql
   # Example: Win rate percentage
   sum(increase(aibetting_executor_orders_matched_total[24h])) / 
   sum(increase(aibetting_executor_orders_placed_total[24h])) * 100
   ```
4. **Panel Type**: Choose (Stat, Time Series, Gauge, etc.)
5. **Title**: "Order Win Rate (24h)"
6. **Unit**: Percent (0-100)
7. **Save Dashboard**

### Create Custom Alert

1. **Edit Panel** → **Alert** tab
2. **Create Alert Rule**:
   ```
   Name: HighWinRateDrop
   Condition: WHEN avg() OF query(A, 5m) IS BELOW 50
   ```
3. **Notification**: Select channel
4. **Message**: "Win rate dropped below 50%"
5. **Save**

---

## 📱 Mobile Monitoring

### Grafana Mobile App

1. Download **Grafana Mobile** (iOS/Android)
2. Add server: `http://YOUR_IP:3000`
3. Login with credentials
4. View dashboards on phone
5. Receive push notifications for alerts

### Quick Dashboard Link

Create shareable link:
```
http://localhost:3000/d/aibetting-executor?orgId=1&refresh=10s&from=now-1h&to=now
```

Share with team for quick access.

---

## 🔔 Alert Testing

### Manually Trigger Alert

```bash
# Simulate circuit breaker trigger
redis-cli SET trading:enabled false

# Wait 30s, check Alertmanager
curl http://localhost:9093/api/v2/alerts | jq '.[] | select(.labels.alertname=="ExecutorCircuitBreakerTriggered")'

# Reset
redis-cli SET trading:enabled true
```

### Test Email Notifications

```bash
# Send test alert
curl -XPOST http://localhost:9093/api/v1/alerts -d '[
  {
    "labels": {
      "alertname": "TestAlert",
      "severity": "warning"
    },
    "annotations": {
      "summary": "This is a test alert"
    }
  }
]'

# Check sent emails in Alertmanager UI
```

---

## 📈 Performance Tips

### Reduce Query Load

1. Increase scrape interval (default: 15s)
   ```yaml
   scrape_interval: 30s  # Less frequent
   ```

2. Use recording rules for complex queries
   ```yaml
   # prometheus.yml
   recording_rules:
     - name: aibetting_recordings
       interval: 1m
       rules:
         - record: aibetting:order_rate:1m
           expr: rate(aibetting_executor_orders_placed_total[1m])
   ```

3. Limit retention (default: 30d)
   ```bash
   --storage.tsdb.retention.time=7d
   ```

### Dashboard Refresh Rate

- **Real-time monitoring**: 5-10s
- **Normal operation**: 30s
- **Historical analysis**: Manual refresh

Set in dashboard: **⚙️** → **Auto refresh** → **10s**

---

## 🗂️ Backup & Restore

### Export Dashboard

```bash
# Via API
curl -H "Authorization: Bearer YOUR_API_KEY" \
  http://localhost:3000/api/dashboards/uid/aibetting-executor \
  | jq . > backup-$(date +%Y%m%d).json

# Via UI: Dashboard → ⚙️ → JSON Model → Copy
```

### Restore Dashboard

```bash
# Via API
curl -X POST -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_API_KEY" \
  -d @backup-20260114.json \
  http://localhost:3000/api/dashboards/db

# Via UI: Dashboards → Import → Upload JSON
```

---

## 🎓 Learning Resources

- **Prometheus Query Basics**: https://prometheus.io/docs/prometheus/latest/querying/basics/
- **Grafana Dashboard Tutorial**: https://grafana.com/tutorials/
- **PromQL Cheat Sheet**: https://promlabs.com/promql-cheat-sheet/
- **Alerting Best Practices**: https://prometheus.io/docs/practices/alerting/

---

## ✅ Production Readiness Checklist

Before going live:

- [ ] All services green in Prometheus targets
- [ ] Dashboard loads with live data
- [ ] Alerts tested and notifications working
- [ ] Alertmanager email/Slack configured
- [ ] Retention period appropriate (30d+)
- [ ] Backup strategy in place
- [ ] Team trained on dashboard usage
- [ ] Mobile app configured for alerts
- [ ] Runbook created for common alerts
- [ ] Load tested with high volume

---

## 🆘 Emergency Procedures

### Dashboard Shows Red Everywhere

1. **Check Executor status**:
   ```bash
   systemctl status aibetting-executor
   # Or: ps aux | grep AIBettingExecutor
   ```

2. **View Executor logs**:
   ```bash
   tail -f AIBettingExecutor/logs/executor-*.log
   ```

3. **Check Redis**:
   ```bash
   redis-cli PING
   redis-cli GET trading:enabled
   ```

4. **Restart Executor**:
   ```bash
   # Stop
   pkill -f AIBettingExecutor
   
   # Start
   cd AIBettingExecutor
   dotnet run &
   ```

### Circuit Breaker Won't Reset

```bash
# Force reset
redis-cli SET trading:enabled true

# Clear failed orders history
redis-cli DEL executor:failed_orders

# Restart Executor
systemctl restart aibetting-executor
```

### Lost All Monitoring

```bash
# Restart monitoring stack
docker-compose down
docker-compose up -d

# Check logs
docker-compose logs -f
```

---

## 📞 Support Contacts

- **Dashboard Issues**: grafana-support@example.com
- **Alert False Positives**: alerts-team@example.com
- **Emergency**: +44-123-456-7890
- **Slack**: #aibetting-support

---

**🎉 You're ready! Dashboard is now monitoring your Executor 24/7.**
