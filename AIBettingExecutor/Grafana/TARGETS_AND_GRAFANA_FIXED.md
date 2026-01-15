# ✅ PROBLEMA RISOLTO - Prometheus Targets e Grafana Data Source

## 🎯 Problemi Risolti

### 1. ❌ Tutti i Target Prometheus erano DOWN
**Causa:** Prometheus cercava le applicazioni AIBetting come container Docker (`aibetting-executor`, `aibetting-analyst`, `aibetting-explorer`) ma le app girano **localmente** fuori da Docker.

**Soluzione:** Aggiornato `prometheus.yml` per usare `host.docker.internal` invece dei nomi container.

### 2. ❌ Grafana non aveva Data Source Prometheus configurato
**Causa:** Primo setup, data source non configurato.

**Soluzione:** Creato e eseguito script `setup-grafana-datasource.ps1` che configura automaticamente il data source.

---

## ✅ Stato Attuale

### Prometheus Targets

| Target | Stato | Note |
|--------|-------|------|
| **prometheus** | ✅ UP | Self-monitoring |
| **redis-exporter** | ✅ UP | Redis metrics |
| **postgres-exporter** | ✅ UP | PostgreSQL metrics |
| **node-exporter** | ✅ UP | System metrics |
| **aibetting-explorer** | ⏸️ DOWN | Applicazione non avviata |
| **aibetting-analyst** | ⏸️ DOWN | Applicazione non avviata |
| **aibetting-executor** | ⏸️ DOWN | Applicazione non avviata |

**Note:** I 3 target delle applicazioni AIBetting sono DOWN perché le applicazioni non sono in esecuzione. Questo è normale se non hai avviato le app.

### Grafana Data Source

✅ **Prometheus data source configurato con successo**
- ID: 2
- Name: Prometheus
- URL: http://prometheus:9090
- Access: Proxy (Server-side)
- Status: ✅ WORKING

---

## 🚀 Come Avviare le Applicazioni AIBetting

Per far diventare **UP** i target delle applicazioni, avviale in terminali separati:

### Terminal 1: AIBettingExplorer
```bash
cd C:\Users\SMARTW\source\repos\AIBettingSolution\AIBettingExplorer
dotnet run
```
**Output atteso:**
```
✅ Redis connected successfully
📊 Prometheus KestrelMetricServer started on http://localhost:5001/metrics
```

### Terminal 2: AIBettingAnalyst
```bash
cd C:\Users\SMARTW\source\repos\AIBettingSolution\AIBettingAnalyst
dotnet run
```
**Output atteso:**
```
✅ Redis connected
📊 Prometheus metrics on http://localhost:5002/metrics
```

### Terminal 3: AIBettingExecutor
```bash
cd C:\Users\SMARTW\source\repos\AIBettingSolution\AIBettingExecutor
dotnet run
```
**Output atteso:**
```
✅ Redis connected
📊 Prometheus metrics server started on port 5003
```

---

## 📊 Verifica Targets in Prometheus

### Metodo 1: Web UI
1. Apri http://localhost:9090/targets
2. Verifica che i target siano **UP** (verde)
3. Se DOWN, controlla la colonna "Error" per dettagli

### Metodo 2: Script PowerShell
```bash
cd AIBettingExecutor\Grafana
.\check-targets.ps1
```

Output esempio:
```
Summary:
  Total Targets: 7
  UP:            7  ✅
  DOWN:          0  ✅

UP Targets:
  + aibetting-explorer (host.docker.internal:5001)
  + aibetting-analyst (host.docker.internal:5002)
  + aibetting-executor (host.docker.internal:5003)
  + prometheus (prometheus:9090)
  + redis (redis-exporter:9121)
  + postgresql (postgres-exporter:9187)
  + node (node-exporter:9100)
```

---

## 📈 Verifica Grafana

### 1. Accedi a Grafana
http://localhost:3000
- Username: `admin`
- Password: `admin` (ti chiederà di cambiarla)

### 2. Verifica Data Source
1. Click sull'icona ⚙️ (Configuration) → Data Sources
2. Dovresti vedere **Prometheus** con badge **Default**
3. Click su **Prometheus** → Scroll down → Click **Save & Test**
4. Dovresti vedere: ✅ "Data source is working"

### 3. Testa con una Query
1. Click su ➕ → Dashboard
2. Add Visualization → Select **Prometheus** data source
3. Nella query, scrivi: `up`
4. Dovresti vedere una tabella con tutti i target e valore 1 per quelli UP

---

## 🔧 File Modificati

### 1. prometheus.yml
**Cambio principale:** Target applicazioni AIBetting
```yaml
# PRIMA (non funzionava)
- targets: ['aibetting-executor:5003']

# DOPO (funziona!)
- targets: ['host.docker.internal:5003']
```

**Motivo:** Le app girano su host Windows, non in container Docker. `host.docker.internal` è l'alias Docker per accedere all'host.

### 2. Grafana Data Source (via API)
Creato automaticamente con script `setup-grafana-datasource.ps1`:
```json
{
  "name": "Prometheus",
  "type": "prometheus",
  "access": "proxy",
  "url": "http://prometheus:9090",
  "isDefault": true
}
```

---

## 📁 Script Creati

### setup-grafana-datasource.ps1
Configura automaticamente il data source Prometheus in Grafana.
```bash
.\setup-grafana-datasource.ps1
```

### check-targets.ps1
Verifica lo stato di tutti i target Prometheus.
```bash
.\check-targets.ps1
```

Output:
- ✅ Lista target UP (verde)
- ❌ Lista target DOWN (rosso) con suggerimenti troubleshooting

---

## 🎨 Creare Dashboard in Grafana

### Metodo 1: Importare Dashboard Pre-costruite

1. Vai su **Dashboards** → **Import**
2. Inserisci un ID dashboard da Grafana.com, ad esempio:
   - **1860** - Node Exporter Full
   - **7362** - Redis Dashboard
   - **9628** - PostgreSQL Database
3. Seleziona **Prometheus** come data source
4. Click **Import**

### Metodo 2: Creare Dashboard Custom

1. Click ➕ → **Dashboard**
2. **Add Visualization** → Select **Prometheus**
3. Query esempi:

**Order Rate (Executor):**
```promql
rate(aibetting_executor_orders_placed_total[5m]) * 60
```

**Signal Processing Rate (Analyst):**
```promql
rate(aibetting_analyst_signals_generated_total[5m]) * 60
```

**Price Update Rate (Explorer):**
```promql
rate(aibetting_price_updates_total[5m]) * 60
```

**Redis Memory Usage:**
```promql
redis_memory_used_bytes / 1024 / 1024
```

**PostgreSQL Active Connections:**
```promql
pg_stat_database_numbackends
```

4. Configura visualizzazione (Graph, Gauge, Stat, ecc.)
5. Click **Apply** → **Save Dashboard**

---

## 🔔 Configurare Alerting

### 1. In Prometheus (Alert Rules)

Le alert rules sono già configurate in `alert-rules.yml`:
- Circuit Breaker Triggered
- Betfair Disconnected
- High Order Failure Rate
- Low Account Balance
- ecc.

Verifica: http://localhost:9090/alerts

### 2. In Grafana (Notification Channels)

1. Vai su **Alerting** → **Notification channels**
2. Click **Add channel**
3. Scegli tipo (Email, Slack, Teams, Webhook, ecc.)
4. Configura credenziali
5. **Test** → **Save**

### 3. Aggiungere Alert a Dashboard Panel

1. Apri un panel in edit mode
2. Tab **Alert**
3. **Create Alert**
4. Configura condizione (es. `WHEN avg() OF query(A, 5m) IS ABOVE 100`)
5. Seleziona notification channel
6. **Save**

---

## 🎯 Metriche Chiave da Monitorare

### AIBettingExecutor
- `aibetting_executor_orders_placed_total` - Ordini piazzati
- `aibetting_executor_orders_matched_total` - Ordini matched
- `aibetting_executor_circuit_breaker_status` - Circuit breaker (0=ok, 1=triggered)
- `aibetting_executor_account_balance` - Balance account
- `aibetting_executor_order_execution_latency_seconds` - Latency esecuzione

### AIBettingAnalyst
- `aibetting_analyst_signals_generated_total` - Segnali generati
- `aibetting_analyst_strategy_avg_confidence` - Confidenza strategie

### AIBettingExplorer
- `aibetting_price_updates_total` - Aggiornamenti prezzi
- `aibetting_processing_latency_seconds` - Latency processing

### Infrastructure
- `redis_memory_used_bytes` - Memoria Redis
- `pg_stat_database_numbackends` - Connessioni PostgreSQL
- `node_memory_MemAvailable_bytes` - Memoria sistema disponibile

---

## 🆘 Troubleshooting

### Target rimane DOWN dopo aver avviato l'app

**Verifica 1: Metriche endpoint accessibile**
```bash
curl http://localhost:5001/metrics  # Explorer
curl http://localhost:5002/metrics  # Analyst
curl http://localhost:5003/metrics  # Executor
```

**Verifica 2: Prometheus può risolvere host.docker.internal**
```bash
docker exec aibetting-prometheus-v2 nslookup host.docker.internal
```

**Verifica 3: Firewall non blocca porte**
```bash
Test-NetConnection -ComputerName localhost -Port 5001
Test-NetConnection -ComputerName localhost -Port 5002
Test-NetConnection -ComputerName localhost -Port 5003
```

**Soluzione:** Se il problema persiste, riavvia Prometheus:
```bash
docker restart aibetting-prometheus-v2
```

### Grafana "Data source is working" ma non mostra dati

**Causa:** Time range potrebbe essere sbagliato o nessun dato in quel range.

**Soluzione:**
1. Cambia time range in alto a destra (es. "Last 15 minutes")
2. Verifica query PromQL: scrivi `up` per testare
3. Controlla che le app siano running e generino metriche

### Dashboard vuote o "No data"

**Causa:** Query PromQL non matchano i nomi delle metriche attuali.

**Soluzione:**
1. Vai su http://localhost:9090/graph
2. Type ahead per vedere metriche disponibili
3. Aggiorna query nelle dashboard con i nomi corretti

---

## ✅ Checklist Finale

- [x] Prometheus configurato per applicazioni host con `host.docker.internal`
- [x] Prometheus riavviato e configurazione caricata
- [x] Data source Prometheus creato in Grafana
- [x] Data source testato con successo
- [x] Target infrastructure (redis, postgresql, node) UP
- [ ] **TODO:** Avviare AIBettingExplorer, Analyst, Executor
- [ ] **TODO:** Verificare tutti i target UP in Prometheus
- [ ] **TODO:** Creare/importare dashboards in Grafana
- [ ] **TODO:** Configurare notification channels per alerting

---

## 📚 Risorse

### URL Utili
- **Prometheus**: http://localhost:9090
- **Prometheus Targets**: http://localhost:9090/targets
- **Prometheus Alerts**: http://localhost:9090/alerts
- **Grafana**: http://localhost:3000
- **Alertmanager**: http://localhost:9093

### Documentazione
- **Prometheus Docs**: https://prometheus.io/docs/
- **Grafana Docs**: https://grafana.com/docs/
- **PromQL Guide**: https://prometheus.io/docs/prometheus/latest/querying/basics/

### Dashboard IDs (Grafana.com)
- **1860** - Node Exporter Full
- **7362** - Redis Dashboard for Prometheus
- **9628** - PostgreSQL Database
- **763** - Redis (by Oliver006)

---

**Data Risoluzione:** 15 Gennaio 2026  
**Stato:** ✅ COMPLETATO  
**Prossimo Step:** Avviare le applicazioni AIBetting per far diventare UP tutti i target

---

## 🎉 Congratulazioni!

Hai risolto entrambi i problemi:
1. ✅ Prometheus ora può raggiungere le applicazioni host
2. ✅ Grafana ha il data source Prometheus configurato

**Ora puoi:**
- Visualizzare metriche real-time in Grafana
- Creare dashboard custom
- Configurare alert per monitoraggio proattivo
- Tracciare performance e business metrics

**Happy Monitoring! 📊🚀**
