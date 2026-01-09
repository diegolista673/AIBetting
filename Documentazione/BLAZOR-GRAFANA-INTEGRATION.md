# 📊 Grafana Dashboard Integration - Blazor Guide

## ✅ Implementazione Completata

### File Creati

#### 1. **Monitoring.razor** - Pagina principale
- Path: `AIBettingBlazorDashboard\Components\Pages\Monitoring.razor`
- Features:
  - Embed Grafana dashboard con iframe
  - Selezione dashboard da dropdown
  - Refresh button
  - Fullscreen toggle
  - Link esterni a Grafana/Prometheus
  - Auto-refresh configurabile

#### 2. **monitoring.css** - Stili
- Path: `AIBettingBlazorDashboard\wwwroot\css\monitoring.css`
- Features:
  - Responsive design (desktop + mobile)
  - Fullscreen mode
  - Dark mode support
  - Loading states
  - Info cards

#### 3. **MonitoringConfiguration.cs** - Configurazione
- Path: `AIBettingBlazorDashboard\Configuration\MonitoringConfiguration.cs`
- Modello di configurazione per Grafana/Prometheus URLs

#### 4. **appsettings.json** - Settings
- Aggiunta sezione `Monitoring` con:
  - URL Grafana/Prometheus
  - Dashboard UIDs
  - Auto-refresh interval
  - Time range default

#### 5. **NavMenu.razor** - Aggiornato
- Aggiunto link "Monitoring" con icona
- Riordinato menu per priorità

#### 6. **Program.cs** - Aggiornato
- Registrato `MonitoringConfiguration` in DI

#### 7. **App.razor** - Aggiornato
- Incluso `monitoring.css` nel layout

---

## 🚀 Come Usare

### Prerequisiti

1. **Grafana deve essere in esecuzione:**
```powershell
docker ps | Select-String grafana
# Deve mostrare: aibetting_grafana: Up
```

2. **Dashboard deve essere importata in Grafana:**
- Apri http://localhost:3000
- Import `grafana-dashboard-explorer.json`
- UID dashboard: `aibetting-explorer`

3. **Blazor Dashboard deve essere avviato:**
```powershell
cd AIBettingBlazorDashboard
dotnet run
```

### Accesso

1. Apri browser: **http://localhost:5000** (o porta configurata)
2. Clicca su **"Monitoring"** nel menu laterale
3. Seleziona dashboard dal dropdown
4. La dashboard Grafana appare embedded nella pagina

---

## 📋 Configurazione

### Personalizza URLs (appsettings.json)

```json
{
  "Monitoring": {
    "GrafanaBaseUrl": "http://localhost:3000",
    "PrometheusBaseUrl": "http://localhost:9090",
    "ExplorerMetricsUrl": "http://localhost:5001/metrics",
    "DefaultDashboard": "explorer",
    "AutoRefreshInterval": "5s",
    "DefaultTimeRange": "15m"
  }
}
```

### Aggiungi Nuove Dashboard

In `appsettings.json`, sezione `Dashboards`:

```json
"Dashboards": {
  "mia-dashboard": {
    "uid": "my-dashboard-uid",
    "name": "My Custom Dashboard",
    "description": "Description here"
  }
}
```

**⚠️ IMPORTANTE:** L'UID deve corrispondere a quello in Grafana!

---

## 🎨 Features Implementate

### 1. Dashboard Selector
- Dropdown con lista dashboard disponibili
- Descrizione dashboard visualizzata
- Switch istantaneo tra dashboard

### 2. Controlli
- **Refresh**: Ricarica dashboard con cache bypass
- **Open in Grafana**: Apre dashboard in tab separata (full UI)
- **Fullscreen**: Modalità fullscreen (Esc per uscire)

### 3. Info Cards
- **Real-time Metrics**: Descrizione metriche visualizzate
- **Quick Links**: Link diretti a Grafana, Prometheus, Metrics raw
- **Dashboard Settings**: Auto-refresh e time range configurati

### 4. Error Handling
- Alert se Grafana non è raggiungibile
- Loading spinner durante caricamento
- Link diretto a Grafana per troubleshooting

### 5. Responsive Design
- Layout adattivo desktop/tablet/mobile
- Menu collapsible su mobile
- Iframe ridimensionabile

---

## 🔧 Troubleshooting

### ❌ "Dashboard Error" - Grafana non raggiungibile

**Causa:** Grafana container non attivo

**Soluzione:**
```powershell
docker-compose --profile monitoring up -d grafana
```

### ❌ Dashboard vuota o errore 404

**Causa:** Dashboard non importata o UID errato

**Soluzione:**
1. Verifica UID in Grafana:
   - Apri http://localhost:3000
   - Vai sulla dashboard
   - URL sarà `/d/{UID}/...`
2. Aggiorna `appsettings.json` con UID corretto
3. Riavvia Blazor app

### ❌ Dashboard non si aggiorna

**Causa:** Auto-refresh disabilitato o Prometheus non riceve dati

**Soluzione:**
1. Verifica Prometheus target UP:
   ```powershell
   start http://localhost:9090/targets
   ```
2. Verifica Explorer sta generando metriche:
   ```powershell
   curl http://localhost:5001/metrics | Select-String "aibetting"
   ```
3. Clicca "Refresh" button nella pagina Monitoring

### ❌ Iframe bloccato (X-Frame-Options)

**Causa:** Grafana potrebbe bloccare embedding

**Soluzione:**
Modifica `grafana.ini` (in Docker volume):
```ini
[security]
allow_embedding = true
```

Oppure usa Grafana config in `docker-compose.yml`:
```yaml
environment:
  - GF_SECURITY_ALLOW_EMBEDDING=true
```

---

## 🎯 Modalità Kiosk

La pagina usa **Kiosk TV mode** di Grafana per nascondere:
- ✅ Top navigation bar
- ✅ Side menu
- ✅ Time picker
- ✅ Zoom controls

**Parametri URL usati:**
```
?orgId=1&kiosk=tv&refresh=5s&from=now-15m&to=now
```

Per disabilitare kiosk mode, rimuovi `&kiosk=tv` dall'URL embed.

---

## 📊 Dashboard Disponibili (Default)

### 1. Explorer Metrics
- **UID**: `aibetting-explorer`
- **Metriche**: Price updates, Processing latency (p50/p95/p99)
- **Refresh**: 5s
- **Range**: Last 15 minutes

### 2. Infrastructure Overview
- **UID**: `infrastructure-overview`
- **Metriche**: Redis + PostgreSQL status
- **Refresh**: 15s
- **Range**: Last 30 minutes

### 3. Redis Metrics
- **UID**: `redis-metrics`
- **Metriche**: Memory, Commands/sec, Connections
- **Refresh**: 10s

### 4. PostgreSQL Metrics
- **UID**: `postgres-metrics`
- **Metriche**: Queries, Connections, Transactions
- **Refresh**: 10s

---

## 🔐 Autenticazione Grafana

### Opzione 1: Anonymous Access (Default - per questa implementazione)

Grafana deve permettere accesso anonimo per iframe embedding:

```yaml
# docker-compose.yml
environment:
  - GF_AUTH_ANONYMOUS_ENABLED=true
  - GF_AUTH_ANONYMOUS_ORG_ROLE=Viewer
  - GF_SECURITY_ALLOW_EMBEDDING=true
```

### Opzione 2: Service Account Token (Produzione)

Per produzione, usa Service Account Token:

1. Crea Service Account in Grafana
2. Genera token con ruolo `Viewer`
3. Aggiungi token all'header HTTP:

```csharp
// In Monitoring.razor
<iframe src="@embedUrl" 
        id="grafana-iframe"
        data-auth-token="@grafanaToken">
</iframe>

@code {
    private string grafanaToken = "YOUR_SERVICE_ACCOUNT_TOKEN";
}
```

---

## 🚀 Prossimi Miglioramenti (Opzionali)

### 1. Live Data Refresh
- SignalR per aggiornamenti real-time senza iframe reload
- Websocket connection a Grafana API

### 2. Multi-Dashboard View
- Grid layout con 2-4 dashboard contemporanee
- Split screen

### 3. Custom Panels
- Fetch dati da Prometheus API direttamente
- Render charts con Chart.js/Plotly
- Più flessibilità del layout

### 4. Dashboard Sharing
- Generate share link con snapshot
- Export dashboard as PDF

### 5. Alert Integration
- Visualizza Grafana alerts nella Blazor UI
- Notifiche real-time per anomalie

---

## 📁 File Structure

```
AIBettingBlazorDashboard/
├── Components/
│   ├── Pages/
│   │   └── Monitoring.razor ✅ NUOVO
│   └── Layout/
│       └── NavMenu.razor ✅ AGGIORNATO
├── Configuration/
│   └── MonitoringConfiguration.cs ✅ NUOVO
├── wwwroot/
│   └── css/
│       └── monitoring.css ✅ NUOVO
├── appsettings.json ✅ AGGIORNATO
└── Program.cs ✅ AGGIORNATO
```

---

## 🎊 Risultato Finale

Quando tutto è configurato correttamente, la pagina Monitoring mostra:

```
┌─────────────────────────────────────────────────┐
│ 📊 System Monitoring              [Buttons]     │
├─────────────────────────────────────────────────┤
│ Select Dashboard: [Explorer Metrics ▼]          │
├─────────────────────────────────────────────────┤
│                                                  │
│  [GRAFANA DASHBOARD EMBEDDED]                   │
│  - Total Price Updates: 285                     │
│  - Rate: 2.5/sec                                │
│  - Latency p95: 12ms                            │
│  - ...grafici real-time...                      │
│                                                  │
├─────────────────────────────────────────────────┤
│ Real-time Metrics | Quick Links | Settings      │
└─────────────────────────────────────────────────┘
```

---

## ✅ Checklist Deployment

- [ ] Grafana container attivo
- [ ] Dashboard importata in Grafana con UID corretto
- [ ] `appsettings.json` configurato con URL corretti
- [ ] `GF_SECURITY_ALLOW_EMBEDDING=true` in Grafana
- [ ] Blazor app compilata senza errori
- [ ] Pagina Monitoring accessibile da menu
- [ ] Dashboard visibile nell'iframe
- [ ] Refresh button funzionante
- [ ] Fullscreen mode funzionante
- [ ] Links esterni funzionanti

---

**Creato**: 2026-01-09  
**Versione**: 1.0  
**Stack**: Blazor Server + Grafana + Prometheus
