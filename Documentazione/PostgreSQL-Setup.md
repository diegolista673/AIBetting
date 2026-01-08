# ?? Guida Installazione PostgreSQL per AIBetting

Guida completa per installare PostgreSQL su Ubuntu e connettere il progetto AIBettingAccounting.

---

## ?? PARTE 1: Installazione PostgreSQL su Ubuntu

### Step 1: Aggiorna Sistema

```bash
sudo apt update && sudo apt upgrade -y
```

### Step 2: Installa PostgreSQL

```bash
# Installa PostgreSQL e contrib
sudo apt install postgresql postgresql-contrib -y

# Verifica versione installata
psql --version
# Output atteso: psql (PostgreSQL) 14.x o superiore
```

### Step 3: Avvia e Abilita Servizio

```bash
# Verifica stato servizio
sudo systemctl status postgresql

# Avvia servizio (se non attivo)
sudo systemctl start postgresql

# Abilita avvio automatico al boot
sudo systemctl enable postgresql

# Verifica che sia in ascolto sulla porta 5432
sudo netstat -tuln | grep 5432
# Output: tcp 0.0.0.0:5432 LISTEN
```

---

## ?? PARTE 2: Configurazione Database

### Step 1: Accedi a PostgreSQL

```bash
# Entra come utente postgres (superuser di sistema)
sudo -u postgres psql
```

### Step 2: Crea Database e Utente

```sql
-- Crea database dedicato
CREATE DATABASE aibetting_db
    WITH 
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.UTF-8'
    LC_CTYPE = 'en_US.UTF-8'
    TEMPLATE = template0;

-- Crea utente applicativo
CREATE USER aibetting_user WITH PASSWORD 'tua_password_sicura_qui';

-- Assegna privilegi database
GRANT ALL PRIVILEGES ON DATABASE aibetting_db TO aibetting_user;

-- Connetti al database
\c aibetting_db

-- PostgreSQL 15+ richiede grant su schema public
GRANT ALL ON SCHEMA public TO aibetting_user;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO aibetting_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO aibetting_user;

-- Verifica creazione
\l aibetting_db
\du aibetting_user

-- Esci
\q
```

### Step 3: Imposta Password per Utente postgres (opzionale)

```sql
-- Entra di nuovo come postgres
sudo -u postgres psql

-- Imposta password
ALTER USER postgres WITH PASSWORD 'password_postgres_sicura';

-- Esci
\q
```

---

## ?? PARTE 3: Configurazione Accesso Remoto (Opzionale)

### Step 1: Modifica postgresql.conf

```bash
# Trova file di configurazione
sudo find /etc/postgresql -name postgresql.conf

# Modifica (usa versione corretta, es. 14)
sudo nano /etc/postgresql/14/main/postgresql.conf
```

Cerca e modifica:
```conf
# Da:
#listen_addresses = 'localhost'

# A (per accettare connessioni da qualsiasi IP):
listen_addresses = '*'

# O specifica IP del tuo server applicativo:
listen_addresses = '192.168.1.100,localhost'
```

### Step 2: Modifica pg_hba.conf

```bash
sudo nano /etc/postgresql/14/main/pg_hba.conf
```

Aggiungi alla fine del file:

```conf
# TYPE  DATABASE        USER            ADDRESS                 METHOD

# Per sviluppo locale (permetti da tutta la rete locale)
host    aibetting_db    aibetting_user  192.168.0.0/16         md5

# Per produzione (solo IP specifico del server app)
host    aibetting_db    aibetting_user  192.168.1.100/32       md5

# Per accesso da Docker containers sulla stessa macchina
host    aibetting_db    aibetting_user  172.17.0.0/16          md5

# ATTENZIONE: MAI usare questo in produzione!
# host    all             all             0.0.0.0/0              md5
```

### Step 3: Riavvia PostgreSQL

```bash
sudo systemctl restart postgresql

# Verifica che abbia riavviato senza errori
sudo systemctl status postgresql
```

### Step 4: Configura Firewall (UFW)

```bash
# Verifica stato firewall
sudo ufw status

# Se attivo, permetti PostgreSQL
sudo ufw allow 5432/tcp

# Specifica IP sorgente (più sicuro)
sudo ufw allow from 192.168.1.100 to any port 5432

# Ricarica firewall
sudo ufw reload

# Verifica regole
sudo ufw status numbered
```

---

## ??? PARTE 4: Crea Schema Database

### Step 1: Esegui Script SQL

```bash
# Opzione 1: Esegui da file
psql -h localhost -U aibetting_user -d aibetting_db -f Documentazione/database-schema.sql -W

# Opzione 2: Copia-incolla manuale
psql -h localhost -U aibetting_user -d aibetting_db -W
```

### Step 2: Verifica Tabelle Create

```sql
-- Lista tabelle
\dt

-- Output atteso:
--  Schema |      Name        | Type  |     Owner      
-- --------+------------------+-------+----------------
--  public | daily_summaries  | table | aibetting_user
--  public | trades           | table | aibetting_user

-- Descrivi struttura tabella trades
\d trades

-- Verifica indici
\di

-- Verifica views
\dv

-- Testa view
SELECT * FROM v_trading_stats;
```

---

## ?? PARTE 5: Configura Progetto .NET

### Step 1: Aggiorna Connection String

Modifica `AIBettingAccounting/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Accounting": "Host=localhost;Port=5432;Database=aibetting_db;Username=aibetting_user;Password=tua_password_sicura_qui;SslMode=Prefer;Trust Server Certificate=true"
  }
}
```

#### Connection String per Ambienti Diversi

```json
// Sviluppo locale
"Accounting": "Host=localhost;Port=5432;Database=aibetting_db;Username=aibetting_user;Password=dev_password;SslMode=Disable"

// Server remoto (Ubuntu VPS)
"Accounting": "Host=192.168.1.100;Port=5432;Database=aibetting_db;Username=aibetting_user;Password=prod_password;SslMode=Require"

// Docker container sulla stessa rete
"Accounting": "Host=postgres_container;Port=5432;Database=aibetting_db;Username=aibetting_user;Password=docker_password;SslMode=Disable"
```

### Step 2: Test Connessione da .NET

```bash
# Naviga nella directory AIBettingAccounting
cd AIBettingAccounting

# Esegui test connessione
dotnet run --project DatabaseConnectionTest.cs
```

**Output Atteso:**
```
?? AIBetting - Test Connessione PostgreSQL
===========================================

?? Connection String: Host=localhost;Port=5432;Database=aibetting_db;Username=aibetting_user;Password=***

?? Test 1: Verifica connessione...
? Connessione stabilita con successo!

?? Test 2: Verifica schema database...
? Tabella 'trades': PRESENTE
? Tabella 'daily_summaries': PRESENTE

?? Test 3: Conta records esistenti...
?? Trades presenti: 0
?? Daily summaries presenti: 0

?? Test 4: Inserimento record di test...
? Record inserito con ID: 12345678-abcd-...

?? Test 5: Verifica lettura record...
? Record letto correttamente

?? Test 6: Pulizia record di test...
? Record di test rimosso

==================================================
? TUTTI I TEST COMPLETATI CON SUCCESSO!
==================================================
```

---

## ?? PARTE 6: Test Manuale SQL

### Inserisci Trade di Test

```sql
-- Connetti
psql -h localhost -U aibetting_user -d aibetting_db

-- Inserisci trade
INSERT INTO trades (
    id, timestamp, market_id, selection_id, stake, odds, 
    type, status, commission, profit_loss, net_profit
) VALUES (
    gen_random_uuid(),
    NOW(),
    '1.234567890',
    '12345',
    100.00,
    2.50,
    'BACK',
    'MATCHED',
    5.00,
    150.00,
    145.00
);

-- Verifica inserimento
SELECT * FROM trades ORDER BY created_at DESC LIMIT 5;

-- Controlla daily summary (viene aggiornato automaticamente dal trigger)
SELECT * FROM daily_summaries ORDER BY date DESC;

-- Verifica view statistiche
SELECT * FROM v_trading_stats;

-- Verifica view P&L giornaliero
SELECT * FROM v_daily_pnl;
```

---

## ??? PARTE 7: Operazioni Comuni

### Backup Database

```bash
# Backup completo
pg_dump -h localhost -U aibetting_user -d aibetting_db > backup_$(date +%Y%m%d).sql

# Backup solo schema (senza dati)
pg_dump -h localhost -U aibetting_user -d aibetting_db --schema-only > schema_backup.sql

# Backup solo dati
pg_dump -h localhost -U aibetting_user -d aibetting_db --data-only > data_backup.sql
```

### Ripristina Database

```bash
# Ripristina da backup
psql -h localhost -U aibetting_user -d aibetting_db < backup_20240115.sql
```

### Reset Database (ATTENZIONE!)

```sql
-- Elimina tutte le tabelle
DROP TABLE IF EXISTS trades CASCADE;
DROP TABLE IF EXISTS daily_summaries CASCADE;

-- Ri-esegui script schema
\i Documentazione/database-schema.sql
```

### Monitoring

```sql
-- Connessioni attive
SELECT * FROM pg_stat_activity WHERE datname = 'aibetting_db';

-- Dimensione database
SELECT pg_size_pretty(pg_database_size('aibetting_db'));

-- Dimensione tabelle
SELECT 
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;

-- Statistiche query lente
SELECT * FROM pg_stat_statements ORDER BY mean_exec_time DESC LIMIT 10;
```

---

## ?? Troubleshooting

### Problema: "psql: error: connection to server failed"

```bash
# Verifica che PostgreSQL sia in esecuzione
sudo systemctl status postgresql

# Controlla log per errori
sudo tail -f /var/log/postgresql/postgresql-14-main.log
```

### Problema: "FATAL: password authentication failed"

```bash
# Resetta password utente
sudo -u postgres psql
ALTER USER aibetting_user WITH PASSWORD 'nuova_password';
\q

# Verifica pg_hba.conf (metodo autenticazione)
sudo nano /etc/postgresql/14/main/pg_hba.conf
# Assicurati che sia "md5" o "scram-sha-256", non "peer"

# Riavvia
sudo systemctl restart postgresql
```

### Problema: "Connection refused on port 5432"

```bash
# Verifica che PostgreSQL sia in ascolto
sudo netstat -tuln | grep 5432

# Controlla listen_addresses in postgresql.conf
sudo grep listen_addresses /etc/postgresql/14/main/postgresql.conf

# Verifica firewall
sudo ufw status | grep 5432
```

### Problema: .NET non si connette da Windows a Ubuntu

1. Verifica IP Ubuntu: `ip addr show`
2. Testa connessione: `telnet <ubuntu_ip> 5432`
3. Controlla `pg_hba.conf` permetta l'IP Windows
4. Verifica firewall Ubuntu: `sudo ufw allow from <windows_ip> to any port 5432`

---

## ?? Performance Tuning (Opzionale)

### Ottimizzazioni postgresql.conf

```bash
sudo nano /etc/postgresql/14/main/postgresql.conf
```

```conf
# Memoria (per server con 8GB RAM)
shared_buffers = 2GB
effective_cache_size = 6GB
maintenance_work_mem = 512MB
work_mem = 64MB

# Checkpoint
checkpoint_completion_target = 0.9
wal_buffers = 16MB
default_statistics_target = 100

# Query planner
random_page_cost = 1.1  # Per SSD
effective_io_concurrency = 200

# Logging (per debugging)
log_min_duration_statement = 1000  # Log query > 1sec
log_line_prefix = '%t [%p]: [%l-1] user=%u,db=%d,app=%a,client=%h '
log_checkpoints = on
log_connections = on
log_disconnections = on
log_lock_waits = on
```

Dopo modifiche:
```bash
sudo systemctl restart postgresql
```

---

## ? Checklist Finale

- [ ] PostgreSQL installato e in esecuzione
- [ ] Database `aibetting_db` creato
- [ ] Utente `aibetting_user` creato con privilegi
- [ ] Schema (tabelle, indici, views, triggers) creato
- [ ] Connection string aggiornata in `appsettings.json`
- [ ] Test connessione .NET passato
- [ ] Backup automatico schedulato (cron job)
- [ ] Firewall configurato correttamente
- [ ] Accesso remoto testato (se necessario)

---

## ?? Risorse Utili

- **Documentazione PostgreSQL**: https://www.postgresql.org/docs/
- **EF Core con PostgreSQL**: https://www.npgsql.org/efcore/
- **pgAdmin (GUI)**: https://www.pgadmin.org/
- **DBeaver (GUI alternativa)**: https://dbeaver.io/

---

**Ultima Modifica**: 2024-01-15  
**Autore**: Diego Lista  
**Versione**: 1.0.0
