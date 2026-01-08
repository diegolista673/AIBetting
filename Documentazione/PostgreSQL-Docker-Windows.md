# AIBetting - Guida Docker PostgreSQL per Windows

## ?? Prerequisiti

- **Docker Desktop** installato: https://www.docker.com/products/docker-desktop/

---

## ?? Quick Start

### Step 1: Avvia PostgreSQL

```powershell
# Naviga nella directory del progetto
cd C:\Users\SMARTW\source\repos\AIBettingSolution

# Imposta password (opzionale, default: aibetting_password_change_me)
$env:DB_PASSWORD = "tua_password_sicura"

# Avvia container
docker-compose -f docker-compose.postgresql.yml up -d

# Verifica che sia in esecuzione
docker ps
```

**Output atteso:**
```
CONTAINER ID   IMAGE              STATUS         PORTS                    NAMES
abc123...      postgres:16-alpine Up 10 seconds  0.0.0.0:5432->5432/tcp   aibetting_postgres
```

### Step 2: Verifica Connessione

```powershell
# Connetti al container
docker exec -it aibetting_postgres psql -U aibetting_user -d aibetting_db

# Dentro psql, verifica tabelle
\dt

# Dovresti vedere:
#  Schema |      Name        | Type  
# --------+------------------+-------
#  public | daily_summaries  | table
#  public | trades           | table

# Esci
\q
```

### Step 3: Aggiorna Connection String

Il file `AIBettingAccounting\appsettings.json` dovrebbe avere:

```json
{
  "ConnectionStrings": {
    "Accounting": "Host=localhost;Port=5432;Database=aibetting_db;Username=aibetting_user;Password=tua_password_sicura;SslMode=Disable"
  }
}
```

### Step 4: Test da .NET

```powershell
cd AIBettingAccounting
dotnet run
```

---

## ?? pgAdmin (Interfaccia Web Opzionale)

Se hai avviato anche pgAdmin nel docker-compose:

1. Apri browser: http://localhost:5050
2. Login:
   - Email: `admin@aibetting.local`
   - Password: `admin` (default)
3. Aggiungi server:
   - Host: `postgres` (nome del container)
   - Port: `5432`
   - Database: `aibetting_db`
   - Username: `aibetting_user`
   - Password: (quella impostata)

---

## ??? Comandi Utili

### Gestione Container

```powershell
# Avvia
docker-compose -f docker-compose.postgresql.yml up -d

# Ferma (mantiene dati)
docker-compose -f docker-compose.postgresql.yml stop

# Ferma e rimuovi (ATTENZIONE: perde dati se non hai volume)
docker-compose -f docker-compose.postgresql.yml down

# Vedi log
docker-compose -f docker-compose.postgresql.yml logs -f postgres
```

### Backup/Restore

```powershell
# Backup
docker exec aibetting_postgres pg_dump -U aibetting_user aibetting_db > backup.sql

# Restore
Get-Content backup.sql | docker exec -i aibetting_postgres psql -U aibetting_user -d aibetting_db
```

### Esegui Query

```powershell
# Query singola
docker exec aibetting_postgres psql -U aibetting_user -d aibetting_db -c "SELECT COUNT(*) FROM trades;"

# Esegui file SQL
docker exec -i aibetting_postgres psql -U aibetting_user -d aibetting_db < Documentazione\database-schema.sql
```

---

## ?? Configurazione Avanzata

### Cambia Password

Modifica `docker-compose.postgresql.yml` o usa variabile ambiente:

```powershell
# Windows PowerShell
$env:DB_PASSWORD = "nuova_password_super_sicura"
docker-compose -f docker-compose.postgresql.yml up -d
```

### Persisti Dati su Directory Locale

Modifica `docker-compose.postgresql.yml`:

```yaml
volumes:
  - C:\Users\SMARTW\postgres-data:/var/lib/postgresql/data
  - ./Documentazione/database-schema.sql:/docker-entrypoint-initdb.d/schema.sql:ro
```

---

## ? Rimuovi Tutto e Ricomincia

```powershell
# Ferma e rimuovi container + volume
docker-compose -f docker-compose.postgresql.yml down -v

# Riavvia da zero
docker-compose -f docker-compose.postgresql.yml up -d
```

---

## ? Vantaggi Docker

| Feature | Docker | Installazione Nativa |
|---------|--------|---------------------|
| Setup time | < 2 min | 10-20 min |
| Isolamento | ? Totale | ? Modifica sistema |
| Pulizia | `docker-compose down` | Disinstallazione complessa |
| Multi-versione | ? Container separati | ? Conflitti |
| Cross-platform | ? Stesso file | ? Script diversi |

---

## ?? Troubleshooting

### Porta 5432 già in uso

```powershell
# Trova processo che usa la porta
netstat -ano | findstr :5432

# Termina processo (sostituisci PID)
taskkill /PID <pid> /F

# O cambia porta in docker-compose.yml
ports:
  - "5433:5432"  # Usa 5433 invece
```

### Container non si avvia

```powershell
# Vedi errori dettagliati
docker logs aibetting_postgres

# Riavvia container
docker restart aibetting_postgres
```

### Non si connette da .NET

```powershell
# Verifica che container sia "healthy"
docker ps --filter name=aibetting_postgres

# Testa connessione manuale
docker exec aibetting_postgres pg_isready -U aibetting_user
```

---

**Consiglio**: Usa Docker per sviluppo, è più veloce e pulito! ??
