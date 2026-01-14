# 📊 AIBettingAnalyst - Documento di Riepilogo

## 🎯 **Scopo del Servizio**

**AIBettingAnalyst** è il motore di analisi intelligente del sistema AIBetting che analizza in tempo reale i 
mercati sportivi (principalmente calcio), rileva opportunità di trading profittevoli e genera segnali di trading per l'esecuzione automatica.

---

## 🏗️ **Architettura**

```
┌─────────────────┐
│  Redis (Feed)   │ ← Price Updates da Explorer
└────────┬────────┘
         │
         ▼
┌─────────────────────────────────────────┐
│        AIBettingAnalyst                 │
│  ┌───────────────────────────────────┐  │
│  │   Analisi Base                    │  │
│  │   - Surebet Detection             │  │
│  │   - WAP Calculator                │  │
│  │   - Weight of Money               │  │
│  └───────────────────────────────────┘  │
│  ┌───────────────────────────────────┐  │
│  │   Strategie PRO (4)               │  │
│  │   1. Scalping (momentum)          │  │
│  │   2. Steam Move (volume spike)    │  │
│  │   3. Green-Up (profit lock)       │  │
│  │   4. Value Bet (EV+)              │  │
│  └───────────────────────────────────┘  │
│  ┌───────────────────────────────────┐  │
│  │   Strategy Orchestrator           │  │
│  │   - Conflict Resolution           │  │
│  │   - Priority Management           │  │
│  │   - Quality Filtering             │  │
│  └───────────────────────────────────┘  │
└─────────────┬───────────────────────────┘
              │
              ▼
     ┌────────────────┐
     │  Trading       │ → Segnali per Executor
     │  Signals       │
     └────────────────┘
              │
              ▼
     ┌────────────────┐
     │  Prometheus    │ → Metriche per Grafana
     │  Metrics       │
     └────────────────┘
```

---

## 🔍 **Funzionalità Principali**

### **1. Analisi Base (Sempre Attiva)**

#### **Surebet Detection**
- Rileva opportunità di arbitraggio tra back e lay
- Calcola profitto garantito
- Genera segnali con stake ottimale
- **Output:** Segnale con ~0.5-2% profitto garantito

#### **WAP (Weighted Average Price)**
- Calcola prezzo medio ponderato su N livelli di profondità
- Analizza liquidità disponibile
- **Livelli default:** 3

#### **Weight of Money (WoM)**
- Analizza distribuzione denaro back/lay
- Identifica favoriti di mercato
- Rileva sbilanciamenti di liquidità

---

### **2. Strategie PRO (Configurabili)**

#### **🎯 Scalping Strategy**
**Obiettivo:** Trading breve termine su movimenti momentum

**Come Funziona:**
- Calcola momentum (variazione % prezzo)
- Misura velocity (momentum/tempo)
- Rileva movimenti rapidi con liquidità
- **Entry/Exit:** Stop-loss e take-profit automatici

**Parametri:**
- Min Momentum: 0.5%
- Min Velocity: 0.1%/min
- Base Stake: £50
- Validity: 30 secondi

**Output Tipico:**
```
SCALP_LONG: Arsenal vs Man City
Confidence: 78%, ROI: 1.2%, Risk: Medium
Entry: 2.50, Stop: 2.55, Target: 2.45
```

---

#### **🚀 Steam Move Strategy**
**Obiettivo:** Rileva denaro "informato" (insider trading)

**Come Funziona:**
- Monitora spike di volume (2x-5x media)
- Rileva movimenti bruschi prezzo (>2%)
- Calcola acceleration (momentum crescente)
- Analizza shift Weight of Money

**Parametri:**
- Min Volume Spike: 2.0x
- Min Price Movement: 2%
- Base Stake: £100
- Validity: 20 secondi (molto urgente)

**Output Tipico:**
```
STEAM_BULLISH: Liverpool vs Chelsea
Volume: 3.5x, Price: +4.2%, WoM Shift: 15%
Confidence: 85%, ROI: 3.8%, Risk: Medium
```

---

#### **💚 Green-Up Strategy**
**Obiettivo:** Lock-in profitto garantito con hedge

**Come Funziona:**
- Traccia movimenti favorevoli prezzo
- Calcola opportunità hedge
- Identifica profit lock-in risk-free

**Parametri:**
- Min Price Improvement: 3%
- Min Profit: 1%
- Stake: £50

**Output Tipico:**
```
GREEN_UP_OPPORTUNITY: Newcastle vs Brighton
Price improved 4.5%, Profit potential: 2.1%
Confidence: 60%, Risk: Low
```

---

#### **💎 Value Bet Strategy**
**Obiettivo:** Trova selezioni con EV+ (Expected Value positivo)

**Come Funziona:**
- Stima "true odds" con multi-factor analysis
- Calcola Expected Value (EV)
- Usa Kelly Criterion per stake sizing
- VWAP & market consensus

**Formula:**
```
EV = (TrueProb × (MarketOdds - 1)) - (1 - TrueProb)
```

**Parametri:**
- Min Value: 5%
- Min EV: 0.05 (5%)
- Kelly Fraction: 0.25 (conservative)

**Output Tipico:**
```
VALUE_BET: Man Utd vs Tottenham
Market: 3.50, True: 3.00 (16.7% value)
EV: 8.3%, Confidence: 72%
Stake: £75 (Kelly)
```

---

### **3. Strategy Orchestrator**

**Funzioni:**
- Esegue tutte le strategie in parallelo
- Filtra segnali per qualità (confidence, ROI, risk)
- Risolve conflitti tra strategie opposte
- Prioritizza top N segnali

**Conflict Resolution:**
1. **Same Action:** Prende confidence più alta
2. **Opposite Actions:** Weighted score (confidence × priority)
3. **Too Close:** Nessun trade (conflitto irrisolto)

**Output:**
```
[1] STEAM_BULLISH (priority: 95, conf: 0.85, ROI: 4.2%)
[2] SCALP_LONG (priority: 80, conf: 0.78, ROI: 1.5%)
[3] VALUE_BET (priority: 60, conf: 0.72, ROI: 7.3%)
```

---

## 📊 **Metriche Prometheus**

```
# Snapshots elaborati
aibetting_analyst_snapshots_processed_total

# Segnali generati per strategia
aibetting_analyst_signals_generated_total{strategy="scalping"}
aibetting_analyst_signals_generated_total{strategy="steam_move"}
aibetting_analyst_signals_generated_total{strategy="value_bet"}

# Surebet trovati
aibetting_analyst_surebets_found_total

# Latenza elaborazione
aibetting_analyst_processing_latency_seconds

# ROI medio
aibetting_analyst_average_expected_roi
```

**Endpoint:** `http://localhost:5002/metrics`

---

## ⚙️ **Configurazione**

### **Abilitazione Strategie**

**File:** `appsettings.json`

```json
{
  "Analyst": {
    "ProStrategies": {
      "Enabled": true,
      "Scalping": { "Enabled": true },
      "SteamMove": { "Enabled": true },
      "GreenUp": { "Enabled": false },
      "ValueBet": { "Enabled": true }
    }
  }
}
```

### **Tuning Parametri**

Ogni strategia ha ~10-17 parametri configurabili:
- Threshold (momentum, volume, value)
- Risk management (stop-loss, take-profit)
- Stake sizing
- Time windows
- Confidence levels

---

## 🔄 **Flusso Dati**

### **Input**
- **Canale Redis:** `channel:price-updates`
- **Formato:** JSON con marketId, timestamp, totale matched
- **Frequenza:** ~2 secondi per mercato

### **Processing**
1. Fetch snapshot completo da Redis
2. Aggiorna history (ultimi 15 snapshots per mercato)
3. Analisi base (Surebet, WAP, WoM)
4. Analisi PRO (4 strategie in parallelo)
5. Orchestrator (filtra, risolve conflitti, prioritizza)

### **Output**
- **Canale Redis:** `channel:strategy-signals`
- **Storage Redis:** `strategy-signals:{marketId}:{timestamp}` (TTL: 1h)
- **Metriche:** Prometheus endpoint

---

## 📈 **Performance Attese**

| Metrica | Valore |
|---------|--------|
| **Snapshots/sec** | ~2.5 (5 mercati × 0.5Hz) |
| **Segnali/ora** | 20-40 (dipende da strategie) |
| **ROI medio** | 2-5% |
| **Latency p95** | < 100ms |
| **Memory** | ~200MB |
| **CPU** | ~10-20% (1 core) |

---

## 🚀 **Come Avviare**

```powershell
# 1. Assicurati che Redis e Explorer siano attivi
docker ps | Select-String "redis"
Get-Process | Where { $_.ProcessName -like "*Explorer*" }

# 2. Avvia Analyst
cd AIBettingAnalyst
dotnet run

# Output atteso:
# 📊 Analyst Service initialized
# ✅ Scalping Strategy enabled
# ✅ Steam Move Strategy enabled
# ✅ Value Bet Strategy enabled
# 🔔 Subscribing to Redis channel: channel:price-updates
# ✅ Analyst active - monitoring price updates
```

---

## 🔗 **Integrazione con Altri Servizi**

### **← Input da:**
- **Explorer:** Feed prezzi real-time
- **Redis:** Storage snapshot e comunicazione

### **→ Output per:**
- **Executor:** Segnali trading per esecuzione
- **Accounting:** Tracking performance
- **Grafana:** Visualizzazione metriche
- **Blazor Dashboard:** Monitoring web UI

---

## 💡 **Use Cases Tipici**

### **Scenario 1: Surebet (Sempre Attivo)**
```
Arsenal vs Man City
Back Arsenal @ 2.50 (£100) → Vincita: £250
Lay Arsenal @ 2.55 (£98)  → Liability: £152
Profitto garantito: £0.80 (0.4%)
```

### **Scenario 2: Steam Move**
```
Liverpool vs Chelsea
Volume spike 4x, prezzo da 2.20 → 2.05 in 30 secondi
WoM shift: 18% verso back
→ Signal: Back Liverpool @ 2.05, ROI: 5%
```

### **Scenario 3: Value Bet**
```
Man Utd vs Tottenham
Market odds: 3.50 (28.6% implied probability)
True odds estimate: 3.00 (33.3% true probability)
Value: 16.7%, EV: 8.3%
→ Signal: Back Man Utd @ 3.50, Stake: £75 (Kelly)
```

---

## 🎯 **Best Practices**

### **Tuning Iniziale**
- Inizia con confidence alta (0.7+)
- Stake piccoli (£20-50)
- Abilita 1-2 strategie alla volta
- Monitora per 1 settimana

### **Monitoring**
- Check latency < 100ms
- Verifica ROI atteso vs reale
- Track false positives
- Analizza conflitti risolti

### **Scaling**
- Una strategia alla volta
- Aumenta stake gradualmente
- Backtest su dati storici
- Optimizza parametri

---

## 📚 **Documentazione Correlata**

- **`ANALYST-PRO-FEATURES-STATUS.md`** - Stato implementazione
- **`ANALYST-PRO-CONFIGURATION-GUIDE.md`** - Guida configurazione
- **`SONARQUBE-PLSQL-FIX.md`** - Fix code quality
- **`GRAFANA-TROUBLESHOOTING.md`** - Setup monitoring

---

## ✅ **Checklist Rapida**

**Analyst funziona correttamente se:**
- [ ] Processo attivo: `Get-Process -Name AIBettingAnalyst`
- [ ] Metriche disponibili: `curl http://localhost:5002/metrics`
- [ ] Log mostra strategie abilitate
- [ ] Snapshots incrementano: `aibetting_analyst_snapshots_processed_total`
- [ ] Segnali pubblicati su Redis: `redis-cli MONITOR | grep strategy-signals`
- [ ] Grafana dashboard mostra dati

---

## 🎉 **Riepilogo Veloce**

**In 3 punti:**
1. 📥 **Riceve** snapshot mercato da Explorer via Redis
2. 🧠 **Analizza** con 4+ strategie (Surebet, Scalping, Steam, Value)
3. 📤 **Pubblica** segnali trading ad alta confidenza per Executor

**Stack Tecnologico:**
- .NET 10 / C# 14
- Redis (Pub/Sub + Storage)
- Prometheus (Metriche)
- Serilog (Logging)

**Risultato:** Sistema di trading algoritmico intelligente che genera 20-40 segnali/ora con ROI medio 2-5% e gestione automatica del rischio.

---

**Creato:** 2026-01-12  
**Versione:** 1.0  
**Porta:** 5002  
**Status:** ✅ Produzione Ready
