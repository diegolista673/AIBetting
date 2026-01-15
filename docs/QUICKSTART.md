# Quick Start Guide

## Prerequisites

- .NET 10 SDK
- Docker Desktop running
- Git (optional, for cloning)

## Step 1: Start Infrastructure (5 minutes)

```bash
cd docker
docker compose up -d
```

This starts:
- ✅ Prometheus (metrics)
- ✅ Grafana (dashboards)
- ✅ Redis (message bus)
- ✅ PostgreSQL (database)
- ✅ Alertmanager (alerts)
- ✅ Exporters (Redis, PostgreSQL, Node)

**Verify all containers are running:**
```bash
docker compose ps
```

All services should show "Up" status.

## Step 2: Verify Infrastructure (2 minutes)

**Test Grafana:**
```bash
# Open http://localhost:3000
# Login: admin / admin
# You should see the home page
```

**Test Prometheus:**
```bash
# Open http://localhost:9090
# Go to Status → Targets
# Infrastructure targets should be UP (prometheus, redis-exporter, postgres-exporter, node-exporter)
```

## Step 3: Configure Applications (Optional)

If using default settings, skip this step. Otherwise, update `appsettings.json` in each project:

**AIBettingExplorer/appsettings.json:**
```json
{
  "Redis": {
    "ConnectionString": "localhost:16379"
  },
  "Betfair": {
    "AppKey": "YOUR_APP_KEY",
    "SessionToken": "YOUR_TOKEN"
  }
}
```

## Step 4: Run Applications (2 minutes)

### Option A: Visual Studio (Recommended)

1. Open `AIBettingSolution.sln`
2. Right-click solution → Properties → Startup Project
3. Select "Multiple startup projects"
4. Set these to "Start":
   - AIBettingExplorer
   - AIBettingAnalyst
   - AIBettingExecutor
5. Press **F5**

### Option B: Command Line

Open 3 terminals:

**Terminal 1 - Explorer:**
```bash
cd AIBettingExplorer
dotnet run
```

**Terminal 2 - Analyst:**
```bash
cd AIBettingAnalyst
dotnet run
```

**Terminal 3 - Executor:**
```bash
cd AIBettingExecutor
dotnet run
```

## Step 5: Verify Applications Running (1 minute)

Each application should show:

**Explorer:**
```
✅ Redis connected successfully
📊 Prometheus KestrelMetricServer started on http://localhost:5001/metrics
```

**Analyst:**
```
✅ Redis connected successfully
📊 Prometheus metrics server started on http://localhost:5002/metrics
```

**Executor:**
```
✅ Redis connected
📊 Prometheus metrics server started on port 5003
```

## Step 6: Check Prometheus Targets (1 minute)

1. Go to http://localhost:9090/targets
2. Verify these targets are **UP**:
   - ✅ aibetting-explorer (host.docker.internal:5001)
   - ✅ aibetting-analyst (host.docker.internal:5002)
   - ✅ aibetting-executor (host.docker.internal:5003)
   - ✅ prometheus
   - ✅ redis-exporter
   - ✅ postgres-exporter
   - ✅ node-exporter

## Step 7: View Dashboards in Grafana (2 minutes)

1. Open http://localhost:3000
2. Login: **admin** / **admin**
3. Change password when prompted
4. Go to **Dashboards** → **Browse**
5. Open **"AIBetting Analyst - Real-time Performance"**

You should see metrics panels. If showing "No data":
- Wait 1-2 minutes for data to accumulate
- Change time range to "Last 5 minutes"
- Verify applications are running

## Step 8: Test with Mock Data (Optional)

If no real Betfair data, test with mock:

```bash
# Publish a test price update
docker exec aibetting-redis redis-cli PUBLISH "channel:price-updates" '{"marketId":{"value":"1.200000000"},"timestamp":"2026-01-15T14:30:00Z","totalMatched":50000,"runnersCount":5}'
```

Check Analyst logs - you should see:
```
Analyzing market: ...
```

## Common Issues

### ❌ Docker containers not starting

```bash
# Check Docker is running
docker --version

# Restart Docker Desktop
# Then: docker compose up -d
```

### ❌ Applications can't connect to Redis

**Check Redis is accessible:**
```bash
docker exec aibetting-redis redis-cli ping
# Should return: PONG
```

**Check port 16379:**
```powershell
Test-NetConnection -ComputerName localhost -Port 16379
# Should show: TcpTestSucceeded : True
```

### ❌ Prometheus targets DOWN

**If applications running but targets DOWN:**
```bash
docker restart aibetting-prometheus-v2
```

Wait 30 seconds, then refresh http://localhost:9090/targets

### ❌ Grafana shows "No data"

1. Verify Prometheus datasource configured:
   - Grafana → Configuration → Data Sources
   - Should see "Prometheus" with green checkmark
2. Check time range (top right) - try "Last 15 minutes"
3. Verify applications are generating metrics:
   ```bash
   curl http://localhost:5001/metrics | Select-String "aibetting"
   ```

## Next Steps

- **Read full documentation**: `docs/README.md`
- **Explore class diagrams**: `docs/diagrams/`
- **Learn PromQL queries**: `prometheus/README.md`
- **Create custom dashboards** in Grafana

## Getting Help

1. Check logs:
   - Application logs: `{Project}/logs/`
   - Docker logs: `docker compose logs -f {service}`
2. Check metrics: http://localhost:5001/metrics (Explorer), 5002 (Analyst), 5003 (Executor)
3. Check Prometheus: http://localhost:9090/graph
4. Consult documentation in `docs/`

---

**Total setup time: ~15 minutes** ⏱️

**You're ready to start automated trading!** 🎉
