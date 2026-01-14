# 📊 Grafana Dashboard - Strategy Signals Metrics

## ✅ **Metriche Implementate**

Sono state aggiunte **4 nuove metriche Prometheus** per tracciare in dettaglio i segnali delle strategie PRO:

### **1. `aibetting_analyst_signals_by_type_total`** (Counter)
Conta i segnali per strategia, tipo di segnale e livello di rischio.

**Labels:**
- `strategy`: Nome strategia (SCALPING, STEAM_MOVE, VALUE_BET, GREEN_UP)
- `signal_type`: Tipo segnale (SCALP_LONG, SCALP_SHORT, STEAM_BULLISH, ecc.)
- `risk_level`: Livello rischio (Low, Medium, High, VeryHigh)

### **2. `aibetting_analyst_last_signal_confidence`** (Gauge)
Confidence dell'ultimo segnale generato per ogni strategia (0-1).

**Labels:**
- `strategy`: Nome strategia

### **3. `aibetting_analyst_last_signal_roi`** (Gauge)
ROI atteso dell'ultimo segnale generato (percentuale).

**Labels:**
- `strategy`: Nome strategia

### **4. `aibetting_analyst_strategy_avg_confidence`** (Gauge)
Confidence media rolling per strategia.

**Labels:**
- `strategy`: Nome strategia

---

## 🎨 **Panel Grafana Suggeriti**

### **Panel 1: Signals Distribution by Strategy (Pie Chart)**

**Query PromQL:**
```promql
sum by (strategy) (
  increase(aibetting_analyst_signals_by_type_total[1h])
)
```

**Configurazione:**
- **Visualization:** Pie Chart
- **Title:** Strategy Signals Distribution (Last Hour)
- **Legend:** {{strategy}}
- **Values:** Percentage

**Aspetto:**
```
┌─────────────────────────────────┐
│ Strategy Signals (Last Hour)   │
│                                 │
│   [Pie Chart]                   │
│   SCALPING:   42%               │
│   STEAM_MOVE: 25%               │
│   VALUE_BET:  33%               │
└─────────────────────────────────┘
```

---

### **Panel 2: Signal Rate by Strategy (Time Series)**

**Query PromQL:**
```promql
rate(aibetting_analyst_signals_by_type_total[5m]) * 3600
```

**Configurazione:**
- **Visualization:** Time series
- **Title:** Signal Rate (signals/hour)
- **Legend:** {{strategy}} - {{signal_type}}
- **Y-axis:** Signals per hour

**Aspetto:**
```
┌─────────────────────────────────────────────┐
│ Signal Rate (signals/hour)                  │
│                                             │
│ [Line Chart con 3-4 linee per strategia]   │
│                                             │
└─────────────────────────────────────────────┘
```

---

### **Panel 3: Latest Signal Details (Table)**

**Query PromQL (Multi-query):**

**Query A - Confidence:**
```promql
aibetting_analyst_last_signal_confidence
```

**Query B - ROI:**
```promql
aibetting_analyst_last_signal_roi
```

**Query C - Count:**
```promql
sum by (strategy) (
  aibetting_analyst_signals_by_type_total
)
```

**Configurazione:**
- **Visualization:** Table
- **Title:** Latest Signals Summary
- **Transformations:**
  - Join by labels (strategy)
  - Organize fields

**Columns:**
1. Strategy
2. Last Confidence (%)
3. Last ROI (%)
4. Total Signals
5. Avg Confidence (%)

**Aspetto:**
```
┌──────────────────────────────────────────────────────────┐
│ Strategy    │ Last Conf │ Last ROI │ Total │ Avg Conf   │
├──────────────────────────────────────────────────────────┤
│ STEAM_MOVE  │   85%     │  4.2%    │  127  │   78%      │
│ SCALPING    │   78%     │  1.5%    │  243  │   72%      │
│ VALUE_BET   │   72%     │  7.3%    │  189  │   69%      │
└──────────────────────────────────────────────────────────┘
```

---

### **Panel 4: Signals by Risk Level (Bar Gauge)**

**Query PromQL:**
```promql
sum by (risk_level) (
  increase(aibetting_analyst_signals_by_type_total[1h])
)
```

**Configurazione:**
- **Visualization:** Bar gauge (horizontal)
- **Title:** Signals by Risk Level (Last Hour)
- **Thresholds:**
  - Low: Green
  - Medium: Yellow
  - High: Orange
  - VeryHigh: Red

**Aspetto:**
```
┌────────────────────────────────┐
│ Signals by Risk Level          │
│                                │
│ Low      ████████░░  8 (20%)   │
│ Medium   ████████████  23 (57%)│
│ High     ████░░░░  9 (23%)     │
└────────────────────────────────┘
```

---

### **Panel 5: Signal Confidence Over Time (Heatmap)**

**Query PromQL:**
```promql
aibetting_analyst_last_signal_confidence
```

**Configurazione:**
- **Visualization:** Time series with threshold coloring
- **Title:** Signal Confidence Timeline
- **Legend:** {{strategy}}
- **Thresholds:**
  - 0-0.6: Red (Low confidence)
  - 0.6-0.75: Yellow (Medium)
  - 0.75-0.85: Light Green (Good)
  - 0.85-1.0: Dark Green (Excellent)

---

### **Panel 6: Expected ROI Distribution (Histogram)**

**Query PromQL:**
```promql
histogram_quantile(0.50, 
  rate(aibetting_analyst_last_signal_roi[5m])
)
```

**Configurazione:**
- **Visualization:** Histogram
- **Title:** Expected ROI Distribution
- **X-axis:** ROI %
- **Y-axis:** Count

---

### **Panel 7: High-Confidence Signals Counter (Stat)**

**Query PromQL:**
```promql
sum(
  aibetting_analyst_signals_by_type_total
  and 
  aibetting_analyst_last_signal_confidence > 0.8
)
```

**Configurazione:**
- **Visualization:** Stat (Big Number)
- **Title:** High-Confidence Signals Today
- **Unit:** Signals
- **Thresholds:**
  - < 5: Red
  - 5-20: Yellow
  - > 20: Green

**Aspetto:**
```
┌─────────────────────────────┐
│ High-Confidence Signals     │
│                             │
│         27                  │
│                             │
│   (confidence > 80%)        │
└─────────────────────────────┘
```

---

### **Panel 8: Strategy Performance Comparison (Bar Chart)**

**Query PromQL:**
```promql
# Avg ROI per strategy
avg by (strategy) (
  aibetting_analyst_last_signal_roi
)
```

**Configurazione:**
- **Visualization:** Bar chart (vertical)
- **Title:** Average ROI by Strategy
- **Y-axis:** ROI %
- **Color:** Gradient (low to high)

---

## 📋 **Dashboard Layout Completo**

```
┌────────────────────────────────────────────────────────────┐
│  AIBetting Analyst - Strategy Signals Dashboard           │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  Row 1: Overview Stats                                    │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │
│  │ Total        │  │ High-Conf    │  │ Avg ROI      │    │
│  │ Signals      │  │ Signals      │  │ All          │    │
│  │   127        │  │    27        │  │  3.2%        │    │
│  └──────────────┘  └──────────────┘  └──────────────┘    │
│                                                            │
│  Row 2: Distribution                                      │
│  ┌────────────────────┐  ┌──────────────────────────┐    │
│  │ Signals by         │  │ Signals by Risk Level    │    │
│  │ Strategy           │  │                          │    │
│  │ [Pie Chart]        │  │ [Bar Gauge]              │    │
│  └────────────────────┘  └──────────────────────────┘    │
│                                                            │
│  Row 3: Time Series                                       │
│  ┌─────────────────────────────────────────────────────┐  │
│  │ Signal Rate Over Time (signals/hour)                │  │
│  │ [Multi-line Time Series]                            │  │
│  └─────────────────────────────────────────────────────┘  │
│                                                            │
│  Row 4: Confidence & ROI                                  │
│  ┌──────────────────────┐  ┌────────────────────────┐    │
│  │ Signal Confidence    │  │ Expected ROI           │    │
│  │ Timeline             │  │ Distribution           │    │
│  │ [Time Series]        │  │ [Histogram]            │    │
│  └──────────────────────┘  └────────────────────────┘    │
│                                                            │
│  Row 5: Latest Signals Table                              │
│  ┌─────────────────────────────────────────────────────┐  │
│  │ Latest Signals Summary                              │  │
│  │ [Table with Strategy, Confidence, ROI, Count, Avg]  │  │
│  └─────────────────────────────────────────────────────┘  │
│                                                            │
│  Row 6: Performance Comparison                            │
│  ┌─────────────────────────────────────────────────────┐  │
│  │ Average ROI by Strategy                             │  │
│  │ [Bar Chart]                                          │  │
│  └─────────────────────────────────────────────────────┘  │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

---

## 🔍 **Query Avanzate**

### **Top 3 Strategies by Signal Count (Last Hour)**

```promql
topk(3, 
  sum by (strategy) (
    increase(aibetting_analyst_signals_by_type_total[1h])
  )
)
```

### **High-ROI Signals Rate (ROI > 5%)**

```promql
count(
  aibetting_analyst_last_signal_roi > 5
) by (strategy)
```

### **Low-Risk Signals Percentage**

```promql
(
  sum(aibetting_analyst_signals_by_type_total{risk_level="Low"})
  /
  sum(aibetting_analyst_signals_by_type_total)
) * 100
```

### **Strategy Efficiency (Signals/Minute)**

```promql
rate(aibetting_analyst_signals_by_type_total[1m]) * 60
```

### **Confidence Trend (Moving Average)**

```promql
avg_over_time(
  aibetting_analyst_last_signal_confidence[10m]
)
```

---

## 🚀 **Import Dashboard JSON**

Per importare rapidamente, usa questo template JSON (da salvare come `analyst-signals-dashboard.json`):

```json
{
  "dashboard": {
    "title": "AIBetting Analyst - Strategy Signals",
    "tags": ["aibetting", "analyst", "signals"],
    "timezone": "browser",
    "panels": [
      {
        "id": 1,
        "title": "Strategy Signals Distribution",
        "type": "piechart",
        "targets": [
          {
            "expr": "sum by (strategy) (increase(aibetting_analyst_signals_by_type_total[1h]))"
          }
        ],
        "gridPos": { "x": 0, "y": 0, "w": 12, "h": 8 }
      },
      {
        "id": 2,
        "title": "Signal Rate (signals/hour)",
        "type": "timeseries",
        "targets": [
          {
            "expr": "rate(aibetting_analyst_signals_by_type_total[5m]) * 3600",
            "legendFormat": "{{strategy}} - {{signal_type}}"
          }
        ],
        "gridPos": { "x": 0, "y": 8, "w": 24, "h": 8 }
      },
      {
        "id": 3,
        "title": "Latest Signals Summary",
        "type": "table",
        "targets": [
          {
            "expr": "aibetting_analyst_last_signal_confidence",
            "format": "table"
          },
          {
            "expr": "aibetting_analyst_last_signal_roi",
            "format": "table"
          },
          {
            "expr": "aibetting_analyst_strategy_avg_confidence",
            "format": "table"
          }
        ],
        "gridPos": { "x": 0, "y": 16, "w": 24, "h": 8 }
      }
    ],
    "refresh": "5s",
    "time": {
      "from": "now-1h",
      "to": "now"
    }
  }
}
```

---

## ⚙️ **Configurazione Prometheus**

Assicurati che Prometheus scraping sia configurato correttamente:

**prometheus.yml:**
```yaml
scrape_configs:
  - job_name: 'aibetting-analyst'
    static_configs:
      - targets: ['localhost:5002']
    scrape_interval: 5s
    scrape_timeout: 5s
```

---

## 📊 **Verifica Metriche**

### **Test manuale endpoint:**

```powershell
# Verifica metriche disponibili
curl http://localhost:5002/metrics | Select-String "aibetting_analyst_signals"

# Output atteso:
# aibetting_analyst_signals_by_type_total{strategy="SCALPING",signal_type="SCALP_LONG",risk_level="Medium"} 12
# aibetting_analyst_last_signal_confidence{strategy="SCALPING"} 0.78
# aibetting_analyst_last_signal_roi{strategy="SCALPING"} 1.5
# aibetting_analyst_strategy_avg_confidence{strategy="SCALPING"} 0.75
```

### **Test query Prometheus:**

1. Apri Prometheus UI: `http://localhost:9090`
2. Prova query:
   ```promql
   aibetting_analyst_signals_by_type_total
   ```
3. Verifica labels: `strategy`, `signal_type`, `risk_level`

---

## ✅ **Checklist Setup**

- [ ] Analyst avviato con strategie PRO abilitate
- [ ] Prometheus scraping attivo su porta 5002
- [ ] Metriche visibili su `http://localhost:5002/metrics`
- [ ] Query funzionanti in Prometheus UI
- [ ] Dashboard Grafana creata con panel suggeriti
- [ ] Refresh dashboard impostato a 5-10 secondi
- [ ] Alert configurati per segnali high-confidence

---

## 🎯 **Alert Suggeriti**

### **Alert 1: Low Signal Rate**

```yaml
- alert: LowSignalRate
  expr: rate(aibetting_analyst_signals_by_type_total[5m]) * 3600 < 1
  for: 10m
  annotations:
    summary: "Low signal rate detected"
    description: "Generating less than 1 signal/hour"
```

### **Alert 2: High-Confidence Signal**

```yaml
- alert: HighConfidenceSignal
  expr: aibetting_analyst_last_signal_confidence > 0.9
  annotations:
    summary: "High confidence signal: {{ $labels.strategy }}"
    description: "Confidence: {{ $value }}%"
```

### **Alert 3: High ROI Opportunity**

```yaml
- alert: HighROIOpportunity
  expr: aibetting_analyst_last_signal_roi > 8
  annotations:
    summary: "High ROI signal: {{ $labels.strategy }}"
    description: "Expected ROI: {{ $value }}%"
```

---

## 📈 **Next Steps**

1. ✅ **Implementa dashboard base** con 3-4 panel essenziali
2. ⏭️ **Monitora per 24h** per vedere pattern
3. ⏭️ **Aggiungi alert** per opportunità critiche
4. ⏭️ **Ottimizza layout** in base a feedback
5. ⏭️ **Aggiungi annotation** per eventi importanti

---

**Creato:** 2026-01-12  
**Status:** ✅ Metriche Implementate  
**Endpoint:** `http://localhost:5002/metrics`  
**Dashboard:** Ready for Import
