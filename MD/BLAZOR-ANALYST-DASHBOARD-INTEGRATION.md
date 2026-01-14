# ✅ Aggiunta Dashboard Analyst al Blazor Dashboard

## 📋 **Modifiche Implementate**

### **1. Configurazione**

#### **File: `AIBettingBlazorDashboard/Configuration/MonitoringConfiguration.cs`**
- ✅ Aggiunta proprietà `AnalystMetricsUrl` per endpoint metriche Analyst (porta 5002)

#### **File: `AIBettingBlazorDashboard/appsettings.json`**
- ✅ Aggiunta configurazione dashboard `analyst`:
  ```json
  "analyst": {
    "uid": "aibetting-analyst",
    "name": "Analyst Performance",
    "description": "Real-time analysis metrics from AIBettingAnalyst"
  }
  ```
- ✅ Aggiunto `AnalystMetricsUrl: "http://localhost:5002/metrics"`

---

### **2. Nuova Pagina Analyst**

#### **File: `AIBettingBlazorDashboard/Components/Pages/AnalystMonitoring.razor`**

**Caratteristiche:**
- 📊 **Route:** `/analyst`
- 🖼️ **Embed Grafana:** Dashboard `aibetting-analyst` in modalità kiosk (TV mode)
- 🔄 **Auto-Refresh:** Configurabile (default: 5s)
- ⏱️ **Time Range:** Last 15 minutes (configurabile)
- 🎛️ **Controlli:**
  - Refresh dashboard
  - Open in Grafana (new tab)
  - Open Raw Metrics
  - Toggle Fullscreen

**Metriche Visualizzate:**
- ✅ **Snapshots Processed:** Totale snapshot di mercato analizzati
- ✅ **Surebets Found:** Opportunità di arbitraggio rilevate
- ✅ **Signals Generated:** Segnali di trading pubblicati
- ✅ **Processing Latency:** Performance analisi (p50/p95/p99)
- ✅ **Expected ROI:** ROI medio atteso

**Informazioni Aggiuntive:**
- 📊 Key Performance Indicators
- 🔗 Quick Links (Prometheus, Grafana, Raw Metrics)
- 💡 Dashboard Tips per l'uso

---

### **3. Navigazione**

#### **File: `AIBettingBlazorDashboard/Components/Layout/NavMenu.razor`**
- ✅ Aggiunta voce menu **"Analyst"**:
  ```html
  <NavLink class="nav-link" href="analyst">
      <span class="bi bi-cpu" aria-hidden="true"></span> Analyst
  </NavLink>
  ```

#### **File: `AIBettingBlazorDashboard/Components/Pages/Monitoring.razor`**
- ✅ Aggiunto link **"Analyst Dashboard"** nei Quick Links
- ✅ Aggiunto link **"Analyst Metrics (Raw)"** per accesso diretto alle metriche

---

## 🚀 **Come Usare**

### **1. Avvia lo Stack**

```powershell
# 1. Avvia container Docker (Prometheus, Grafana, Redis)
docker-compose --profile monitoring up -d

# 2. Avvia Explorer
cd AIBettingExplorer
dotnet run

# 3. Avvia Analyst
cd ../AIBettingAnalyst
dotnet run

# 4. Avvia Blazor Dashboard
cd ../AIBettingBlazorDashboard
dotnet run
```

### **2. Accedi alla Dashboard Analyst**

**Opzione A: Via Blazor Dashboard**
1. Apri: http://localhost:5000
2. Click su **"Analyst"** nel menu laterale
3. La dashboard Grafana viene embedded automaticamente

**Opzione B: Via Monitoring Page**
1. Apri: http://localhost:5000/monitoring
2. Dropdown **"Select Dashboard"** → Seleziona **"Analyst Performance"**

**Opzione C: Diretta**
- Blazor: http://localhost:5000/analyst
- Grafana: http://localhost:3000/d/aibetting-analyst

---

## 📊 **Struttura Dashboard**

### **Layout Pagina Analyst**

```
┌─────────────────────────────────────────────────────┐
│ 📊 Analyst Performance Dashboard                    │
│ [Refresh] [Open Grafana] [Raw Metrics] [Fullscreen]│
├─────────────────────────────────────────────────────┤
│ Dashboard: Analyst | Refresh: 5s | Range: 15m      │
├─────────────────────────────────────────────────────┤
│                                                     │
│         [Grafana Dashboard Embedded]               │
│         - Total Snapshots Processed                 │
│         - Surebets Found                            │
│         - Processing Latency (p95)                  │
│         - Signals Generated Rate                    │
│         - Average Expected ROI                      │
│                                                     │
├─────────────────────────────────────────────────────┤
│ 📊 Analyst Metrics | 🔗 Quick Links | 🎯 KPIs      │
└─────────────────────────────────────────────────────┘
```

---

## 🔧 **Configurazione Avanzata**

### **Personalizza Time Range**

Modifica `appsettings.json`:

```json
"Monitoring": {
  "DefaultTimeRange": "30m",  // Last 30 minutes
  "AutoRefreshInterval": "10s"  // Refresh ogni 10 secondi
}
```

### **Aggiungi Dashboard Aggiuntive**

```json
"Dashboards": {
  "analyst-detailed": {
    "uid": "aibetting-analyst-detailed",
    "name": "Analyst Detailed View",
    "description": "Extended analytics with more metrics"
  }
}
```

---

## 🎯 **URL Disponibili**

| Risorsa | URL | Descrizione |
|---------|-----|-------------|
| **Blazor Analyst Page** | http://localhost:5000/analyst | Pagina dedicata Analyst |
| **Blazor Monitoring** | http://localhost:5000/monitoring | Monitoring con selezione dashboard |
| **Grafana Analyst Dashboard** | http://localhost:3000/d/aibetting-analyst | Dashboard Grafana diretta |
| **Analyst Raw Metrics** | http://localhost:5002/metrics | Metriche Prometheus raw |
| **Prometheus Query** | http://localhost:9090/graph?g0.expr=aibetting_analyst_snapshots_processed_total | Query diretta Prometheus |

---

## ✅ **Checklist Verifica**

### **Prima di Usare**

- [ ] **Docker containers** attivi (Prometheus, Grafana, Redis)
- [ ] **Analyst service** in esecuzione (porta 5002)
- [ ] **Explorer service** in esecuzione (porta 5001) - necessario per feed dati
- [ ] **Prometheus target** `aibetting-analyst` UP (http://localhost:9090/targets)
- [ ] **Grafana dashboard** `aibetting-analyst` esistente (http://localhost:3000/dashboards)
- [ ] **Blazor Dashboard** in esecuzione (porta 5000)

### **Test Funzionalità**

- [ ] Navigazione a `/analyst` mostra dashboard embedded
- [ ] Auto-refresh funziona (dashboard si aggiorna ogni 5s)
- [ ] Bottone **"Refresh"** forza reload dashboard
- [ ] Bottone **"Open in Grafana"** apre dashboard in nuova tab
- [ ] Bottone **"Raw Metrics"** apre endpoint `/metrics`
- [ ] Bottone **"Fullscreen"** attiva modalità kiosk
- [ ] Quick Links funzionano correttamente
- [ ] Dropdown in `/monitoring` include opzione **"Analyst Performance"**

---

## 🐛 **Troubleshooting**

### **Dashboard Mostra "Loading..." Infinito**

**Causa:** Grafana non raggiungibile o dashboard non esiste

**Fix:**
```powershell
# Verifica Grafana attivo
curl http://localhost:3000

# Verifica dashboard esiste
curl http://localhost:3000/api/search | Select-String "aibetting-analyst"

# Se manca, importa dashboard
# (vedi documentazione GRAFANA-TROUBLESHOOTING.md)
```

### **Mostra "No Data" in Dashboard**

**Causa:** Analyst non sta pubblicando metriche o Prometheus non le raccoglie

**Fix:**
```powershell
# 1. Verifica Analyst attivo
Get-Process -Name "AIBettingAnalyst"

# 2. Verifica metriche disponibili
curl http://localhost:5002/metrics | Select-String "aibetting_analyst_snapshots"

# 3. Verifica Prometheus target UP
curl http://localhost:9090/api/v1/targets | ConvertFrom-Json | 
    Select -Expand data | 
    Select -Expand activeTargets | 
    Where { $_.labels.job -eq "aibetting-analyst" }

# 4. In Grafana, cambia Time Range a "Last 15 minutes"
```

### **Errore "Dashboard not found in configuration"**

**Causa:** `appsettings.json` non contiene configurazione `analyst`

**Fix:**
```json
// Verifica che appsettings.json contenga:
"Dashboards": {
  "analyst": {
    "uid": "aibetting-analyst",
    "name": "Analyst Performance",
    "description": "Real-time analysis metrics from AIBettingAnalyst"
  }
}
```

---

## 📚 **File Modificati**

```
AIBettingBlazorDashboard/
├── appsettings.json                              (MODIFICATO)
├── Configuration/
│   └── MonitoringConfiguration.cs                (MODIFICATO)
├── Components/
│   ├── Layout/
│   │   └── NavMenu.razor                         (MODIFICATO)
│   └── Pages/
│       ├── Monitoring.razor                      (MODIFICATO)
│       └── AnalystMonitoring.razor               (NUOVO)
```

---

## 🎉 **Funzionalità Implementate**

- ✅ Pagina dedicata Analyst con route `/analyst`
- ✅ Embed Grafana dashboard in modalità kiosk
- ✅ Auto-refresh configurabile
- ✅ Controlli interattivi (Refresh, Fullscreen, ecc.)
- ✅ Quick Links a Prometheus, Grafana, Raw Metrics
- ✅ Informazioni KPI e Tips d'uso
- ✅ Integrazione con menu navigazione
- ✅ Supporto selezione dashboard da Monitoring page
- ✅ Configurazione centralizzata in appsettings.json

---

**Creato:** 2026-01-12  
**Status:** ✅ Completato  
**Dashboard URL:** http://localhost:5000/analyst
