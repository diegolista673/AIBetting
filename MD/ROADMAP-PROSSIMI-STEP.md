# 🎯 Roadmap AIBetting - Prossimi Step (No API Betfair Necessarie)

## ✅ Stato Attuale (Completato)

### Fase 1: Infrastructure ✅
- [x] AIBettingCore (models, interfaces)
- [x] AIBettingAccounting (PostgreSQL + EF Core)
- [x] AIBettingExplorer (Mock stream + Prometheus metrics)
- [x] Docker Infrastructure (Redis, PostgreSQL, Prometheus, Grafana)
- [x] Blazor Dashboard (monitoring page)
- [x] Monitoring Stack completo

---

## 🚀 Prossima Fase: AIBettingAnalyst (No API Betfair)

### Obiettivo
Implementare logica di analisi e rilevamento opportunità di trading usando **dati mock** da Explorer.

### Durata Stimata
3-5 giorni

---

## 📋 Tasks Fase 2A: Analyst Implementation

### 1. Setup Progetto (30 minuti)
- [ ] Creare `AnalystService.cs`
- [ ] Configurare Redis subscription
- [ ] Setup Serilog logging
- [ ] Prometheus metrics

### 2. Redis Integration (2 ore)
- [ ] Subscribe a `channel:price-updates`
- [ ] Parse snapshots da Redis
- [ ] Cache ultimi N snapshots per analisi trend
- [ ] Publish signals su `channel:trading-signals`

### 3. Surebet Detector (4-6 ore)
- [ ] Algoritmo rilevamento arbitrage
- [ ] Calcolo back/lay spread
- [ ] Minimum profit threshold (es: >1%)
- [ ] Validation liquidity (size disponibile)
- [ ] Signal generation con confidence score

**Esempio Surebet:**
```
Market: Arsenal vs Man City
Home Back: 2.10 (€500)
Home Lay: 2.08 (€450)

Arbitrage: Buy at 2.08, Sell at 2.10
Profit: ~0.96% (se matched entrambi)
```

### 4. WAP Calculator (2-3 ore)
- [ ] Weighted Average Price calculation
- [ ] 3-level order book depth
- [ ] Back/Lay WAP separati
- [ ] Trend detection (WAP increasing/decreasing)

**Formula WAP:**
```csharp
WAP = Σ(Price_i × Size_i) / Σ(Size_i)
```

### 5. Weight of Money (WoM) (2-3 ore)
- [ ] Calcolo distribuzione volume per runner
- [ ] Percentuale back vs lay per selection
- [ ] Identify "steam moves" (volume spikes)
- [ ] Anomaly detection

**Esempio WoM:**
```
Total Matched: €100,000
Home: €55,000 (55%) ← Favorite
Draw: €25,000 (25%)
Away: €20,000 (20%)
```

### 6. Spread Analysis (2 ore)
- [ ] Monitor Back-Lay spread per runner
- [ ] Historical spread tracking
- [ ] Tight spread = liquid market
- [ ] Wide spread = opportunity?

### 7. Prometheus Metrics (1 ora)
- [ ] `aibetting_signals_generated_total` (Counter)
- [ ] `aibetting_surebet_opportunities_found` (Counter)
- [ ] `aibetting_analyst_processing_latency` (Histogram)
- [ ] `aibetting_wap_calculation_time` (Histogram)

### 8. Grafana Dashboard Analyst (2 ore)
- [ ] Panel: Signals generated/minute
- [ ] Panel: Surebet count (24h)
- [ ] Panel: Average profit per surebet
- [ ] Panel: Processing latency p95
- [ ] Panel: Top profitable markets

---

## 📐 Architettura Analyst

```
┌─────────────────────────────────────────────┐
│ AIBettingExplorer (Mock Stream)            │
│ Genera 5 mercati × 3 runners ogni 2s       │
└────────────┬────────────────────────────────┘
             │ Publish su channel:price-updates
             ▼
┌─────────────────────────────────────────────┐
│ Redis Pub/Sub                               │
│ Key: prices:{marketId}:{timestamp}          │
└────────────┬────────────────────────────────┘
             │ Subscribe
             ▼
┌─────────────────────────────────────────────┐
│ AIBettingAnalyst (NUOVO!)                   │
│                                              │
│ 1. Receive snapshot                         │
│ 2. Calculate WAP + WoM                      │
│ 3. Detect Surebet (Back-Lay arbitrage)     │
│ 4. Validate liquidity + risk               │
│ 5. Generate signal IF profitable           │
│ 6. Publish su channel:trading-signals      │
│ 7. Update Prometheus metrics               │
└────────────┬────────────────────────────────┘
             │ Publish signal
             ▼
┌─────────────────────────────────────────────┐
│ Redis channel:trading-signals               │
│                                              │
│ Signal Format:                              │
│ {                                            │
│   "market_id": "1.200000000",               │
│   "strategy": "surebet",                    │
│   "confidence": 0.85,                       │
│   "expected_roi": 0.012,  // 1.2%          │
│   "back_selection": "Arsenal",              │
│   "back_odds": 2.10,                        │
│   "lay_odds": 2.08,                         │
│   "stake_back": 100.0,                      │
│   "stake_lay": 101.0,                       │
│   "timestamp": "2026-01-09T15:30:00Z"       │
│ }                                            │
└─────────────────────────────────────────────┘
```

---

## 🧪 Testing Strategy (Mock Mode)

### Unit Tests
- [ ] `SurebetDetector_Should_Find_Arbitrage_Opportunity()`
- [ ] `WAPCalculator_Should_Compute_Correct_Average()`
- [ ] `WoMAnalyzer_Should_Calculate_Distribution()`
- [ ] `SpreadAnalyzer_Should_Detect_Tight_Market()`

### Integration Tests
- [ ] Redis subscription funziona
- [ ] Signal generation end-to-end
- [ ] Prometheus metrics incrementano
- [ ] Performance: < 50ms processing per snapshot

### Mock Data Scenarios
```csharp
// Scenario 1: Surebet chiaro
Home Back: 2.10, Lay: 2.08 → Profit 0.96%

// Scenario 2: No arbitrage
Home Back: 2.10, Lay: 2.12 → Loss

// Scenario 3: Low liquidity
Home Back: 2.10 (€50), Lay: 2.08 (€30) → Skip (too small)

// Scenario 4: Steam move
Home Back spikes from 2.10 → 1.85 in 10s → Investigate
```

---

## 🎯 Success Criteria (Fase 2A)

| Metrica | Target | Come Verificare |
|---------|--------|----------------|
| **Signals/minute** | 1-5 | Prometheus counter |
| **Surebet detection accuracy** | >90% | Unit tests |
| **Processing latency p95** | <50ms | Prometheus histogram |
| **False positives** | <10% | Manual review signals |
| **Prometheus metrics** | 4+ metriche | /metrics endpoint |
| **Grafana dashboard** | 5+ panels | Visual check |

---

## 📊 Output Atteso (Dopo Implementazione)

### Console Logs (Analyst)
```
[INFO] AIBettingAnalyst starting
[INFO] Subscribing to Redis channel:price-updates
[INFO] Analyst ready - monitoring 5 markets
[INFO] SUREBET FOUND! Market: 1.200000000 (Arsenal vs Man City)
       Back: Arsenal @ 2.10 (€500)
       Lay: Arsenal @ 2.08 (€450)
       Profit: 0.96% | Confidence: 0.85
[INFO] Signal published to channel:trading-signals
[INFO] Metrics: 1 surebet detected, 45 snapshots processed
```

### Grafana Dashboard (Analyst)
```
┌─────────────────────────────────────┐
│ Analyst Performance - Real-time    │
├─────────────────────────────────────┤
│ Signals Generated (24h)             │
│ 127                                 │
├─────────────────────────────────────┤
│ Surebet Opportunities Found         │
│ [Grafico: 3-5 per ora]             │
├─────────────────────────────────────┤
│ Average Expected ROI                │
│ 1.2%                                │
├─────────────────────────────────────┤
│ Processing Latency p95              │
│ 38ms ✅                             │
└─────────────────────────────────────┘
```

---

## 🚫 Cosa NON Serve in Questa Fase

- ❌ **Account Betfair** (usi mock)
- ❌ **API Keys** (nessuna chiamata API)
- ❌ **Certificato .pfx** (non serve autenticazione)
- ❌ **IP Whitelisting** (nessuna connessione Betfair)
- ❌ **Capitale reale** (tutto simulato)

---

## 🎯 Quando Richiedere API Betfair

**DOPO aver completato:**
1. ✅ Analyst implementation + testing
2. ✅ Backtesting framework
3. ✅ Executor dry-run mode
4. ✅ Performance validation (ROI >3% su mock)
5. ✅ Risk manager testato

**Tempo stimato:** 2-3 settimane da ORA

---

## 📞 Next Action

**Inizia ORA con AIBettingAnalyst!**

```powershell
# Create new project structure
cd AIBettingAnalyst
dotnet new console
dotnet add package StackExchange.Redis
dotnet add package Serilog
dotnet add package prometheus-net

# Start coding!
# File: AnalystService.cs
# File: SurebetDetector.cs
# File: WAPCalculator.cs
```

**Vuoi che inizi a implementare AIBettingAnalyst?** Posso creare:
1. Struttura progetto
2. AnalystService con Redis subscription
3. SurebetDetector algorithm
4. Prometheus metrics
5. Tests

Dimmi e partiamo! 🚀

---

**Creato:** 2026-01-09  
**Fase:** 2A - Analyst Implementation  
**API Betfair Required:** ❌ NO  
**Estimated Time:** 3-5 days
