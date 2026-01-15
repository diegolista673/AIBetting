# ✅ Configurazioni Aggiornate - Redis e PostgreSQL Docker

## 📋 Problema Risolto

**Errore originale:**
```
StackExchange.Redis.RedisConnectionException: It was not possible to connect to the redis server(s) localhost:6379
```

**Causa:** Le applicazioni cercavano di connettersi a Redis sulla porta 6379 (istanza locale), ma Redis è in Docker sulla porta **16379**.

## 🔧 Modifiche Apportate

### 1. Redis Connection String (porta 16379)

**File modificati:**
- ✅ `AIBettingExplorer\appsettings.json`
- ✅ `AIBettingAnalyst\appsettings.json`  
- ✅ `AIBettingExecutor\appsettings.json`

**Prima:**
```json
"ConnectionString": "localhost:6379,password=RedisAIBet2024!,abortConnect=false,..."
```

**Dopo:**
```json
"ConnectionString": "localhost:16379,abortConnect=false,connectRetry=3,connectTimeout=5000,syncTimeout=5000"
```

**Cambiamenti:**
- ✅ Porta cambiata da `6379` → `16379`
- ✅ Rimossa password (Redis Docker non ha password configurata)
- ✅ Mantenuti timeout e retry settings

### 2. PostgreSQL Connection String (porta 15432)

**File modificato:**
- ✅ `AIBettingAccounting\appsettings.json`

**Prima:**
```json
"Accounting": "Host=localhost;Port=5432;Database=aibetting_db;Username=aibetting_user;Password=AIBet2024!WinRate;..."
```

**Dopo:**
```json
"Accounting": "Host=localhost;Port=15432;Database=aibetting_accounting;Username=aibetting;Password=AIBetting2024!;SslMode=Disable"
```

**Cambiamenti:**
- ✅ Porta cambiata da `5432` → `15432`
- ✅ Database name: `aibetting_db` → `aibetting_accounting` (match Docker)
- ✅ Username: `aibetting_user` → `aibetting` (match Docker)
- ✅ Password aggiornata per match Docker credentials

## 🐳 Servizi Docker Mapping

| Servizio | Porta Docker (interna) | Porta Host (esterna) | Connection String |
|----------|------------------------|----------------------|-------------------|
| Redis | 6379 | 16379 | `localhost:16379` |
| PostgreSQL | 5432 | 15432 | `localhost:15432` |
| Prometheus | 9090 | 9090 | `http://localhost:9090` |
| Grafana | 3000 | 3000 | `http://localhost:3000` |
| Alertmanager | 9093 | 9093 | `http://localhost:9093` |

## ✅ Test di Verifica

### Redis Test
```bash
# Test from Docker container
docker exec aibetting-redis redis-cli ping
# Expected: PONG

# Test connection from PowerShell
Test-NetConnection -ComputerName localhost -Port 16379
# Expected: TcpTestSucceeded: True
```

### PostgreSQL Test
```bash
# Test from Docker container
docker exec aibetting-postgres pg_isready -U aibetting
# Expected: /var/run/postgresql:5432 - accepting connections

# Test connection from PowerShell
Test-NetConnection -ComputerName localhost -Port 15432
# Expected: TcpTestSucceeded: True
```

## 🚀 Come Usare le Nuove Configurazioni

### 1. Assicurati che i container Docker siano running
```bash
cd AIBettingExecutor\Grafana
docker compose ps
```

Output atteso:
```
aibetting-redis              Up      0.0.0.0:16379->6379/tcp
aibetting-postgres           Up      0.0.0.0:15432->5432/tcp
```

### 2. Riavvia le tue applicazioni
Le applicazioni ora si connetteranno automaticamente ai servizi Docker:

```bash
# AIBettingExplorer
cd AIBettingExplorer
dotnet run

# AIBettingAnalyst
cd AIBettingAnalyst
dotnet run

# AIBettingExecutor
cd AIBettingExecutor
dotnet run
```

### 3. Verifica le connessioni nei log
Cerca nei log di startup:
- ✅ `✅ Redis connected`
- ✅ `Connected to PostgreSQL`

## 🔄 Rollback (se necessario)

Se vuoi tornare a usare istanze locali di Redis/PostgreSQL:

### Per Redis locale (porta 6379):
```json
"ConnectionString": "localhost:6379,password=RedisAIBet2024!,abortConnect=false,..."
```

### Per PostgreSQL locale (porta 5432):
```json
"Accounting": "Host=localhost;Port=5432;Database=aibetting_db;..."
```

## 📝 Note Importanti

1. **Password Redis Docker**: Il Redis container non ha password configurata per semplicità in ambiente di sviluppo
2. **Conflitti di porta**: Le porte 16379 e 15432 sono state scelte per evitare conflitti con istanze locali
3. **Networking Docker**: I container comunicano tra loro usando i nomi container (es. `redis:6379`), non `localhost`
4. **Persistenza dati**: I dati sono persistenti nei volumi Docker anche dopo restart

## 🛠️ Troubleshooting

### Errore: "Cannot connect to Redis"
```bash
# 1. Verifica che il container sia running
docker ps | findstr redis

# 2. Verifica la porta
Test-NetConnection -ComputerName localhost -Port 16379

# 3. Riavvia il container
docker restart aibetting-redis
```

### Errore: "Cannot connect to PostgreSQL"
```bash
# 1. Verifica che il container sia running
docker ps | findstr postgres

# 2. Verifica la porta
Test-NetConnection -ComputerName localhost -Port 15432

# 3. Riavvia il container
docker restart aibetting-postgres
```

## 📊 Files Checklist

| File | Status | Changes |
|------|--------|---------|
| AIBettingExplorer\appsettings.json | ✅ Updated | Redis port 16379, no password |
| AIBettingAnalyst\appsettings.json | ✅ Updated | Redis port 16379, no password |
| AIBettingExecutor\appsettings.json | ✅ Updated | Redis port 16379, no password |
| AIBettingAccounting\appsettings.json | ✅ Updated | PostgreSQL port 15432, updated credentials |
| AIBettingBlazorDashboard\appsettings.json | ℹ️ No changes | No Redis/PostgreSQL config |

## ✅ Conferma Finale

**Tutti i file di configurazione sono stati aggiornati con successo!**

Le applicazioni ora sono configurate per usare:
- ✅ Redis Docker (porta 16379)
- ✅ PostgreSQL Docker (porta 15432)
- ✅ Prometheus, Grafana, Alertmanager Docker

**Prossimo passo:** Riavvia le applicazioni per applicare le nuove configurazioni.

---

**Data:** 15 Gennaio 2026  
**Stato:** ✅ COMPLETATO
