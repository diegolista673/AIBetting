# ✅ AIBetting Monitoring Stack - Ripristino Completato

## 📋 Riepilogo Operazioni Eseguite

### 1. Aggiornamento Docker Compose
**File:** `AIBettingExecutor/Grafana/docker-compose.yml`

**Modifiche apportate:**
- ✅ Aggiunto servizio **Redis** (porta 16379:6379)
- ✅ Aggiunto servizio **PostgreSQL** (porta 15432:5432)
- ✅ Aggiunto servizio **postgres-exporter** (porta 9187)
- ✅ Aggiornato **redis-exporter** per puntare al Redis container
- ✅ Configurati volumi persistenti per tutti i servizi
- ✅ Risolti conflitti di porta con istanze locali

**Porte modificate per evitare conflitti:**
- Redis: `16379:6379` (invece di 6379:6379)
- PostgreSQL: `15432:5432` (invece di 5432:5432)

### 2. Procedura di Avvio Eseguita

```bash
cd AIBettingExecutor\Grafana
docker compose down
docker compose pull
docker compose up -d
```

### 3. Stato Container - TUTTI RUNNING ✅

| Container | Stato | Porta | Health Check |
|-----------|-------|-------|--------------|
| aibetting-prometheus-v2 | ✅ Running | 9090:9090 | ✅ HEALTHY |
| aibetting-grafana | ✅ Running | 3000:3000 | ✅ HEALTHY |
| aibetting-alertmanager | ✅ Running | 9093:9093 | ✅ HEALTHY |
| aibetting-redis | ✅ Running | 16379:6379 | ✅ HEALTHY (PONG) |
| aibetting-postgres | ✅ Running | 15432:5432 | ✅ HEALTHY (accepting connections) |
| aibetting-redis-exporter | ✅ Running | 9122:9121 | ✅ Running |
| aibetting-postgres-exporter | ✅ Running | 9187:9187 | ✅ Running |
| aibetting-node-exporter | ✅ Running | 9100:9100 | ✅ Running |

### 4. Volumi Persistenti Creati

```
grafana_prometheus-data
grafana_grafana-data
grafana_alertmanager-data
grafana_redis-data
grafana_postgres-data
```

## 🌐 Accesso ai Servizi

### Interfacce Web
- **Prometheus**: http://localhost:9090
  - Targets: http://localhost:9090/targets
  - Alerts: http://localhost:9090/alerts
  - Config: http://localhost:9090/config

- **Grafana**: http://localhost:3000
  - Username: `admin`
  - Password: `admin`
  - Version: 12.3.1
  
- **Alertmanager**: http://localhost:9093
  - Status: http://localhost:9093/#/status

### Database & Cache
- **Redis**: `localhost:16379`
  - No password (per Docker networking interno usa `redis:6379`)
  - Persistent: AOF enabled

- **PostgreSQL**: `localhost:15432`
  - Database: `aibetting_accounting`
  - Username: `aibetting`
  - Password: `AIBetting2024!`
  - Connection string: `Host=localhost;Port=15432;Database=aibetting_accounting;Username=aibetting;Password=AIBetting2024!`

### Exporters (Metrics Endpoints)
- **Redis Exporter**: http://localhost:9122/metrics
- **Postgres Exporter**: http://localhost:9187/metrics
- **Node Exporter**: http://localhost:9100/metrics

## 📊 Configurazione Prometheus

### Targets configurati in `prometheus.yml`:
- ✅ aibetting-explorer:5001 (tier: input)
- ✅ aibetting-analyst:5002 (tier: processing)
- ✅ aibetting-executor:5003 (tier: output)
- ✅ prometheus:9090 (self-monitoring)
- ✅ redis-exporter:9121
- ✅ postgres-exporter:9187
- ✅ node-exporter:9100

### Alert Rules
**File:** `alert-rules.yml`
- Executor critical alerts (Betfair disconnection, circuit breaker, balance)
- Performance alerts (latency, throughput)
- Resource alerts (CPU, memory)
- Order quality alerts (failure rate, cancellation rate)

### Alertmanager Configuration
**File:** `alertmanager.yml`
- Routing basato su severity (critical, warning, info)
- Email notifications configurate
- Webhook Slack/Teams pronti per configurazione
- Inhibition rules per evitare spam

## 🚀 Prossimi Passi

### 1. ✅ Configurazioni Aggiornate (COMPLETATO)

**Tutti i file appsettings.json sono stati aggiornati per usare i servizi Docker:**

**Redis (porta 16379):**
- ✅ AIBettingExplorer/appsettings.json
- ✅ AIBettingAnalyst/appsettings.json
- ✅ AIBettingExecutor/appsettings.json
- Connection String: `localhost:16379,abortConnect=false,connectRetry=3,connectTimeout=5000,syncTimeout=5000`

**PostgreSQL (porta 15432):**
- ✅ AIBettingAccounting/appsettings.json
- Connection String: `Host=localhost;Port=15432;Database=aibetting_accounting;Username=aibetting;Password=AIBetting2024!;SslMode=Disable`

**IMPORTANTE:** Tutte le applicazioni ora puntano a Redis e PostgreSQL nei container Docker!

### 2. Configurare Grafana (PRIMO ACCESSO)
```bash
# Accedi a http://localhost:3000
# Login: admin/admin (ti verrà chiesto di cambiare password)

1. Add Data Source -> Prometheus
   - URL: http://prometheus:9090
   - Access: Server (default)
   - Save & Test

2. Import Dashboards
   - Go to Dashboards -> Import
   - Upload JSON or use dashboard IDs
   - Select Prometheus as data source
```

### 3. Verificare Targets Prometheus
```bash
# Accedi a http://localhost:9090/targets
# Verifica che i seguenti siano UP:
- prometheus (self)
- redis-exporter
- postgres-exporter
- node-exporter

# I seguenti saranno DOWN fino a quando non avvii le app:
- aibetting-explorer
- aibetting-analyst
- aibetting-executor
```

### 4. Configurare AIBettingExecutor per usare Redis/PostgreSQL in Docker

**Opzione A: Usa i servizi Docker (consigliato per test)**
Modifica `appsettings.json`:
```json
{
  "Executor": {
    "Redis": {
      "ConnectionString": "localhost:16379"
    }
  }
}
```

**Opzione B: Usa istanze locali (configurazione attuale)**
Mantieni la configurazione esistente che punta a Redis/PostgreSQL locali.

### 5. Avviare AIBettingExecutor con Metrics
Assicurati che `Executor:PrometheusMetricsPort` sia configurato su `5003`:
```json
{
  "Executor": {
    "PrometheusMetricsPort": 5003
  }
}
```

Poi avvia l'applicazione. Prometheus inizierà a raccogliere metriche da `localhost:5003/metrics`.

### 6. Personalizzare Alertmanager (Opzionale)
Modifica `alertmanager.yml` per configurare:
- Email SMTP settings
- Slack webhook URL
- PagerDuty integration
- Teams/Discord webhooks

Dopo le modifiche:
```bash
docker restart aibetting-alertmanager
```

## 🔧 Comandi Utili

### Gestione Stack
```bash
# Directory di lavoro
cd AIBettingExecutor\Grafana

# Avviare stack
docker compose up -d

# Fermare stack (mantiene dati)
docker compose down

# Fermare e rimuovere volumi (PERDI I DATI!)
docker compose down -v

# Restart singolo servizio
docker compose restart prometheus
docker compose restart grafana

# Visualizzare log
docker logs aibetting-prometheus-v2 --tail=100 -f
docker logs aibetting-grafana --tail=100 -f
docker logs aibetting-redis --tail=50

# Stato servizi
docker compose ps
```

### Debug & Troubleshooting
```bash
# Accedere a un container
docker exec -it aibetting-prometheus-v2 sh
docker exec -it aibetting-grafana bash
docker exec -it aibetting-redis redis-cli

# Verificare configurazione Prometheus
docker exec aibetting-prometheus-v2 promtool check config /etc/prometheus/prometheus.yml

# Verificare connessione Redis
docker exec aibetting-redis redis-cli ping
docker exec aibetting-redis redis-cli info

# Verificare connessione PostgreSQL
docker exec aibetting-postgres psql -U aibetting -d aibetting_accounting -c "\l"
docker exec aibetting-postgres psql -U aibetting -d aibetting_accounting -c "\dt"

# Testare metriche exporters
curl http://localhost:9122/metrics  # Redis
curl http://localhost:9187/metrics  # PostgreSQL
curl http://localhost:9100/metrics  # Node
```

### Backup & Restore
```bash
# Backup Grafana dashboards/settings
docker exec aibetting-grafana grafana-cli admin export > grafana-backup.json

# Backup PostgreSQL database
docker exec aibetting-postgres pg_dump -U aibetting aibetting_accounting > backup.sql

# Restore PostgreSQL
docker exec -i aibetting-postgres psql -U aibetting aibetting_accounting < backup.sql

# Backup Redis data
docker exec aibetting-redis redis-cli BGSAVE
docker cp aibetting-redis:/data/dump.rdb ./redis-backup.rdb
```

## 📝 Note Importanti

### 1. Networking
- Tutti i servizi sono sulla rete Docker `aibetting-monitoring-v2`
- Comunicazione interna usa nomi container (es. `prometheus:9090`)
- Accesso esterno usa `localhost:PORT` (es. `localhost:9090`)

### 2. Persistenza Dati
- I dati sono persistenti anche dopo `docker compose down`
- Per reset completo: `docker compose down -v` (ATTENZIONE: cancella tutto!)
- I volumi sono in: `/var/lib/docker/volumes/grafana_*`

### 3. Sicurezza
- Password di default: **CAMBIALA PRIMA DI ANDARE IN PRODUZIONE**
- Grafana admin/admin
- PostgreSQL AIBetting2024!
- Redis senza password
- Alertmanager senza autenticazione

### 4. Performance
- Prometheus retention: 30 giorni
- Scrape interval: 10-30 secondi (varia per job)
- Redis AOF persistence enabled
- PostgreSQL shared_buffers e work_mem default

## 🎯 Test di Verifica Finale

Esegui questi test per confermare che tutto funziona:

```bash
# 1. Tutti i container sono UP
docker compose ps | findstr "Up"

# 2. Prometheus è healthy
docker exec aibetting-prometheus-v2 wget -qO- http://localhost:9090/-/healthy

# 3. Grafana è healthy
docker exec aibetting-grafana wget -qO- http://localhost:3000/api/health

# 4. Redis risponde
docker exec aibetting-redis redis-cli ping

# 5. PostgreSQL risponde
docker exec aibetting-postgres pg_isready -U aibetting

# 6. Exporters esportano metriche
curl -s http://localhost:9122/metrics | Select-String "redis_up"
curl -s http://localhost:9187/metrics | Select-String "pg_up"
```

## ✅ Risultato Finale

**Tutti i servizi sono stati ripristinati con successo!**

Stack completo:
- ✅ Prometheus (monitoring & alerting)
- ✅ Grafana (visualization)
- ✅ Alertmanager (alert routing)
- ✅ Redis (cache & message bus)
- ✅ PostgreSQL (database)
- ✅ Redis Exporter (metrics)
- ✅ PostgreSQL Exporter (metrics)
- ✅ Node Exporter (system metrics)

**Data:** 15 Gennaio 2026
**Versioni:**
- Prometheus: 3.9.1
- Grafana: 12.3.1
- Alertmanager: 0.30.1
- Redis: 6.2.21
- PostgreSQL: 15.15

---

Per domande o problemi, verifica i log dei container con:
```bash
docker logs <container-name> --tail=100 -f
