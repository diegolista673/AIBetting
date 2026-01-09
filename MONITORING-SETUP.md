# 🚀 Monitoring Setup Guide - AIBetting Explorer

Guida completa per configurare Prometheus + Grafana per monitorare `AIBettingExplorer`.

---

## 📋 Prerequisiti

- Docker Desktop installato e in esecuzione
- `AIBettingExplorer` funzionante (espone metriche su porta 5001)
- Redis attivo

---

## ⚡ Quick Start (3 Minuti)

### 1️⃣ Avvia Stack Monitoring

```powershell
# Dalla root della solution
docker-compose -f docker-compose.monitoring.yml up -d

# Verifica che i container siano attivi
docker ps | Select-String "prometheus|grafana"
```

**Output atteso:**
```
aibetting-prometheus   Up 2 seconds   0.0.0.0:9090->9090/tcp
aibetting-grafana      Up 2 seconds   0.0.0.0:3000->3000/tcp
```

### 2️⃣ Avvia AIBettingExplorer

```powershell
cd AIBettingExplorer
dotnet run
```

**Output atteso:**
```
[12:30:00 INF] 📊 Prometheus metrics exposed on http://localhost:5001/metrics
[12:30:00 INF] 🚀 AIBettingExplorer starting
[12:30:00 INF] ✅ Redis connected successfully
[12:30:01 INF] 🧪 MOCK MODE: Using simulated Betfair Stream API
[12:30:01 INF] 💡 Monitoring:
   • Prometheus metrics: http://localhost:5001/metrics
   • Prometheus UI: http://localhost:9090
   • Grafana dashboard: http://localhost:3000
```

### 3️⃣ Accedi alle Dashboard

| Servizio | URL | Credenziali |
|----------|-----|-------------|
| **Prometheus** | http://localhost:9090 | Nessuna |
| **Grafana** | http://localhost:3000 | admin / admin |
| **Metriche Raw** | http://localhost:5001/metrics | Nessuna |

---

## 📊 Verifica Configurazione

### ✅ Step 1: Verifica Metriche Raw

Apri browser: http://localhost:5001/metrics

**Dovresti vedere:**
```
# HELP aibetting_price_updates_total Total price updates processed
# TYPE aibetting_price_updates_total counter
aibetting_price_updates_total 42

# HELP aibetting_processing_latency_seconds Time to process price update
# TYPE aibetting_processing_latency_seconds histogram
aibetting_processing_latency_seconds_bucket{le="0.005"} 35
aibetting_processing_latency_seconds_bucket{le="0.01"} 40
aibetting_processing_latency_seconds_sum 0.35
aibetting_processing_latency_seconds_count 42
```

### ✅ Step 2: Verifica Prometheus Targets

1. Vai su: http://localhost:9090/targets
2. Cerca il target **aibetting-explorer**
3. Stato deve essere **UP** (verde)

**Se è DOWN:**
- Verifica che Explorer sia attivo
- Controlla che Prometheus possa raggiungere `host.docker.internal:5001`
- Su Linux, cambia target in `prometheus.yml` a `172.17.0.1:5001`

### ✅ Step 3: Query Test su Prometheus

Vai su: http://localhost:9090/graph

**Query da testare:**

```promql
# 1. Totale price updates
aibetting_price_updates_total

# 2. Rate updates/sec (ultimi 5 minuti)
rate(aibetting_price_updates_total[5m])

# 3. Latenza media
rate(aibetting_processing_latency_seconds_sum[1m]) / rate(aibetting_processing_latency_seconds_count[1m])

# 4. p95 latency (target < 50ms)
histogram_quantile(0.95, rate(aibetting_processing_latency_seconds_bucket[1m]))
```

### ✅ Step 4: Configura Grafana Data Source

1. Vai su: http://localhost:3000
2. Login: `admin` / `admin` (cambierà password al primo accesso)
3. Vai su **Configuration** (⚙️) → **Data Sources**
4. Clicca **Add data source**
5. Seleziona **Prometheus**
6. Configura:
   - **Name**: `Prometheus`
   - **URL**: `http://prometheus:9090`
   - Clicca **Save & Test**

**Output atteso:** ✅ "Data source is working"

### ✅ Step 5: Importa Dashboard

**Opzione A - Import JSON:**
1. Su Grafana: **Dashboards** → **Import**
2. Clicca **Upload JSON file**
3. Seleziona: `grafana-dashboard-explorer.json`
4. Seleziona data source: **Prometheus**
5. Clicca **Import**

**Opzione B - Manuale:**
1. **Dashboards** → **New Dashboard**
2. **Add Panel** → **Add a new panel**
3. Query: `aibetting_price_updates_total`
4. Visualization: **Stat**
5. Title: "Total Price Updates"
6. **Save dashboard**

---

## 📈 Dashboard Panels

La dashboard `AIBetting Explorer - Real-time Metrics` include:

### Panel 1: Total Price Updates
- **Tipo**: Stat (numero grande)
- **Query**: `aibetting_price_updates_total`
- **Descrizione**: Numero totale snapshot processati

### Panel 2: Price Updates Rate
- **Tipo**: Time Series (grafico)
- **Query**: `rate(aibetting_price_updates_total[1m])`
- **Descrizione**: Updates al secondo (dovrebbe essere ~2.5/sec per 5 mercati)

### Panel 3: Processing Latency (Percentiles)
- **Tipo**: Time Series (grafico)
- **Query**:
  - p50: `histogram_quantile(0.50, rate(aibetting_processing_latency_seconds_bucket[1m]))`
  - p95: `histogram_quantile(0.95, rate(aibetting_processing_latency_seconds_bucket[1m]))`
  - p99: `histogram_quantile(0.99, rate(aibetting_processing_latency_seconds_bucket[1m]))`
- **Threshold**: 
  - Verde: < 50ms
  - Giallo: 50-100ms
  - Rosso: > 100ms

### Panel 4: Average Processing Latency
- **Tipo**: Stat
- **Query**: `rate(aibetting_processing_latency_seconds_sum[1m]) / rate(aibetting_processing_latency_seconds_count[1m])`

### Panel 5: p95 Latency
- **Tipo**: Stat
- **Query**: `histogram_quantile(0.95, rate(aibetting_processing_latency_seconds_bucket[1m]))`
- **Target**: < 50ms

### Panel 6: Total Snapshots Processed
- **Tipo**: Stat
- **Query**: `aibetting_processing_latency_seconds_count`

---

## 🎯 Valori Attesi (Mock Mode)

| Metrica | Valore Atteso | Note |
|---------|---------------|------|
| **Updates Rate** | ~2.5/sec | 5 mercati × 3 runners ÷ 2 sec = 7.5 snapshots/2s |
| **p50 Latency** | < 10ms | Redis locale molto veloce |
| **p95 Latency** | < 50ms | Target architetturale |
| **p99 Latency** | < 100ms | Spike occasionali accettabili |
| **Total Updates** | Crescente | Incrementa linearmente |

---

## 🔧 Troubleshooting

### ❌ Prometheus Target DOWN

**Sintomo:** Target `aibetting-explorer` mostra stato **DOWN** su http://localhost:9090/targets

**Diagnosi:**
```powershell
# Verifica che Explorer esponga metriche
curl http://localhost:5001/metrics
```

**Soluzioni:**

1. **Windows/Mac**: `prometheus.yml` deve usare `host.docker.internal:5001`
2. **Linux**: Cambia a `172.17.0.1:5001` (IP del bridge Docker)
3. **Firewall**: Verifica che porta 5001 sia accessibile

**Fix Linux:**
```yaml
# In prometheus.yml
- targets: ['172.17.0.1:5001']
```

Poi riavvia Prometheus:
```powershell
docker-compose -f docker-compose.monitoring.yml restart prometheus
```

### ❌ Grafana: "No data"

**Cause comuni:**
1. Data source Prometheus non configurato
2. Query Prometheus errata
3. Time range troppo vecchio
4. Explorer non sta generando dati

**Soluzioni:**
```powershell
# 1. Verifica data source
# Grafana → Configuration → Data Sources → Prometheus → Test

# 2. Verifica query su Prometheus
# http://localhost:9090/graph → Query: aibetting_price_updates_total

# 3. Cambia time range su Grafana: "Last 15 minutes"

# 4. Verifica log Explorer
cd AIBettingExplorer
tail -f logs\explorer-*.log
# Deve stampare: [INF] Publish price 1.200000000 @ ...
```

### ❌ Metriche non si aggiornano

**Diagnosi:**
```powershell
# Log Explorer real-time
cd AIBettingExplorer
Get-Content logs\explorer-*.log -Wait | Select-String "Publish price"
```

**Dovrebbe stampare ogni 2 secondi:**
```
[12:35:03 INF] Publish price 1.200000000 @ "2026-01-09T11:35:03Z"
[12:35:05 INF] Publish price 1.200000001 @ "2026-01-09T11:35:05Z"
```

**Se non stampa nulla:**
- Redis non è raggiungibile
- Controlla password in `appsettings.json`
- Verifica `docker ps | Select-String redis`

### ❌ Container non si avvia

**Diagnosi:**
```powershell
# Log Prometheus
docker logs aibetting-prometheus

# Log Grafana
docker logs aibetting-grafana
```

**Fix comuni:**
```powershell
# Riavvia stack
docker-compose -f docker-compose.monitoring.yml down
docker-compose -f docker-compose.monitoring.yml up -d

# Verifica porte libere
netstat -an | Select-String "9090|3000"
```

---

## 🧹 Cleanup

```powershell
# Ferma monitoring stack
docker-compose -f docker-compose.monitoring.yml down

# Rimuovi anche volumi (ATTENZIONE: cancella dati storici)
docker-compose -f docker-compose.monitoring.yml down -v

# Rimuovi container singoli
docker stop aibetting-prometheus aibetting-grafana
docker rm aibetting-prometheus aibetting-grafana
```

---

## 🎓 Query Prometheus Utili

### Throughput
```promql
# Updates totali
aibetting_price_updates_total

# Rate ultimi 5 minuti
rate(aibetting_price_updates_total[5m])

# Increase ultimi 10 minuti
increase(aibetting_price_updates_total[10m])
```

### Latenza
```promql
# Latenza media
rate(aibetting_processing_latency_seconds_sum[1m]) / rate(aibetting_processing_latency_seconds_count[1m])

# p50, p95, p99
histogram_quantile(0.50, rate(aibetting_processing_latency_seconds_bucket[1m]))
histogram_quantile(0.95, rate(aibetting_processing_latency_seconds_bucket[1m]))
histogram_quantile(0.99, rate(aibetting_processing_latency_seconds_bucket[1m]))

# Max latency
max_over_time(aibetting_processing_latency_seconds_bucket[5m])
```

### Alerting (per future estensioni)
```promql
# Alert se latenza p95 > 100ms per 5 minuti
histogram_quantile(0.95, rate(aibetting_processing_latency_seconds_bucket[5m])) > 0.1

# Alert se rate < 1 update/sec (servizio down?)
rate(aibetting_price_updates_total[1m]) < 1
```

---

## 🚀 Prossimi Passi

1. ✅ **Monitoring funzionante** - Explorer metriche visibili su Grafana
2. 📊 **Aggiungi Analyst** - Quando implementato, esponi metriche su porta 5002
3. 🎯 **Aggiungi Executor** - Metriche ordini su porta 5003
4. 🔔 **Alert Grafana** - Notifiche email/Slack per anomalie
5. 📈 **Dashboard P&L** - Visualizza profitti da PostgreSQL

---

## 📚 Documentazione

- Prometheus: https://prometheus.io/docs/
- Grafana: https://grafana.com/docs/grafana/latest/
- prometheus-net: https://github.com/prometheus-net/prometheus-net

---

**Versione**: 1.0  
**Data**: 2026-01-09  
**Autore**: AIBetting Team
