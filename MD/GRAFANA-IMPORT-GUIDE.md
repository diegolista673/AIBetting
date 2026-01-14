# 📊 Guida Import Dashboard Grafana - AIBetting Explorer

## ✅ Pre-requisiti Completati

- ✅ Grafana in esecuzione su http://localhost:3000
- ✅ Data Source Prometheus configurato (ID: 1, URL: http://prometheus:9090)
- ✅ Explorer in esecuzione e genera metriche
- ✅ Prometheus sta facendo scraping (target UP)

---

## 🎯 Import Dashboard in 5 Step (2 minuti)

### Step 1: Login a Grafana

1. Apri browser: **http://localhost:3000**
2. Login:
   - **Username**: `admin`
   - **Password**: `admin`
3. (Opzionale) Cambia password o clicca "Skip"

---

### Step 2: Vai alla Sezione Import

1. Clicca sull'icona **"+"** (Plus) nella sidebar sinistra
2. Oppure vai direttamente a: **http://localhost:3000/dashboard/import**
3. Oppure: **Dashboards** → **New** → **Import**

---

### Step 3: Upload Dashboard JSON

1. Clicca su **"Upload JSON file"**
2. Seleziona il file: `grafana-dashboard-explorer.json`
   - **Path completo**: `C:\Users\SMARTW\source\repos\AIBettingSolution\grafana-dashboard-explorer.json`
3. Oppure copia-incolla il contenuto del JSON nell'area di testo

---

### Step 4: Configura Dashboard

Nella schermata di configurazione:

1. **Dashboard Name**: `AIBetting Explorer - Real-time Metrics` (già precompilato)
2. **Folder**: Lascia "General" o seleziona una cartella
3. **Unique ID (UID)**: `aibetting-explorer` (già precompilato)
4. **Prometheus Data Source**: Seleziona **"Prometheus"** dal dropdown
   - Dovrebbe essere già selezionato automaticamente

---

### Step 5: Importa!

1. Clicca sul pulsante **"Import"**
2. La dashboard si aprirà automaticamente
3. Dovresti vedere **6 panels** con dati in tempo reale:

---

## 📈 Panels nella Dashboard

### Panel 1: Total Price Updates
- **Tipo**: Stat (numero grande)
- **Valore atteso**: Numero crescente (es: 250)
- **Descrizione**: Totale snapshots processati dall'avvio

### Panel 2: Price Updates Rate (per second)
- **Tipo**: Time Series (grafico lineare)
- **Valore atteso**: ~2.5 updates/sec
- **Descrizione**: Rate di updates in tempo reale

### Panel 3: Processing Latency (Percentiles)
- **Tipo**: Time Series (3 linee)
- **Valore atteso**: 
  - p50 (mediana): ~3-5ms
  - p95: ~10-15ms
  - p99: ~20-30ms
- **Descrizione**: Distribuzione latenza processing

### Panel 4: Average Processing Latency
- **Tipo**: Stat
- **Valore atteso**: ~3.5ms
- **Descrizione**: Latenza media elaborazione

### Panel 5: p95 Latency (Target < 50ms)
- **Tipo**: Stat con colori
- **Valore atteso**: < 50ms (verde)
- **Colori**:
  - Verde: < 50ms ✅
  - Giallo: 50-100ms ⚠️
  - Rosso: > 100ms ❌

### Panel 6: Total Snapshots Processed
- **Tipo**: Stat
- **Valore atteso**: Uguale a Panel 1
- **Descrizione**: Contatore totale campioni

---

## 🔍 Verifiche Post-Import

### Verifica 1: Dati Real-time

Aspetta 5-10 secondi e verifica che:
- ✅ I numeri crescano
- ✅ I grafici si aggiornino
- ✅ Il rate sia ~2.5 updates/sec

### Verifica 2: Time Range

In alto a destra, il time range dovrebbe essere:
- **From**: `now-15m` (ultimi 15 minuti)
- **To**: `now`
- **Refresh**: `5s` (auto-refresh ogni 5 secondi)

### Verifica 3: Query Panels

Clicca su un panel → **Edit** per vedere la query Prometheus:

```promql
# Panel 1 - Total Updates
aibetting_price_updates_total

# Panel 2 - Rate
rate(aibetting_price_updates_total[1m])

# Panel 3 - p95 Latency
histogram_quantile(0.95, rate(aibetting_processing_latency_seconds_bucket[1m]))
```

---

## 🎨 Personalizzazioni Opzionali

### Cambia Refresh Rate

1. Clicca sull'icona **orologio** in alto a destra
2. Seleziona refresh interval: `5s`, `10s`, `30s`, `1m`
3. Consigliato: **5s** per vedere dati in tempo reale

### Cambia Time Range

1. Clicca sull'icona **orologio**
2. Seleziona range: `Last 5 minutes`, `Last 15 minutes`, `Last 1 hour`
3. Oppure selezione custom

### Aggiungi Alert (Opzionale)

Su qualsiasi panel:
1. **Edit** → **Alert** tab
2. Configura soglia (es: p95 latency > 100ms)
3. Configura notifica (Email, Slack, etc.)

---

## 🚨 Troubleshooting

### ❌ "No data" nei panels

**Causa**: Prometheus non sta ricevendo dati

**Verifica**:
```powershell
# 1. Explorer è attivo?
# Cerca nella console: "Metrics update: 50 snapshots processed"

# 2. Prometheus sta facendo scraping?
# Apri: http://localhost:9090/targets
# Verifica: aibetting-explorer = UP (verde)

# 3. Query manuale su Prometheus
# Apri: http://localhost:9090/graph
# Query: aibetting_price_updates_total
# Clicca "Execute" → Dovresti vedere un numero
```

**Soluzione**:
- Se Explorer non è attivo: `cd AIBettingExplorer; dotnet run`
- Se Prometheus target è DOWN: Verifica `prometheus.yml` e riavvia container

---

### ❌ "Data source not found"

**Causa**: Data source Prometheus non selezionato

**Soluzione**:
1. Edit dashboard (icona ingranaggio in alto)
2. **Settings** → **Variables** → **Datasource**
3. Seleziona **Prometheus**
4. **Save dashboard**

---

### ❌ Grafici "vuoti" o "flat"

**Causa**: Time range troppo vecchio o troppo corto

**Soluzione**:
1. Cambia time range a **"Last 15 minutes"**
2. Assicurati che Explorer sia stato attivo negli ultimi 15 minuti
3. Refresh manuale: Clicca icona **circolare** in alto a destra

---

## 🎊 Dashboard Funzionante!

Quando tutto funziona, dovresti vedere:

```
┌─────────────────────────────────────────────────────┐
│ AIBetting Explorer - Real-time Metrics              │
│                                                      │
│ ┌───────────┐  ┌──────────────────────────────────┐│
│ │Total: 285 │  │ [GRAFICO CRESCENTE] ~2.5/sec    ││
│ └───────────┘  └──────────────────────────────────┘│
│                                                      │
│ ┌──────────────────────────────────────────────────┐│
│ │ Processing Latency Percentiles                  ││
│ │ [3 LINEE: p50=3ms, p95=12ms, p99=25ms]         ││
│ └──────────────────────────────────────────────────┘│
│                                                      │
│ ┌──────┐  ┌──────┐  ┌──────┐                       │
│ │ 3.5ms│  │12ms  │  │ 285  │                       │
│ │ Avg  │  │ p95  │  │Total │                       │
│ └──────┘  └──────┘  └──────┘                       │
└─────────────────────────────────────────────────────┘
```

---

## 🎯 Query Prometheus Utili per Debug

Puoi testare queste query su Prometheus UI (http://localhost:9090/graph):

```promql
# Verifica metriche disponibili
{__name__=~"aibetting.*"}

# Rate updates ultimi 5 minuti
rate(aibetting_price_updates_total[5m])

# Latenza media ultimi 10 minuti
rate(aibetting_processing_latency_seconds_sum[10m]) / 
rate(aibetting_processing_latency_seconds_count[10m])

# Distribuzione latency buckets
aibetting_processing_latency_seconds_bucket

# p99 latency
histogram_quantile(0.99, rate(aibetting_processing_latency_seconds_bucket[1m]))
```

---

## 📊 Metriche Business vs Sistema

### Metriche AIBetting (Business)
- `aibetting_price_updates_total` - Snapshots processati
- `aibetting_processing_latency_seconds` - Latenza processing
- `aibetting_startup_test` - Test metrica startup

### Metriche Sistema (.NET)
- `system_runtime_cpu_usage` - Uso CPU
- `system_runtime_gc_*` - Garbage Collection
- `system_runtime_threadpool_*` - ThreadPool

---

## 🚀 Prossimi Passi

1. ✅ **Dashboard Explorer funzionante**
2. 📊 **Crea dashboard per Analyst** (quando implementato)
3. 🎯 **Crea dashboard per Executor** (quando implementato)
4. 🔔 **Configura Alert** (email/Slack per anomalie)
5. 📈 **Dashboard P&L** (profit/loss tracking)

---

## 📚 Risorse

- Grafana Docs: https://grafana.com/docs/
- Prometheus Query Language: https://prometheus.io/docs/prometheus/latest/querying/basics/
- Dashboard Examples: https://grafana.com/grafana/dashboards/

---

**Guida creata**: 2026-01-09  
**Versione**: 1.0  
**Stack**: AIBetting Explorer + Prometheus + Grafana
