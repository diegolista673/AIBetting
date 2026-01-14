# ?? AIBetting - Start Immediato

## ? Setup in 3 Comandi (2 minuti)

```powershell
# 1. Avvia infrastructure (PostgreSQL + Redis)
docker-compose up -d

# 2. Verifica che tutto sia running
docker ps

# 3. Test connessione
docker exec -it aibetting_postgres psql -U aibetting_user -d aibetting_db -c "\dt"
docker exec -it aibetting_redis redis-cli -a RedisAIBet2024! PING
```

**Output atteso Step 2:**
```
CONTAINER ID   IMAGE                STATUS         PORTS
abc123...      postgres:16-alpine   Up 30 seconds  0.0.0.0:5432->5432/tcp
def456...      redis:7-alpine       Up 30 seconds  0.0.0.0:6379->6379/tcp
```

**Output atteso Step 3:**
```
 Schema |      Name        | Type
--------+------------------+-------
 public | daily_summaries  | table
 public | trades           | table

PONG
```

? **Fatto!** Infrastructure pronta.

---

## ?? Credenziali Default

| Servizio | Credenziale | Valore |
|----------|-------------|--------|
| PostgreSQL | Host | `localhost:5432` |
| | Database | `aibetting_db` |
| | Username | `aibetting_user` |
| | Password | `AIBet2024!WinRate` |
| Redis | Host | `localhost:6379` |
| | Password | `RedisAIBet2024!` |
| **pgAdmin** | Email | `admin@aibetting.com` |
| | Password | `admin` |
| | URL | http://localhost:5050 |

---

## ?? Personalizza Password (Opzionale)

```powershell
# Crea file .env
Copy-Item .env.example .env

# Modifica .env con tue password
notepad .env

# Riavvia stack
docker-compose down
docker-compose up -d
```

---

## ?? Test Progetti .NET

### Test AIBettingAccounting (PostgreSQL)
```powershell
cd AIBettingAccounting
dotnet run
```

### Test AIBettingExplorer (Redis)
```powershell
cd AIBettingExplorer
dotnet run
```

---

## ?? GUI Tools (Opzionali)

```powershell
# Avvia anche pgAdmin e RedisInsight
docker-compose --profile tools up -d
```

Poi apri:
- **pgAdmin**: http://localhost:5050
- **RedisInsight**: http://localhost:5540

---

## ??? Comandi Utili

```powershell
# Avvia
docker-compose up -d

# Ferma (mantiene dati)
docker-compose stop

# Ferma e rimuovi (mantiene dati)
docker-compose down

# Vedi log
docker-compose logs -f

# Backup database
docker exec aibetting_postgres pg_dump -U aibetting_user aibetting_db > backup.sql
```

---

## ?? Documentazione Completa

- **Setup dettagliato**: `Documentazione/Docker-Infrastructure-Guide.md`
- **Quick start**: `DOCKER-QUICKSTART.md`
- **Specifiche progetto**: `Documentazione/Specifiche.md`
- **RiskManager**: `Documentazione/RiskManager-Guida.md`

---

## ? Checklist

- [ ] Docker Desktop installato
- [ ] `docker-compose up -d` eseguito
- [ ] `docker ps` mostra 2 container running
- [ ] PostgreSQL accessibile
- [ ] Redis accessibile
- [ ] Tabelle DB create

**Ready!** Ora puoi sviluppare AIBetting. ??

---

**Problemi?** Vedi: `Documentazione/Docker-Infrastructure-Guide.md` ? Sezione Troubleshooting
