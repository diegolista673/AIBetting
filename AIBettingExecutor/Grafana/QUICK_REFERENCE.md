# 🛠️ QUICK REFERENCE - Scripts & Comandi Utili

## 📁 Script PowerShell Disponibili

Tutti gli script sono in `AIBettingExecutor\Grafana\`

### 🚀 Gestione Applicazioni

#### manage-apps.ps1
Gestisce start/stop/restart delle applicazioni AIBetting.

```powershell
# Vedere lo stato di tutte le app
.\manage-apps.ps1 -Action status

# Avviare tutte le app (in finestre separate)
.\manage-apps.ps1 -Action start

# Fermare tutte le app
.\manage-apps.ps1 -Action stop

# Riavviare tutte le app
.\manage-apps.ps1 -Action restart
```

**Output status:**
```
AIBettingExplorer:
  Process:  RUNNING (PID: 12345)
  Port:     5001 LISTENING
  Memory:   85.3 MB
  CPU:      2.5s

AIBettingAnalyst:
  Process:  RUNNING (PID: 12346)
  Port:     5002 LISTENING
  ...
```

---

### 🧹 Cleanup & Build

#### cleanup-build.ps1
Risolve problemi di build (file locked, processi zombie).

```powershell
.\cleanup-build.ps1
```

**Cosa fa:**
1. Termina tutti i processi AIBetting
2. Elimina cartelle bin/obj
3. Verifica nessun processo rimasto
4. Controlla disponibilità porte 5001, 5002, 5003

**Quando usarlo:**
- Errore MSB3027/MSB3021 (file locked)
- Build fallisce con "file in use"
- Dopo chiusura anomala applicazioni

---

### 📊 Monitoring Stack

#### setup-grafana-datasource.ps1
Configura automaticamente data source Prometheus in Grafana.

```powershell
.\setup-grafana-datasource.ps1
```

**Output:**
```
1. Checking Grafana health...
   OK - Grafana is healthy (version: 12.3.1)

2. Checking existing data sources...
   INFO - Prometheus data source already exists (ID: 1)
   Deleting old data source...
   OK - Old data source deleted

3. Creating new Prometheus data source...
   OK - Prometheus data source created successfully!
   ID: 2
   Name: Prometheus
   URL: http://prometheus:9090

4. Testing Prometheus data source...
   OK - Data source test successful!
```

---

#### check-targets.ps1
Verifica stato targets Prometheus.

```powershell
.\check-targets.ps1
```

**Output:**
```
Summary:
  Total Targets: 7
  UP:            7
  DOWN:          0

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

#### verify-stack.ps1
Health check completo stack Docker.

```powershell
.\verify-stack.ps1
```

**Verifica:**
- Prometheus HEALTHY
- Grafana HEALTHY
- Alertmanager HEALTHY
- Redis PONG
- PostgreSQL accepting connections

---

#### test-connectivity.ps1
Test connettività Redis e PostgreSQL.

```powershell
.\test-connectivity.ps1
```

---

#### test-e2e.ps1
Test end-to-end completo (stack + connectivity + config).

```powershell
.\test-e2e.ps1
```

---

## 🔧 Comandi Docker Compose

```powershell
cd AIBettingExecutor\Grafana

# Avviare stack
docker compose up -d

# Fermare stack (mantiene dati)
docker compose down

# Riavviare stack
docker compose restart

# Vedere log
docker compose logs -f prometheus
docker compose logs -f grafana
docker compose logs --tail=100 redis

# Status container
docker compose ps
```

---

## 🐛 Troubleshooting Rapido

### ❌ Build fallisce con "file locked"

```powershell
# Soluzione 1: Script cleanup
.\cleanup-build.ps1

# Soluzione 2: Manuale
Get-Process -Name "AIBetting*" | Stop-Process -Force
```

---

### ❌ Porta già in uso (5001, 5002, 5003)

```powershell
# Identificare processo che usa la porta
netstat -ano | findstr "5001"
netstat -ano | findstr "5002"
netstat -ano | findstr "5003"

# Terminare processo (sostituisci PID)
Stop-Process -Id <PID> -Force

# O usa lo script
.\manage-apps.ps1 -Action stop
```

---

### ❌ Redis connection timeout

```powershell
# Verifica Docker Redis running
docker ps | findstr redis

# Test connettività
docker exec aibetting-redis redis-cli ping
# Output atteso: PONG

# Test porta accessibile
Test-NetConnection -ComputerName localhost -Port 16379
```

**Fix:** Verifica `appsettings.json` usa porta **16379** (non 6379).

---

### ❌ Prometheus targets DOWN

```powershell
# Check status
.\check-targets.ps1

# Se app target DOWN ma app running:
# 1. Verifica metrics endpoint
curl http://localhost:5001/metrics
curl http://localhost:5002/metrics
curl http://localhost:5003/metrics

# 2. Riavvia Prometheus
docker restart aibetting-prometheus-v2

# 3. Verifica prometheus.yml usa host.docker.internal
docker exec aibetting-prometheus-v2 cat /etc/prometheus/prometheus.yml | findstr "host.docker.internal"
```

---

### ❌ Grafana non mostra dati

```powershell
# 1. Verifica data source
.\setup-grafana-datasource.ps1

# 2. Accedi a Grafana
Start-Process "http://localhost:3000"
# Configuration -> Data Sources -> Prometheus -> Save & Test

# 3. Test query semplice
# Query: up
# Dovresti vedere tutti i target con valore 1
```

---

## 🎯 Workflow Completo

### Primo Setup (dopo clone repo)

```powershell
# 1. Avvia stack Docker
cd AIBettingExecutor\Grafana
docker compose up -d

# 2. Configura Grafana
.\setup-grafana-datasource.ps1

# 3. Avvia applicazioni
.\manage-apps.ps1 -Action start

# 4. Verifica tutto UP
.\check-targets.ps1
```

---

### Uso Giornaliero

```powershell
# Mattina: Avvia tutto
cd AIBettingExecutor\Grafana
docker compose up -d
.\manage-apps.ps1 -Action start

# Durante il giorno: Monitora
Start-Process "http://localhost:9090/targets"
Start-Process "http://localhost:3000"

# Sera: Ferma tutto
.\manage-apps.ps1 -Action stop
docker compose down
```

---

### Dopo Modifiche Codice

```powershell
# 1. Ferma app
.\manage-apps.ps1 -Action stop

# 2. Cleanup (se build fallisce)
.\cleanup-build.ps1

# 3. Build manuale (opzionale)
cd AIBettingAnalyst
dotnet build

# 4. Riavvia
cd ..\AIBettingExecutor\Grafana
.\manage-apps.ps1 -Action start
```

---

## 📚 URL Utili

| Servizio | URL | Credenziali |
|----------|-----|-------------|
| **Prometheus** | http://localhost:9090 | - |
| **Prometheus Targets** | http://localhost:9090/targets | - |
| **Prometheus Alerts** | http://localhost:9090/alerts | - |
| **Grafana** | http://localhost:3000 | admin/admin |
| **Alertmanager** | http://localhost:9093 | - |
| **Explorer Metrics** | http://localhost:5001/metrics | - |
| **Analyst Metrics** | http://localhost:5002/metrics | - |
| **Executor Metrics** | http://localhost:5003/metrics | - |

---

## 🎯 Metriche Chiave

### Test rapido metriche disponibili

```powershell
# Explorer
curl http://localhost:5001/metrics | Select-String "aibetting"

# Analyst
curl http://localhost:5002/metrics | Select-String "aibetting"

# Executor
curl http://localhost:5003/metrics | Select-String "aibetting"
```

### Query PromQL Utili

```promql
# Tutte le app UP
up

# Order rate (per minuto)
rate(aibetting_executor_orders_placed_total[5m]) * 60

# Signal generation rate
rate(aibetting_analyst_signals_generated_total[5m]) * 60

# Price updates rate
rate(aibetting_price_updates_total[5m]) * 60

# Redis memory usage (MB)
redis_memory_used_bytes / 1024 / 1024

# PostgreSQL connections
pg_stat_database_numbackends

# Execution latency P99
histogram_quantile(0.99, rate(aibetting_executor_order_execution_latency_seconds_bucket[5m]))
```

---

## 🆘 Aiuto Rapido

```powershell
# Status completo sistema
.\verify-stack.ps1          # Docker stack
.\manage-apps.ps1           # Apps status
.\check-targets.ps1         # Prometheus targets

# Reset completo (ULTIMA RISORSA)
.\manage-apps.ps1 -Action stop
docker compose down -v      # ATTENZIONE: Cancella dati!
.\cleanup-build.ps1
docker compose up -d
.\setup-grafana-datasource.ps1
.\manage-apps.ps1 -Action start
```

---

## 📄 Documentazione Completa

1. **SOLUTION_SUMMARY.md** - Risoluzione errore Redis
2. **TARGETS_AND_GRAFANA_FIXED.md** - Setup Prometheus & Grafana
3. **STACK_RESTORATION_COMPLETE.md** - Guida completa stack
4. **CONFIGURATION_CHANGES.md** - Modifiche configurazioni
5. **QUICK_REFERENCE.md** - Questo file

---

**Ultimo Aggiornamento:** 15 Gennaio 2026  
**Versione:** 1.0
