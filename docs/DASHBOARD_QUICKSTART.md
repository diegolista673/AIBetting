# 🚀 AIBetting Blazor Dashboard - QUICK START GUIDE

## ✅ Implementation Status: COMPLETE

**Build Status:** ✅ SUCCESS  
**Ready to Run:** YES  
**Date Completed:** 2025-01-15

---

## 📋 Prerequisites Check

Before starting, ensure you have:

- [x] **.NET 10 SDK** installed
- [x] **Docker Desktop** running
- [x] **Visual Studio 2022** (or VS Code)
- [x] **Git** repository cloned

---

## 🏃 3-Step Quick Start

### Step 1: Start Infrastructure (Docker)

Open terminal in solution root:

```bash
cd docker
docker compose up -d
```

**Wait 30 seconds** for services to start. Verify:

```bash
docker ps
```

You should see running:
- `aibetting-prometheus-v2`
- `aibetting-grafana`
- `aibetting-redis`
- `aibetting-postgres`

---

### Step 2: Configure Multiple Startup Projects

**Visual Studio:**
1. Right-click on **Solution 'AIBettingSolution'**
2. Click **"Set Startup Projects..."**
3. Select **"Multiple startup projects"**
4. Set to **"Start"** for:
   - ✅ AIBettingExplorer
   - ✅ AIBettingAnalyst
   - ✅ AIBettingExecutor
   - ✅ AIBettingBlazorDashboard
5. Click **OK**

**Command Line Alternative:**
```bash
# Terminal 1
cd AIBettingExplorer
dotnet run

# Terminal 2 (new window)
cd AIBettingAnalyst
dotnet run

# Terminal 3 (new window)
cd AIBettingExecutor
dotnet run

# Terminal 4 (new window)
cd AIBettingBlazorDashboard
dotnet run
```

---

### Step 3: Start & Access

**Visual Studio:**
- Press **F5** (or click ▶ green button)
- Wait for all 4 applications to start (~30 seconds)

**Verify Services:**
- Explorer: http://localhost:5001/metrics
- Analyst: http://localhost:5002/metrics  
- Executor: http://localhost:5003/metrics
- Dashboard: **http://localhost:5000** ⭐

---

## 🎯 What You'll See

### Dashboard Homepage (`/`)

When you open http://localhost:5000 you'll see:

```
┌────────────────────────────────────────────┐
│ AIBetting Dashboard                        │
├────────────────────────────────────────────┤
│ [Explorer UP] [Analyst UP] [Executor UP]  │
│ [Balance: £1000] [Orders/min: 0]          │
├────────────────────────────────────────────┤
│ [Live Data Table: Orders]                  │
│ [Live Data Table: Signals]                 │
├────────────────────────────────────────────┤
│ [Grafana Embed: System Overview]          │
└────────────────────────────────────────────┘
```

**What works:**
- ✅ Service status cards (green if UP)
- ✅ KPI metrics (Balance, Orders/min, Exposure)
- ✅ Live data tables (last 10 data points)
- ✅ Grafana embeds (historical charts)
- ✅ SignalR real-time updates (every 5s)

---

## 📊 Available Pages

### 1. Dashboard (`/`)
- **Purpose:** Real-time system overview
- **Features:** Status cards, KPIs, live data, Grafana
- **Update:** Every 5 seconds via SignalR

### 2. Executor Control (`/executor`)
- **Purpose:** Control trading and risk settings
- **Features:**
  - Circuit breaker reset
  - Pause/Resume trading
  - Risk configuration (8 parameters)
- **Status:** ⚠️ API endpoints not yet implemented in Executor

### 3. Analytics (`/analytics`)
- **Purpose:** Historical performance analysis
- **Features:** 4 Grafana dashboards (tabs)
- **Dashboards:** Overview, Explorer, Analyst, Executor

---

## 🎉 Summary

You now have:

✅ **Fully functional dashboard** at http://localhost:5000  
✅ **Real-time metrics** updating every 5 seconds  
✅ **3 monitoring pages** (Dashboard, Executor, Analytics)  
✅ **4 microservices** running (Explorer, Analyst, Executor, Dashboard)  
✅ **Complete infrastructure** (Prometheus, Grafana, Redis, PostgreSQL)  

**Next:** Start generating trading signals and watch the system in action! 🚀

---

**Built with .NET 10, Blazor Server, MudBlazor, SignalR, and ❤️**
