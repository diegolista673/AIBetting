# ?? AIBetting - Quick Start con Docker

Setup completo dell'infrastruttura AIBetting in **5 minuti**.

---

## ?? Prerequisiti

- **Docker Desktop** installato: https://www.docker.com/products/docker-desktop/
- **.NET 10 SDK** installato: https://dotnet.microsoft.com/download/dotnet/10.0
- **Git** installato

---

## ? Setup Rapido (3 Step)

### Step 1: Avvia Infrastructure

```powershell
# Naviga nella directory progetto
cd C:\Users\SMARTW\source\repos\AIBettingSolution

# Avvia PostgreSQL + Redis
docker-compose -f docker-compose.postgresql.yml up -d

# Verifica che siano running
docker ps
```

**Output atteso:**
```
CONTAINER ID   IMAGE                STATUS         PORTS
abc123...      postgres:16-alpine   Up 30 seconds  0.0.0.0:5432->5432/tcp
def456...      redis:7-alpine       Up 30 seconds  0.0.0.0:6379->6379/tcp
```

### Step 2: Verifica Connessioni

```powershell
# Test PostgreSQL
docker exec -it aibetting_postgres psql -U aibetting_user -d aibetting_db -c "\dt"

# Output atteso:
#  Schema |      Name        | Type
# --------+------------------+-------
#  public | daily_summaries  | table
#  public | trades           | table

# Test Redis
docker exec -it aibetting_redis redis-cli -a RedisAIBet2024! PING

# Output atteso:
# PONG
```

### Step 3: Test Progetti .NET

```powershell
# Test AIBettingAccounting (PostgreSQL)
cd AIBettingAccounting
dotnet run

# Output atteso:
# ? Connessione PostgreSQL verificata
# ? Tabelle presenti

# Test AIBettingExplorer (Redis)
cd ..\AIBettingExplorer
dotnet run

# Output atteso:
# ? Redis connesso
```

---

## ?? Credenziali Default

| Servizio | Credenziale | Valore |
|----------|-------------|--------|
| **PostgreSQL** | Username | `aibetting_user` |
| | Password | `AIBet2024!WinRate` |
| | Database | `aibetting_db` |
| | Port | `5432` |
| **Redis** | Password | `RedisAIBet2024!` |
| | Port | `6379` |
| **pgAdmin** | Email | `admin@aibetting.com` |
| | Password | `admin` |
| | URL | http://localhost:5050 |
| **RedisInsight** | URL | http://localhost:5540 |

---

## ?? Connection Strings nei Progetti

### AIBettingAccounting/appsettings.json
```json
{
  "ConnectionStrings": {
    "Accounting": "Host=localhost;Port=5432;Database=aibetting_db;Username=aibetting_user;Password=AIBet2024!WinRate;SslMode=Disable"
  }
}
```

### AIBettingExplorer/appsettings.json
```json
{
  "Redis": {
    "ConnectionString": "localhost:6379,password=RedisAIBet2024!,abortConnect=false"
  }
}
```

### AIBettingAnalyst/appsettings.json
```json
{
  "Redis": {
    "ConnectionString": "localhost:6379,password=RedisAIBet2024!,abortConnect=false"
  }
}
```

---

## ??? Comandi Utili

### Gestione Stack

```powershell
# Avvia
docker-compose -f docker-compose.postgresql.yml up -d

# Ferma (mantiene dati)
docker-compose -f docker-compose.postgresql.yml stop

# Ferma e rimuovi (mantiene dati nei volumi)
docker-compose -f docker-compose.postgresql.yml down

# Riavvia singolo servizio
docker-compose -f docker-compose.postgresql.yml restart redis

# Vedi log in real-time
docker-compose -f docker-compose.postgresql.yml logs -f
```

### Backup

```powershell
# Backup PostgreSQL
docker exec aibetting_postgres pg_dump -U aibetting_user aibetting_db > backup.sql

# Backup Redis
docker exec aibetting_redis redis-cli -a RedisAIBet2024! SAVE
docker cp aibetting_redis:/data/dump.rdb ./redis_backup.rdb
```

---

## ?? Prossimi Passi

1. ? Infrastructure Docker running
2. ?? Configura Betfair API credentials in `AIBettingExplorer/appsettings.json`
3. ?? Implementa strategia trading in `AIBettingAnalyst`
4. ?? Testa ordini con `AIBettingExecutor`
5. ?? Monitora con `AIBettingBlazorDashboard`

---

## ?? Documentazione Completa

- **Guida Docker Completa**: `Documentazione/Docker-Infrastructure-Guide.md`
- **Setup PostgreSQL**: `Documentazione/PostgreSQL-Setup.md`
- **RiskManager**: `Documentazione/RiskManager-Guida.md`
- **Specifiche Progetto**: `Documentazione/Specifiche.md`

---

## ?? Troubleshooting

### Porta già in uso

```powershell
# Cambia porta nel docker-compose.yml
ports:
  - "5433:5432"  # PostgreSQL su 5433
  - "6380:6379"  # Redis su 6380
```

### Container non si avvia

```powershell
# Vedi errori
docker logs aibetting_postgres
docker logs aibetting_redis

# Rimuovi e ricrea
docker-compose -f docker-compose.postgresql.yml down
docker-compose -f docker-compose.postgresql.yml up -d
```

---

## ? Checklist Setup

- [ ] Docker Desktop installato e running
- [ ] `docker-compose.postgresql.yml` presente
- [ ] Stack avviato: `docker ps` mostra 2+ container
- [ ] PostgreSQL accessibile: test con `psql`
- [ ] Redis accessibile: test con `redis-cli`
- [ ] Tabelle DB create: `trades` e `daily_summaries`
- [ ] Connection strings aggiornate
- [ ] Test .NET passati

---

**Ready to trade!** ????

Per supporto, vedi: `Documentazione/Docker-Infrastructure-Guide.md`
