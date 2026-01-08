# ?? AIBetting - Guida Completa Docker Infrastructure

Stack completo: **PostgreSQL + Redis + GUI Tools**

---

## ?? Quick Start (3 Comandi)

```powershell
# 1. Naviga nella directory progetto
cd C:\Users\SMARTW\source\repos\AIBettingSolution

# 2. Avvia PostgreSQL + Redis (senza GUI)
docker-compose -f docker-compose.postgresql.yml up -d

# 3. Verifica
docker ps
```

**Output atteso:**
```
CONTAINER ID   IMAGE                STATUS         PORTS                    NAMES
abc123...      postgres:16-alpine   Up 10 seconds  0.0.0.0:5432->5432/tcp   aibetting_postgres
def456...      redis:7-alpine       Up 10 seconds  0.0.0.0:6379->6379/tcp   aibetting_redis
```

---

## ??? Configurazione Completa

### Opzione 1: Setup Base (Solo DB e Redis)

```powershell
docker-compose -f docker-compose.postgresql.yml up -d
```

### Opzione 2: Setup con GUI Tools

```powershell
# Avvia anche pgAdmin e RedisInsight
docker-compose -f docker-compose.postgresql.yml --profile tools up -d
```

---

## ?? Password Configuration

### Metodo 1: Usa Default (Sviluppo)

Le password di default sono:
- PostgreSQL: `AIBet2024!WinRate`
- Redis: `RedisAIBet2024!`
- pgAdmin: `admin`

### Metodo 2: Personalizza (Consigliato)

```powershell
# Crea file .env nella directory progetto
echo "DB_PASSWORD=TuaPasswordPostgres" > .env
echo "REDIS_PASSWORD=TuaPasswordRedis" >> .env
echo "PGADMIN_PASSWORD=TuaPasswordPgAdmin" >> .env

# Avvia con password personalizzate
docker-compose -f docker-compose.postgresql.yml up -d
```

---

## ?? Connection Strings per .NET

### PostgreSQL

**AIBettingAccounting/appsettings.json:**
```json
{
  "ConnectionStrings": {
    "Accounting": "Host=localhost;Port=5432;Database=aibetting_db;Username=aibetting_user;Password=AIBet2024!WinRate;SslMode=Disable"
  }
}
```

### Redis

**AIBettingExplorer/appsettings.json:**
```json
{
  "Redis": {
    "ConnectionString": "localhost:6379,password=RedisAIBet2024!,abortConnect=false,connectRetry=3,connectTimeout=5000"
  }
}
```

**Nel codice C#:**
```csharp
// Setup Redis
services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = ConfigurationOptions.Parse("localhost:6379");
    config.Password = "RedisAIBet2024!";
    config.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(config);
});
```

---

## ?? Test Connessioni

### Test PostgreSQL

```powershell
# Connetti al container
docker exec -it aibetting_postgres psql -U aibetting_user -d aibetting_db

# Verifica tabelle
\dt

# Output atteso:
#  Schema |      Name        | Type
# --------+------------------+-------
#  public | daily_summaries  | table
#  public | trades           | table

# Esci
\q
```

### Test Redis

```powershell
# Connetti al container
docker exec -it aibetting_redis redis-cli -a RedisAIBet2024!

# Test comandi base
127.0.0.1:6379> PING
PONG

127.0.0.1:6379> SET test:key "AIBetting"
OK

127.0.0.1:6379> GET test:key
"AIBetting"

127.0.0.1:6379> DEL test:key
(integer) 1

# Esci
127.0.0.1:6379> EXIT
```

---

## ??? GUI Tools (Opzionali)

### pgAdmin (PostgreSQL)

1. Avvia con tools: `docker-compose -f docker-compose.postgresql.yml --profile tools up -d`
2. Apri browser: http://localhost:5050
3. Login:
   - Email: `admin@aibetting.com`
   - Password: `admin`
4. Add server:
   - Name: `AIBetting`
   - Host: `postgres` (nome container Docker)
   - Port: `5432`
   - Database: `aibetting_db`
   - Username: `aibetting_user`
   - Password: `AIBet2024!WinRate`

### RedisInsight (Redis)

1. Apri browser: http://localhost:5540
2. "Add Redis Database"
3. Configurazione:
   - Host: `redis` (nome container Docker)
   - Port: `6379`
   - Database Alias: `AIBetting Redis`
   - Password: `RedisAIBet2024!`
4. Test Connection ? Add Database

---

## ??? Comandi Utili

### Gestione Container

```powershell
# Avvia solo PostgreSQL + Redis
docker-compose -f docker-compose.postgresql.yml up -d postgres redis

# Avvia tutto (incluso GUI)
docker-compose -f docker-compose.postgresql.yml --profile tools up -d

# Ferma (mantiene dati)
docker-compose -f docker-compose.postgresql.yml stop

# Ferma e rimuovi container (mantiene volumi/dati)
docker-compose -f docker-compose.postgresql.yml down

# Ferma e ELIMINA TUTTO (inclusi dati!) ??
docker-compose -f docker-compose.postgresql.yml down -v

# Riavvia singolo servizio
docker-compose -f docker-compose.postgresql.yml restart redis

# Vedi log
docker-compose -f docker-compose.postgresql.yml logs -f
docker-compose -f docker-compose.postgresql.yml logs -f redis
```

### Backup & Restore

#### PostgreSQL

```powershell
# Backup database
docker exec aibetting_postgres pg_dump -U aibetting_user aibetting_db > backup_$(Get-Date -Format "yyyyMMdd_HHmmss").sql

# Restore database
Get-Content backup_20240115_143022.sql | docker exec -i aibetting_postgres psql -U aibetting_user -d aibetting_db

# Backup compresso
docker exec aibetting_postgres pg_dump -U aibetting_user aibetting_db | gzip > backup.sql.gz
```

#### Redis

```powershell
# Backup RDB snapshot
docker exec aibetting_redis redis-cli -a RedisAIBet2024! SAVE
docker cp aibetting_redis:/data/dump.rdb ./redis_backup_$(Get-Date -Format "yyyyMMdd").rdb

# Restore (ferma Redis, copia file, riavvia)
docker-compose -f docker-compose.postgresql.yml stop redis
docker cp redis_backup_20240115.rdb aibetting_redis:/data/dump.rdb
docker-compose -f docker-compose.postgresql.yml start redis
```

---

## ?? Monitoring & Troubleshooting

### Check Health Status

```powershell
# Verifica stato salute
docker ps --filter name=aibetting --format "table {{.Names}}\t{{.Status}}"

# Output atteso:
# NAMES                   STATUS
# aibetting_redis         Up 5 minutes (healthy)
# aibetting_postgres      Up 5 minutes (healthy)
```

### View Logs

```powershell
# Log tutti i servizi
docker-compose -f docker-compose.postgresql.yml logs --tail=100

# Log specifico servizio
docker-compose -f docker-compose.postgresql.yml logs -f redis

# Log con timestamp
docker logs --timestamps aibetting_redis
```

### Resource Usage

```powershell
# Verifica uso risorse
docker stats aibetting_postgres aibetting_redis

# Output esempio:
# CONTAINER             CPU %     MEM USAGE / LIMIT     NET I/O
# aibetting_postgres    0.15%     45.2MiB / 7.77GiB     12kB / 8kB
# aibetting_redis       0.08%     12.5MiB / 7.77GiB     8kB / 4kB
```

---

## ?? Troubleshooting Comuni

### Problema: Porta già in uso

```powershell
# Verifica chi usa la porta 5432
netstat -ano | findstr :5432

# Cambia porta nel docker-compose.yml
ports:
  - "5433:5432"  # Usa 5433 invece di 5432

# Aggiorna connection string
"Host=localhost;Port=5433;..."
```

### Problema: Container non si avvia

```powershell
# Vedi errori dettagliati
docker logs aibetting_postgres
docker logs aibetting_redis

# Rimuovi e ricrea
docker-compose -f docker-compose.postgresql.yml down
docker-compose -f docker-compose.postgresql.yml up -d
```

### Problema: Dati persi dopo restart

```powershell
# Verifica che i volumi esistano
docker volume ls | findstr aibetting

# Se mancano, ricrea con volumi espliciti
docker-compose -f docker-compose.postgresql.yml up -d
```

### Problema: Redis connection timeout

```powershell
# Test connessione manuale
docker exec -it aibetting_redis redis-cli -a RedisAIBet2024! PING

# Se fallisce, riavvia Redis
docker-compose -f docker-compose.postgresql.yml restart redis

# Verifica firewall Windows non blocchi localhost:6379
```

---

## ?? Test Completo End-to-End

### Step 1: Avvia Infrastructure

```powershell
docker-compose -f docker-compose.postgresql.yml up -d
```

### Step 2: Test PostgreSQL

```powershell
docker exec -it aibetting_postgres psql -U aibetting_user -d aibetting_db -c "SELECT COUNT(*) FROM trades;"
# Output: 0 (database vuoto, corretto)
```

### Step 3: Test Redis

```powershell
docker exec -it aibetting_redis redis-cli -a RedisAIBet2024! PING
# Output: PONG
```

### Step 4: Test da .NET

```powershell
cd AIBettingAccounting
dotnet run

# Output atteso:
# ? Connessione PostgreSQL verificata
# ? Tabelle presenti: trades, daily_summaries
```

```powershell
cd AIBettingExplorer
dotnet run

# Output atteso:
# ? Redis connesso
# ? Listening to Betfair stream...
```

---

## ?? Deploy Produzione (VPS Ubuntu)

```sh
# Su VPS Ubuntu
git clone https://github.com/diegolista673/AIBetting
cd AIBetting

# Crea .env con password produzione
cat > .env <<EOF
DB_PASSWORD=ProductionPasswordSecure123!
REDIS_PASSWORD=RedisProductionPassword456!
PGADMIN_PASSWORD=AdminSecurePassword789!
EOF

# Avvia stack
docker-compose -f docker-compose.postgresql.yml up -d

# Verifica
docker ps
docker-compose -f docker-compose.postgresql.yml logs

# Setup backup automatico (cron)
crontab -e
# Aggiungi:
0 2 * * * docker exec aibetting_postgres pg_dump -U aibetting_user aibetting_db | gzip > /backup/aibetting_$(date +\%Y\%m\%d).sql.gz
```

---

## ?? Configurazione Consigliata per AIBetting

### Sviluppo Locale (Windows)

```powershell
# Solo DB + Redis (niente GUI per risparmiare RAM)
docker-compose -f docker-compose.postgresql.yml up -d postgres redis
```

**Connection Strings:**
- PostgreSQL: `Host=localhost;Port=5432;...`
- Redis: `localhost:6379,password=RedisAIBet2024!`

### Testing/CI

```powershell
# Stack completo per test automatici
docker-compose -f docker-compose.postgresql.yml up -d
dotnet test
docker-compose -f docker-compose.postgresql.yml down -v
```

### Produzione (VPS)

```sh
# Stack ottimizzato produzione (no GUI)
docker-compose -f docker-compose.postgresql.yml up -d postgres redis

# Con monitoring esterno (Prometheus/Grafana)
# Con backup automatico su S3/storage remoto
```

---

## ?? Performance Tips

### PostgreSQL

```sh
# Aumenta shared_buffers (se RAM > 4GB)
docker exec -it aibetting_postgres bash
echo "shared_buffers = 256MB" >> /var/lib/postgresql/data/postgresql.conf
docker-compose -f docker-compose.postgresql.yml restart postgres
```

### Redis

```sh
# Abilita RDB + AOF persistence
docker exec -it aibetting_redis redis-cli -a RedisAIBet2024! CONFIG SET save "900 1 300 10 60 10000"
docker exec -it aibetting_redis redis-cli -a RedisAIBet2024! CONFIG SET appendonly yes
```

---

## ? Checklist Setup Completo

- [ ] Docker Desktop installato
- [ ] File `docker-compose.postgresql.yml` presente
- [ ] `.env` creato con password (opzionale)
- [ ] Avviato stack: `docker-compose up -d`
- [ ] PostgreSQL raggiungibile: `docker exec ... psql`
- [ ] Redis raggiungibile: `docker exec ... redis-cli`
- [ ] Tabelle DB create: `\dt` mostra `trades` e `daily_summaries`
- [ ] Connection string aggiornate in `appsettings.json`
- [ ] Test .NET funzionante: `dotnet run` in AIBettingAccounting
- [ ] Backup automatico configurato (produzione)

---

## ?? Congratulazioni!

Hai ora una **infrastruttura production-ready** per AIBetting:

? PostgreSQL per persistenza  
? Redis per cache e pub/sub  
? GUI tools per debugging  
? Backup/restore pronti  
? Stesso setup dev/prod  

**Prossimo step**: Integra AIBettingExplorer e AIBettingAnalyst con Redis! ??

---

**Versione**: 1.0.0  
**Ultima Modifica**: 2024-01-15  
**Autore**: Diego Lista
