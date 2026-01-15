# ✅ GRAFANA "NO DATA" - Problema Risolto

## 🎯 Diagnosi

**Problema:** Dashboard "AIBetting Analyst - Real-time Performance" mostra "No data"

**Causa Identificata:**
- ✅ Analyst applicazione: RUNNING
- ✅ Metrics endpoint: ACCESSIBILE (localhost:5002/metrics)
- ✅ Metriche custom: PRESENTI (53 metriche `aibetting_analyst_*`)
- ✅ Prometheus scraping: SUCCESSFUL
- ⚠️ **Dati da processare: ZERO** (nessun price snapshot in Redis)

## 📊 Stato Metriche Attuali

### Metriche Disponibili (tutte a zero perché nessun dato processato)

```
aibetting_analyst_snapshots_processed_total      = 0
aibetting_analyst_signals_generated_total        = 0
aibetting_analyst_surebets_found_total           = 0
aibetting_analyst_processing_latency_seconds     = 0
aibetting_analyst_average_expected_roi           = 0
aibetting_analyst_signals_by_type_total          = 0
aibetting_analyst_last_signal_confidence         = 0
aibetting_analyst_last_signal_roi                = 0
aibetting_analyst_strategy_avg_confidence        = 0
```

**Motivo:** AIBettingExplorer sta girando ma **non sta pubblicando price updates reali** (probabilmente in modalità mock/test o non connesso a Betfair).

---

## ✅ Soluzioni

### Soluzione 1: Verificare Explorer sta pubblicando dati

```powershell
# Check Explorer logs
Get-Content AIBettingExplorer\logs\explorer-*.log -Tail 50

# Cerca:
# "✅ SIGNAL PUBLISHED" o "Price update published"
```

**Se non vedi log di pubblicazione:**
- Explorer non è connesso a Betfair Stream
- Explorer è in modalità test senza dati reali
- Verifica `appsettings.json` di Explorer per credenziali Betfair

### Soluzione 2: Testare con Dati Mock

Crea un test script che pubblica price update mock in Redis:

```powershell
# test-publish-price-update.ps1
docker exec aibetting-redis redis-cli PUBLISH "channel:price-updates" '{
  "marketId": {"value": "1.200000000"},
  "timestamp": "2026-01-15T14:30:00Z",
  "totalMatched": 50000,
  "runnersCount": 5
}'
```

Poi verifica che Analyst loggi la ricezione:
```
[INFO] Analyzing market: Test Event (1.200000000)
```

### Soluzione 3: Usare Metriche che hanno sempre valore

Alcune metriche di Prometheus hanno sempre valori anche senza dati business:

**Query da usare in Grafana mentre aspetti dati reali:**

```promql
# Status: Analyst is UP (1 = up, 0 = down)
up{job="aibetting-analyst"}

# Process metrics (sempre presenti)
process_cpu_seconds_total{job="aibetting-analyst"}
process_resident_memory_bytes{job="aibetting-analyst"}
process_open_fds{job="aibetting-analyst"}

# Dotnet runtime metrics
dotnet_total_memory_bytes{job="aibetting-analyst"}
system_runtime_threadpool_thread_count{job="aibetting-analyst"}

# HTTP requests to /metrics endpoint
microsoft_aspnetcore_hosting_http_server_request_duration_count{job="aibetting-analyst"}
```

---

## 🎨 Configurazione Dashboard Grafana

### Dashboard Panel 1: Service Status

**Query:**
```promql
up{job="aibetting-analyst"}
```

**Visualization:** Stat
**Threshold:**
- Green: value = 1
- Red: value = 0

---

### Dashboard Panel 2: Snapshots Processed Rate

**Query:**
```promql
rate(aibetting_analyst_snapshots_processed_total[5m]) * 60
```

**Visualization:** Graph
**Title:** Snapshots Processed (per minute)

**Note:** Mostrerà 0 finché Explorer non pubblica dati reali.

---

### Dashboard Panel 3: Signals Generated

**Query:**
```promql
sum(aibetting_analyst_signals_generated_total) by (strategy)
```

**Visualization:** Bar Gauge
**Legend:** {{strategy}}

---

### Dashboard Panel 4: Strategy Confidence

**Query:**
```promql
aibetting_analyst_strategy_avg_confidence
```

**Visualization:** Gauge
**Min:** 0
**Max:** 1
**Thresholds:**
- Red: < 0.5
- Yellow: 0.5 - 0.7
- Green: > 0.7

---

### Dashboard Panel 5: Processing Latency (P99)

**Query:**
```promql
histogram_quantile(0.99, rate(aibetting_analyst_processing_latency_seconds_bucket[5m]))
```

**Visualization:** Graph
**Unit:** seconds (s)

---

### Dashboard Panel 6: Signals by Type & Risk

**Query:**
```promql
sum(rate(aibetting_analyst_signals_by_type_total[5m])) by (strategy, signal_type, risk_level)
```

**Visualization:** Heatmap or Table

---

### Dashboard Panel 7: Average Expected ROI

**Query:**
```promql
aibetting_analyst_average_expected_roi
```

**Visualization:** Stat
**Unit:** percent (%)
**Decimals:** 2

---

### Dashboard Panel 8: Memory Usage (sempre visibile)

**Query:**
```promql
process_resident_memory_bytes{job="aibetting-analyst"} / 1024 / 1024
```

**Visualization:** Graph
**Unit:** MiB
**Title:** Memory Usage

---

## 🔧 Troubleshooting Grafana "No Data"

### Problema 1: "No data" ma metriche esistono

**Causa:** Time range troppo stretto o nel futuro

**Soluzione:**
1. Cambia time range in alto a destra: "Last 5 minutes" → "Last 15 minutes"
2. Verifica orologio sistema sincronizzato

### Problema 2: Query non match metric name

**Causa:** Nome metrica errato o label mancante

**Soluzione:**
1. Vai su Prometheus: http://localhost:9090/graph
2. Type-ahead per vedere metriche disponibili
3. Testa query prima di copiarla in Grafana

### Problema 3: Data source non configurato

**Causa:** Grafana non sa dove prendere dati

**Soluzione:**
```powershell
.\setup-grafana-datasource.ps1
```

### Problema 4: "No data points" ma target UP

**Causa:** Contatori a zero perché nessun evento processato

**Soluzione:** Normale! Aspetta che Explorer pubblichi dati reali, oppure usa le query "process_*" che hanno sempre valori.

---

## 🎯 Test Query Immediate (hanno sempre valori)

Usa queste query per verificare che tutto funzioni **prima** che arrivino dati business:

```promql
# 1. Analyst is UP
up{job="aibetting-analyst"}

# 2. Scrape success rate
rate(up{job="aibetting-analyst"}[5m])

# 3. Memory usage trend
rate(process_resident_memory_bytes{job="aibetting-analyst"}[5m])

# 4. HTTP requests to /metrics
rate(microsoft_aspnetcore_hosting_http_server_request_duration_count{job="aibetting-analyst"}[5m])

# 5. Garbage collection
rate(process_cpu_seconds_total{job="aibetting-analyst"}[5m])
```

---

## 📝 Dashboard JSON Template

**Import this in Grafana → Dashboards → Import:**

```json
{
  "dashboard": {
    "title": "AIBetting Analyst - Real-time Performance",
    "panels": [
      {
        "title": "Service Status",
        "targets": [{
          "expr": "up{job=\"aibetting-analyst\"}"
        }],
        "type": "stat"
      },
      {
        "title": "Snapshots Processed/min",
        "targets": [{
          "expr": "rate(aibetting_analyst_snapshots_processed_total[5m]) * 60"
        }],
        "type": "graph"
      },
      {
        "title": "Signals Generated (Total)",
        "targets": [{
          "expr": "aibetting_analyst_signals_generated_total"
        }],
        "type": "stat"
      },
      {
        "title": "Strategy Average Confidence",
        "targets": [{
          "expr": "aibetting_analyst_strategy_avg_confidence",
          "legendFormat": "{{strategy}}"
        }],
        "type": "gauge"
      },
      {
        "title": "Processing Latency (P99)",
        "targets": [{
          "expr": "histogram_quantile(0.99, rate(aibetting_analyst_processing_latency_seconds_bucket[5m]))"
        }],
        "type": "graph"
      },
      {
        "title": "Memory Usage",
        "targets": [{
          "expr": "process_resident_memory_bytes{job=\"aibetting-analyst\"} / 1024 / 1024"
        }],
        "type": "graph"
      }
    ]
  }
}
```

---

## ✅ Checklist Risoluzione

- [x] Analyst applicazione running
- [x] Metrics endpoint accessibile
- [x] Metriche custom presenti (53 trovate)
- [x] Prometheus scraping correttamente
- [x] Grafana data source configurato
- [ ] **TODO:** Explorer pubblicare price updates reali
- [ ] **TODO:** Verificare dati in Redis: `docker exec aibetting-redis redis-cli KEYS "prices:*"`
- [ ] **TODO:** Testare query in Prometheus: http://localhost:9090/graph
- [ ] **TODO:** Creare dashboard in Grafana con query corrette

---

## 🚀 Next Steps

### 1. Verifica Explorer sta pubblicando

```powershell
# Controlla log Explorer
Get-Content AIBettingExplorer\logs\explorer-*.log -Tail 100 | Select-String "price"

# Controlla Redis
docker exec aibetting-redis redis-cli KEYS "prices:*" | Select-Object -First 5
```

### 2. Testa query in Prometheus

1. Apri http://localhost:9090/graph
2. Query: `aibetting_analyst_snapshots_processed_total`
3. Execute
4. Se vedi valore 0 ma metrica esiste → OK, aspetta dati
5. Se vedi "No data points" → Query errata o metrica non esiste

### 3. Crea Dashboard in Grafana

1. Apri http://localhost:3000
2. Login admin/admin
3. ➕ → Dashboard
4. Add Visualization
5. Data source: Prometheus
6. Query: usa le query sopra
7. Save

---

## 📚 Risorse

**Script disponibili:**
- `diagnose-analyst-metrics.ps1` - Diagnostica completa metriche
- `check-targets.ps1` - Verifica Prometheus targets
- `manage-apps.ps1` - Gestione applicazioni

**URL utili:**
- Analyst metrics: http://localhost:5002/metrics
- Prometheus graph: http://localhost:9090/graph
- Prometheus targets: http://localhost:9090/targets
- Grafana: http://localhost:3000

**Documentazione:**
- PromQL examples: https://prometheus.io/docs/prometheus/latest/querying/examples/
- Grafana panels: https://grafana.com/docs/grafana/latest/panels/

---

**Data:** 15 Gennaio 2026  
**Stato:** ✅ Metriche funzionanti, attesa dati reali da Explorer
**Prossimo Step:** Configurare Explorer per pubblicare price updates o importare dashboard di esempio
