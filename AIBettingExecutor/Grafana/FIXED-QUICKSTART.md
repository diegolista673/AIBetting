# 🚀 FIXED - Docker Compose Quick Start

## ✅ Problemi Risolti

1. **Porta 9121 occupata** → Cambiata a 9122
2. **Volume grafana-provisioning mancante** → Rimosso (dashboard importata manualmente)
3. **Targets Prometheus aggiornati** → Redis exporter ora su porta 9122

---

## 🎯 Avvio Rapido

### 1. Naviga nella Directory
```powershell
cd C:\Users\SMARTW\source\repos\AIBettingSolution\AIBettingExecutor\Grafana
```

### 2. Verifica File Necessari
```powershell
# Assicurati che questi file esistano:
ls prometheus.yml
ls alert-rules.yml
ls alertmanager.yml
ls docker-compose.yml
```

### 3. Pulisci Container Precedenti
```powershell
docker-compose down -v
docker system prune -f
```

### 4. Avvia Stack
```powershell
docker-compose up -d
```

### 5. Verifica Stato
```powershell
docker-compose ps

# Output atteso:
# NAME                        STATUS
# aibetting-prometheus        Up
# aibetting-grafana          Up
# aibetting-alertmanager     Up
# aibetting-redis-exporter   Up
# aibetting-node-exporter    Up
```

---

## 🌐 Accesso Servizi

| Servizio | URL | Credenziali |
|----------|-----|-------------|
| **Grafana** | http://localhost:3000 | admin/admin |
| **Prometheus** | http://localhost:9090 | - |
| **Alertmanager** | http://localhost:9093 | - |

---

## 📊 Import Dashboard Grafana

Dato che il provisioning automatico è stato rimosso, importa la dashboard manualmente:

### Step 1: Login Grafana
1. Apri http://localhost:3000
2. Login: `admin` / `admin`
3. (Opzionale) Cambia password

### Step 2: Add Prometheus Data Source
1. **Configuration** (⚙️) → **Data Sources**
2. **Add data source**
3. Seleziona **Prometheus**
4. **URL**: `http://prometheus:9090` (nome container Docker)
5. **Save & Test** (deve dire "Data source is working")

### Step 3: Import Executor Dashboard
1. **Dashboards** (+) → **Import**
2. **Upload JSON file**
3. Seleziona: `C:\Users\SMARTW\source\repos\AIBettingSolution\AIBettingExecutor\Grafana\executor-dashboard.json`
4. **Select a Prometheus data source**: Scegli quello creato al step 2
5. **Import**

✅ Dashboard pronta!

---

## 🔍 Troubleshooting

### ❌ Container non parte

```powershell
# Check logs
docker-compose logs grafana
docker-compose logs prometheus

# Restart singolo container
docker-compose restart grafana
```

### ❌ Prometheus non trova targets

```powershell
# Verifica che i servizi AIBetting siano in esecuzione
# Explorer: porta 5001
# Analyst: porta 5002
# Executor: porta 5003

# Test endpoints
curl http://localhost:5001/metrics
curl http://localhost:5002/metrics
curl http://localhost:5003/metrics
```

### ❌ Redis Exporter fallisce

Il Redis Exporter è **opzionale**. Se fallisce:
```powershell
# Disabilita solo redis-exporter
docker-compose up -d prometheus grafana alertmanager node-exporter
```

Oppure verifica che Redis sia in esecuzione:
```powershell
redis-cli -a RedisAIBet2024! PING
# Risposta attesa: PONG
```

---

## 🛠️ Comandi Utili

```powershell
# Stop tutto
docker-compose down

# Start tutto
docker-compose up -d

# Restart tutto
docker-compose restart

# View logs in real-time
docker-compose logs -f

# Remove tutto (inclusi volumi)
docker-compose down -v

# Check risorse Docker
docker system df
```

---

## 📈 Verifica Metriche

### Prometheus Targets
1. Apri http://localhost:9090/targets
2. Verifica tutti i job siano **UP**:
   - aibetting-executor
   - aibetting-analyst
   - aibetting-explorer
   - prometheus
   - redis (se Redis in esecuzione)
   - node (system metrics)

### Grafana Dashboard
1. Apri http://localhost:3000
2. **Dashboards** → **AIBetting Executor Dashboard**
3. Verifica pannelli mostrano dati (non "No data")

---

## 🎉 Success Checklist

- [x] Docker containers running (`docker-compose ps`)
- [x] Grafana accessible (http://localhost:3000)
- [x] Prometheus targets UP (http://localhost:9090/targets)
- [x] Dashboard imported e funzionante
- [x] Metriche Executor visibili (dopo avvio AIBettingExecutor)

---

## 📞 Support

Se problemi persistono:
1. Check logs: `docker-compose logs`
2. Verifica porte libere: `netstat -ano | findstr :3000`
3. Restart Docker Desktop
4. Verifica .NET services in esecuzione

---

**🚀 Monitoring stack pronto per AIBetting Executor!**
