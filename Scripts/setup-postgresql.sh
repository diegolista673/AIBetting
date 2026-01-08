#!/bin/bash

# AIBetting - PostgreSQL Quick Setup Script
# Per Ubuntu 20.04/22.04/24.04

set -e  # Exit on error

echo "?? AIBetting - Installazione PostgreSQL"
echo "========================================"
echo ""

# Colori
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Funzioni helper
print_success() {
    echo -e "${GREEN}? $1${NC}"
}

print_error() {
    echo -e "${RED}? $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}??  $1${NC}"
}

# Step 1: Verifica privilegi root
if [ "$EUID" -ne 0 ]; then 
    print_error "Questo script deve essere eseguito come root (usa sudo)"
    exit 1
fi

# Step 2: Aggiorna sistema
echo "?? Step 1/7: Aggiornamento sistema..."
apt update -qq && apt upgrade -y -qq
print_success "Sistema aggiornato"

# Step 3: Installa PostgreSQL
echo ""
echo "?? Step 2/7: Installazione PostgreSQL..."
if command -v psql &> /dev/null; then
    print_warning "PostgreSQL già installato ($(psql --version | cut -d' ' -f3))"
else
    apt install -y -qq postgresql postgresql-contrib
    print_success "PostgreSQL installato"
fi

# Step 4: Avvia servizio
echo ""
echo "?? Step 3/7: Avvio servizio PostgreSQL..."
systemctl start postgresql
systemctl enable postgresql
print_success "PostgreSQL avviato e abilitato al boot"

# Step 5: Configura database
echo ""
echo "???  Step 4/7: Creazione database e utente..."

# Chiedi password
read -sp "Inserisci password per utente 'aibetting_user': " DB_PASSWORD
echo ""
read -sp "Conferma password: " DB_PASSWORD_CONFIRM
echo ""

if [ "$DB_PASSWORD" != "$DB_PASSWORD_CONFIRM" ]; then
    print_error "Le password non corrispondono!"
    exit 1
fi

# Crea database e utente
sudo -u postgres psql <<EOF
-- Crea database con locale di default
CREATE DATABASE aibetting_db
    WITH 
    OWNER = postgres
    ENCODING = 'UTF8'
    TEMPLATE = template0;

-- Crea utente
CREATE USER aibetting_user WITH PASSWORD '$DB_PASSWORD';

-- Assegna privilegi
GRANT ALL PRIVILEGES ON DATABASE aibetting_db TO aibetting_user;

-- Connetti al database
\c aibetting_db

-- Grant su schema public (PostgreSQL 15+)
GRANT ALL ON SCHEMA public TO aibetting_user;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO aibetting_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO aibetting_user;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA public TO aibetting_user;
EOF

print_success "Database 'aibetting_db' creato"
print_success "Utente 'aibetting_user' creato"

# Step 6: Configura accesso remoto (opzionale)
echo ""
read -p "Vuoi abilitare accesso remoto al database? (y/n): " -n 1 -r
echo ""
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo "?? Step 5/7: Configurazione accesso remoto..."
    
    # Trova versione PostgreSQL
    PG_VERSION=$(psql --version | grep -oP '\d+' | head -1)
    PG_CONF="/etc/postgresql/$PG_VERSION/main/postgresql.conf"
    PG_HBA="/etc/postgresql/$PG_VERSION/main/pg_hba.conf"
    
    # Modifica postgresql.conf
    if grep -q "^listen_addresses" "$PG_CONF"; then
        sed -i "s/^listen_addresses.*/listen_addresses = '*'/" "$PG_CONF"
    else
        echo "listen_addresses = '*'" >> "$PG_CONF"
    fi
    
    # Modifica pg_hba.conf
    read -p "Inserisci network CIDR consentito (es. 192.168.1.0/24 o 0.0.0.0/0 per tutti): " NETWORK
    echo "host    aibetting_db    aibetting_user    $NETWORK    md5" >> "$PG_HBA"
    
    # Riavvia PostgreSQL
    systemctl restart postgresql
    
    print_success "Accesso remoto configurato per $NETWORK"
else
    echo "??  Step 5/7: Accesso remoto non configurato"
fi

# Step 7: Configura firewall
echo ""
if command -v ufw &> /dev/null && ufw status | grep -q "Status: active"; then
    read -p "Vuoi aprire porta PostgreSQL (5432) sul firewall? (y/n): " -n 1 -r
    echo ""
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        echo "?? Step 6/7: Configurazione firewall..."
        ufw allow 5432/tcp
        ufw reload
        print_success "Porta 5432 aperta sul firewall"
    else
        print_warning "Porta 5432 NON aperta sul firewall (configuralo manualmente)"
    fi
else
    print_warning "UFW non attivo o non installato - skip configurazione firewall"
fi

# Step 8: Verifica installazione
echo ""
echo "?? Step 7/7: Verifica installazione..."

# Test connessione
if PGPASSWORD=$DB_PASSWORD psql -h localhost -U aibetting_user -d aibetting_db -c "SELECT version();" > /dev/null 2>&1; then
    print_success "Connessione al database verificata"
else
    print_error "Impossibile connettersi al database"
    exit 1
fi

# Genera connection string
CONNECTION_STRING="Host=localhost;Port=5432;Database=aibetting_db;Username=aibetting_user;Password=$DB_PASSWORD;SslMode=Prefer"

# Output finale
echo ""
echo "========================================"
echo -e "${GREEN}? INSTALLAZIONE COMPLETATA!${NC}"
echo "========================================"
echo ""
echo "?? Informazioni Database:"
echo "   Host: localhost"
echo "   Port: 5432"
echo "   Database: aibetting_db"
echo "   Username: aibetting_user"
echo ""
echo "?? Connection String per appsettings.json:"
echo "   \"Accounting\": \"$CONNECTION_STRING\""
echo ""
echo "?? Prossimi passi:"
echo "   1. Aggiorna AIBettingAccounting/appsettings.json con la connection string sopra"
echo "   2. Esegui lo script SQL: psql -h localhost -U aibetting_user -d aibetting_db -f Documentazione/database-schema.sql"
echo "   3. Testa connessione: cd AIBettingAccounting && dotnet run"
echo ""
echo "?? Comandi utili:"
echo "   Connetti: psql -h localhost -U aibetting_user -d aibetting_db"
echo "   Backup: pg_dump -h localhost -U aibetting_user -d aibetting_db > backup.sql"
echo "   Status: sudo systemctl status postgresql"
echo ""
