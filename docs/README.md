# AIBetting Solution

**Automated AI-powered betting system for Betfair markets with real-time monitoring and risk management.**

## 📋 Overview

AIBetting is a complete trading automation platform consisting of:
- **Real-time data ingestion** from Betfair Stream API
- **Multi-strategy analysis** with AI/ML components
- **Automated order execution** with comprehensive risk management
- **Full observability** with Prometheus/Grafana monitoring
- **Blazor dashboard** for real-time system visualization

## 🏗️ Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                     AIBetting Platform                        │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌────────────┐    ┌────────────┐    ┌────────────┐        │
│  │  Explorer  │───▶│  Analyst   │───▶│  Executor  │        │
│  │            │    │            │    │            │        │
│  │ Data       │    │ Signal     │    │ Order      │        │
│  │ Ingestion  │    │ Generation │    │ Execution  │        │
│  └────────────┘    └────────────┘    └────────────┘        │
│       │                  │                  │               │
│       └──────────────────┼──────────────────┘               │
│                          ▼                                   │
│                   ┌─────────────┐                           │
│                   │    Redis    │                           │
│                   │  (Message   │                           │
│                   │   Bus +     │                           │
│                   │   Cache)    │                           │
│                   └─────────────┘                           │
│                          │                                   │
│       ┌──────────────────┼──────────────────┐              │
│       ▼                  ▼                  ▼              │
│  ┌────────────┐    ┌────────────┐    ┌────────────┐      │
│  │ Prometheus │    │PostgreSQL  │    │  Grafana   │      │
│  │  Metrics   │    │Accounting  │    │Dashboards  │      │
│  └────────────┘    └────────────┘    └────────────┘      │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## 📦 Projects

### Core Components

| Project | Purpose | Port |
|---------|---------|------|
| **AIBettingCore** | Shared models, interfaces, services | - |
| **AIBettingExplorer** | Betfair data ingestion | 5001 |
| **AIBettingAnalyst** | Signal generation & strategies | 5002 |
| **AIBettingExecutor** | Order execution & risk management | 5003 |
| **AIBettingAccounting** | Trade logging & P&L tracking | - |
| **AIBettingBlazorDashboard** | Web UI for monitoring | 5000 |

### Infrastructure

| Service | Purpose | Port |
|---------|---------|------|
| **Redis** | Message bus & caching | 16379 |
| **PostgreSQL** | Trade & accounting database | 15432 |
| **Prometheus** | Metrics collection | 9090 |
| **Grafana** | Visualization & dashboards | 3000 |
| **Alertmanager** | Alert routing | 9093 |

## 🚀 Quick Start

### Prerequisites
- .NET 10 SDK
- Docker Desktop
- Redis (included in Docker stack)
- PostgreSQL (included in Docker stack)

### 1. Start Infrastructure

```bash
cd docker
docker compose up -d
```

This starts: Prometheus, Grafana, Redis, PostgreSQL, and all exporters.

### 2. Configure Applications

All applications use `appsettings.json`. Update Redis/PostgreSQL connection strings if needed:

```json
{
  "Redis": {
    "ConnectionString": "localhost:16379"
  }
}
```

### 3. Run Applications

**Option A: Run individually**
```bash
# Terminal 1
cd AIBettingExplorer
dotnet run

# Terminal 2
cd AIBettingAnalyst
dotnet run

# Terminal 3
cd AIBettingExecutor
dotnet run
```

**Option B: Use Visual Studio**
- Set multiple startup projects
- Select Explorer, Analyst, Executor
- Press F5

### 4. Access Dashboards

- **Grafana**: http://localhost:3000 (admin/admin)
- **Prometheus**: http://localhost:9090
- **Blazor Dashboard**: http://localhost:5000 (when running)

## 📊 Monitoring

### Grafana Dashboards

Pre-configured dashboards available in `grafana/dashboards/`:
- **AIBetting Analyst** - Signal generation metrics
- **AIBetting Executor** - Order execution performance (to be created)
- **Infrastructure** - Redis, PostgreSQL, system metrics

### Key Metrics

**Analyst:**
- `aibetting_analyst_snapshots_processed_total` - Market snapshots analyzed
- `aibetting_analyst_signals_generated_total` - Signals generated by strategy
- `aibetting_analyst_strategy_avg_confidence` - Strategy confidence levels

**Executor:**
- `aibetting_executor_orders_placed_total` - Orders placed
- `aibetting_executor_orders_matched_total` - Orders matched
- `aibetting_executor_circuit_breaker_status` - Circuit breaker state
- `aibetting_executor_account_balance` - Current balance

**Explorer:**
- `aibetting_price_updates_total` - Price updates received
- `aibetting_processing_latency_seconds` - Data processing latency

See `prometheus/README.md` for complete query reference.

## 🎯 Strategies

The Analyst implements multiple trading strategies:

### 1. Scalping Strategy
**Strategia di Scalping - Trading Rapido su Momentum**

- **Quick in-and-out trades based on momentum**
  - *Operazioni veloci di entrata/uscita basate sul momentum del mercato*
  - Sfrutta i movimenti di prezzo di breve termine (tick-by-tick)
  - Obiettivo: catturare piccoli profitti ripetutamente

- **Targets high-liquidity markets**
  - *Punta ai mercati con alta liquidità*
  - Richiede volumi elevati per garantire esecuzione rapida
  - Spread ridotto tra back e lay per minimizzare i costi

- **Min confidence: 0.6**
  - *Confidenza minima richiesta: 60%*
  - Soglia relativamente bassa per permettere maggiore frequenza di trade
  - Compensata dal basso rischio per operazione

**Come funziona:** Monitora velocità di movimento dei prezzi (velocity), accelerazione e liquidità disponibile. 
                   Quando il momentum supera la soglia, genera segnale di entrata con target di profitto e stop loss predefiniti.

---

### 2. Steam Move Strategy
**Strategia Steam Move - Rilevamento Movimenti Improvvisi**

- **Detects rapid price movements (steam)**
  - *Rileva movimenti rapidi di prezzo causati da grosse puntate*
  - "Steam" = denaro improvviso che entra nel mercato (spesso da insider)
  - Identifica quando il prezzo si muove contro il trend generale

- **Volume spike detection**
  - *Rilevamento picchi di volume anomali*
  - Analizza il rapporto volume corrente vs media storica
  - Cerca variazioni superiori a 2-3x la media normale

- **Min confidence: 0.7**
  - *Confidenza minima richiesta: 70%*
  - Soglia più alta perché si basa su movimenti significativi
  - Maggiore rischio/rendimento rispetto allo scalping

**Come funziona:** Traccia il volume su finestre temporali multiple (5s, 30s, 60s). Quando rileva un picco di volume combinato 
                   con accelerazione del prezzo e shift del Weight of Money, genera segnale nella direzione del movimento.

---

### 3. Value Bet Strategy
**Strategia Value Bet - Scommesse di Valore**

- **Identifies mispriced odds**
  - *Identifica quote mal prezzate dal mercato*
  - Confronta le probabilità implicite con le probabilità reali stimate
  - Cerca discrepanze superiori alla soglia configurata (es. 5%)

- **Kelly Criterion staking**
  - *Gestione stake con Criterio di Kelly*
  - Formula matematica per ottimizzare la dimensione della puntata
  - Massimizza la crescita del bankroll nel lungo termine
  - Formula: `stake = (valore * probabilità - (1 - probabilità)) / valore`

- **Min confidence: 0.6**
  - *Confidenza minima richiesta: 60%*
  - Richiede alta certezza sul calcolo delle probabilità reali
  - Strategia a medio termine (non intraday)

**Come funziona:** Calcola le "true odds" basandosi su modelli statistici e dati storici. 
                   Confronta con le quote di mercato. Se trova valore positivo (quote > probabilità reali), 
                   genera segnale con stake calcolato tramite Kelly Criterion (con frazione conservativa del 25% per limitare la volatilità).

---

### 4. Surebet Detection
**Rilevamento Surebet - Arbitraggio Garantito**

- **Arbitrage opportunities**
  - *Opportunità di arbitraggio tra back e lay*
  - Profit garantito a prescindere dal risultato dell'evento
  - Sfrutta inefficienze temporanee del mercato

- **Back/Lay price discrepancies**
  - *Discrepanze di prezzo tra back (punta) e lay (banca)*
  - Quando il prezzo di back è maggiore del prezzo di lay
  - Formula: `profitto = (1 / back + 1 / lay) < 1`

- **Automatic stake calculation**
  - *Calcolo automatico degli stake per profitto garantito*
  - Distribuisce il capitale in modo da garantire lo stesso profitto su tutti i risultati
  - Formula stake back: `total / (back + 1)`
  - Formula stake lay: `total / lay`

**Come funziona:** Analizza tutte le coppie back/lay per ogni runner. 
                   Quando la somma delle probabilità implicite è minore di 1 (overround negativo), 
                   calcola automaticamente gli stake ottimali per garantire il profitto configurato (es. 0.5%). 
                   Genera segnale immediato con priorità alta.
        
---

### 🎯 Configurazione Strategia

Tutte le strategie possono essere abilitate/disabilitate e configurate tramite `appsettings.json` dell'Analyst:

```json
{
  "Analyst": {
    "ProStrategies": {
      "Enabled": true,
      "Scalping": {
        "Enabled": true,
        "MinConfidence": 0.6,
        "MinMomentumThreshold": 0.02,
        "BaseStake": 10.0
      },
      "SteamMove": {
        "Enabled": true,
        "MinConfidence": 0.7,
        "MinVolumeSpikeMultiplier": 2.0,
        "BaseStake": 20.0
      },
      "ValueBet": {
        "Enabled": true,
        "MinConfidence": 0.6,
        "MinValuePercentage": 5.0,
        "KellyFraction": 0.25
      }
    }
  }
}
```

**Orchestrator:** Le strategie vengono orchestrate dal `StrategyOrchestrator` che:
- Esegue tutte le strategie abilitate in parallelo
- Filtra i segnali per confidenza minima
- Risolve conflitti quando più strategie generano segnali opposti
- Rankizza per ROI atteso e limita il numero di segnali simultanei



## 🛡️ Risk Management

**Gestione del Rischio - Sistema Multi-Livello di Protezione**

The Executor implements multi-layer risk controls:
*L'Executor implementa controlli di rischio su più livelli per proteggere il capitale*

---

### Circuit Breaker
**Interruttore Automatico - Protezione da Perdite Consecutive**

- **Automatically halts trading after X failures**
  - *Blocca automaticamente il trading dopo X fallimenti consecutivi*
  - Previene perdite a cascata causate da malfunzionamenti o condizioni di mercato avverse
  - Default: Si attiva dopo 5 ordini falliti in una finestra di 15 minuti
  - Stato salvato in Redis per persistenza tra riavvii

- **Configurable threshold and window**
  - *Soglia e finestra temporale configurabili*
  - `CircuitBreakerFailureThreshold`: numero di fallimenti prima dell'attivazione
  - `CircuitBreakerWindowMinutes`: durata della finestra di monitoraggio
  - Esempio: 10 fallimenti in 30 minuti per mercati volatili

- **Manual reset required**
  - *Richiede reset manuale per riprendere il trading*
  - Previene riavvii automatici incontrollati
  - Reset via API endpoint o Blazor Dashboard
  - Log dettagliato della causa di attivazione

**Come funziona:** Monitora continuamente il tasso di fallimento degli ordini. Quando il numero di ordini falliti supera la soglia nella finestra temporale configurata, il sistema:
1. Blocca immediatamente tutti i nuovi ordini
2. Annulla gli ordini pendenti (opzionale)
3. Notifica via Prometheus alert e log
4. Rimane in stato "triggered" fino a reset manuale

**Configurazione:**
```json
{
  "Risk": {
    "CircuitBreakerEnabled": true,
    "CircuitBreakerFailureThreshold": 5,
    "CircuitBreakerWindowMinutes": 15
  }
}
```

---

### Position Limits
**Limiti di Posizione - Controllo dell'Esposizione**

- **Max stake per order**
  - *Stake massimo per singolo ordine*
  - Limita la perdita potenziale su una singola operazione
  - Protegge da errori di configurazione o segnali anomali
  - Default: £100 per ordine
  - Formula validazione: `signal.Stake <= MaxStakePerOrder`

- **Max exposure per market**
  - *Esposizione massima per mercato*
  - Somma di tutti gli stake su un singolo mercato (tutti i runner)
  - Evita sovraesposizione su eventi singoli
  - Default: £500 per mercato
  - Formula: `Σ(stake_runner_i) <= MaxExposurePerMarket`

- **Max exposure per selection**
  - *Esposizione massima per singolo runner/selezione*
  - Limita il rischio su un singolo risultato
  - Particolarmente importante per mercati con favoriti netti
  - Default: £200 per selection
  - Considera sia ordini matched che unmatched

- **Max daily loss**
  - *Perdita massima giornaliera*
  - Stop loss globale calcolato dalla mezzanotte UTC
  - Include ordini matched, commissioni e slippage
  - Default: £500 al giorno
  - Reset automatico a mezzanotte o manualmente

**Come funziona:** Prima di ogni ordine, il sistema:
1. Recupera l'esposizione corrente da Redis
2. Calcola la nuova esposizione ipotetica
3. Verifica tutti i limiti in sequenza
4. Rifiuta l'ordine se uno qualsiasi dei limiti viene superato
5. Aggiorna l'esposizione solo dopo conferma da Betfair

**Tracking esposizione in tempo reale:**
- Aggiornamento immediato dopo ogni ordine placed
- Decremento quando ordine viene cancelled o matched
- Reconciliation periodica ogni 60 secondi con Betfair API
- Persistent storage in Redis per resilienza

**Configurazione:**
```json
{
  "Risk": {
    "MaxStakePerOrder": 100.0,
    "MaxExposurePerMarket": 500.0,
    "MaxExposurePerSelection": 200.0,
    "MaxDailyLoss": 500.0
  }
}
```

---

### Validation Pipeline
**Pipeline di Validazione - 5 Livelli di Controllo**

*Ogni segnale passa attraverso 5 controlli sequenziali prima dell'esecuzione*

**1. Signal age check**
- *Verifica età del segnale*
- Rifiuta segnali troppo vecchi (oltre X secondi dalla generazione)
- Previene esecuzione di segnali obsoleti in mercati veloci
- Default timeout: 30 secondi
- Formula: `(Now - Signal.Timestamp) <= MaxSignalAge`

**2. Stake limit validation**
- *Validazione limite stake*
- Primo livello di protezione: singolo ordine
- Controlla: `Signal.Stake <= MaxStakePerOrder`
- Rejection reason: "Stake exceeds maximum allowed per order"

**3. Exposure limit check**
- *Controllo limiti esposizione*
- Secondo livello: esposizione aggregata
- Verifica limiti per market e per selection
- Query Redis per esposizione corrente:
  ```
  current_market = GET risk:exposure:{marketId}
  current_selection = GET risk:exposure:selection:{selectionId}
  ```
- Rejection reasons:
  - "Market exposure limit exceeded"
  - "Selection exposure limit exceeded"

**4. Daily loss verification**
- *Verifica perdita giornaliera*
- Terzo livello: controllo P&L del giorno
- Query: `GET risk:daily-loss` (aggiornato in tempo reale)
- Confronto: `CurrentDailyLoss + PotentialLoss <= MaxDailyLoss`
- Rejection reason: "Daily loss limit reached"
- Include anche ordini pending (worst case scenario)

**5. Circuit breaker state**
- *Stato interruttore automatico*
- Ultimo livello: controllo globale sistema
- Query: `GET risk:circuit-breaker` → stato (0=open, 1=triggered)
- Se triggered: blocca TUTTI gli ordini indipendentemente dai limiti
- Rejection reason: "Circuit breaker triggered - trading halted"
- Bypass disponibile solo per ordini di chiusura posizioni

**Flow decisionale:**
```
Signal → [1] Age? → [2] Stake? → [3] Exposure? → [4] Daily Loss? → [5] Circuit Breaker? → Execute
           ↓ OLD     ↓ HIGH      ↓ EXCEEDED      ↓ EXCEEDED       ↓ TRIGGERED           ↓
         REJECT    REJECT       REJECT          REJECT           REJECT            ✅ SUCCESS
```

**Metriche associate:**
- `aibetting_executor_signals_rejected_total{reason="signal_age"}`
- `aibetting_executor_signals_rejected_total{reason="stake_limit"}`
- `aibetting_executor_signals_rejected_total{reason="exposure_limit"}`
- `aibetting_executor_signals_rejected_total{reason="daily_loss"}`
- `aibetting_executor_signals_rejected_total{reason="circuit_breaker"}`

**Configurazione completa:**
```json
{
  "Risk": {
    "Enabled": true,
    "MaxSignalAgeSeconds": 30,
    "MaxStakePerOrder": 100.0,
    "MaxExposurePerMarket": 500.0,
    "MaxExposurePerSelection": 200.0,
    "MaxDailyLoss": 500.0,
    "CircuitBreakerEnabled": true,
    "CircuitBreakerFailureThreshold": 5,
    "CircuitBreakerWindowMinutes": 15
  }
}
```

---

### 🔐 Best Practices per Risk Management

**Configurazione conservativa (Principianti):**
- MaxStakePerOrder: £10-20
- MaxExposurePerMarket: £50-100
- MaxDailyLoss: £50-100
- CircuitBreakerThreshold: 3 fallimenti

**Configurazione moderata (Intermedi):**
- MaxStakePerOrder: £50-100
- MaxExposurePerMarket: £200-500
- MaxDailyLoss: £200-500
- CircuitBreakerThreshold: 5 fallimenti

**Configurazione aggressiva (Avanzati):**
- MaxStakePerOrder: £100-500
- MaxExposurePerMarket: £1000-2000
- MaxDailyLoss: £1000-2000
- CircuitBreakerThreshold: 10 fallimenti

**⚠️ IMPORTANTE:** 
- Inizia sempre con limiti bassi e aumenta gradualmente
- Monitora giornalmente le metriche di rejection
- Rivedi i limiti dopo eventi di circuit breaker
- Usa Paper Trading per testare nuove configurazioni

## 📁 Project Structure

```
AIBettingSolution/
├── docker/                           # Docker infrastructure
│   └── docker-compose.yml            # All services definition
├── prometheus/                       # Prometheus configuration
│   ├── prometheus.yml                # Scrape config
│   ├── alert-rules.yml               # Alert definitions
│   ├── alertmanager.yml              # Alert routing
│   └── README.md                     # Query reference
├── grafana/                          # Grafana configuration
│   ├── provisioning/
│   │   ├── datasources/              # Auto-configured datasources
│   │   └── dashboards/               # Dashboard provisioning
│   └── dashboards/                   # Dashboard JSON files
├── docs/                             # Documentation
│   ├── diagrams/                     # Architecture diagrams
│   │   ├── AIBettingCore-ClassDiagram.md
│   │   ├── AIBettingExplorer-ClassDiagram.md
│   │   ├── AIBettingAnalyst-ClassDiagram.md
│   │   └── AIBettingExecutor-ClassDiagram.md
│   └── README.md                     # This file
├── AIBettingCore/                    # Shared library
├── AIBettingExplorer/                # Data ingestion
├── AIBettingAnalyst/                 # Signal generation
├── AIBettingExecutor/                # Order execution
├── AIBettingAccounting/              # Trade logging
└── AIBettingBlazorDashboard/         # Web UI
```

## 🔧 Configuration

### Environment Variables

```bash
# Redis
REDIS_CONNECTION_STRING=localhost:16379

# PostgreSQL
POSTGRES_CONNECTION_STRING=Host=localhost;Port=15432;...

# Betfair
BETFAIR_APP_KEY=your_app_key
BETFAIR_SESSION_TOKEN=your_token
```

### appsettings.json

Each application has its own configuration file. Key settings:

**Executor (`AIBettingExecutor/appsettings.json`):**
```json
{
  "Executor": {
    "Risk": {
      "Enabled": true,
      "CircuitBreakerEnabled": true,
      "MaxStakePerOrder": 100.0,
      "MaxExposurePerMarket": 500.0,
      "MaxDailyLoss": 500.0
    },
    "Trading": {
      "EnablePaperTrading": true,
      "UseMockBetfair": true
    }
  }
}
```

## 📚 Documentation

- **Architecture Diagrams**: `docs/diagrams/` - Class diagrams with Mermaid
- **Prometheus Queries**: `prometheus/README.md` - Useful PromQL queries
- **API Documentation**: Each project has inline XML comments

## 🧪 Testing

### Mock Mode
Run Executor with `UseMockBetfair: true` for testing without real Betfair API:

```json
{
  "Trading": {
    "UseMockBetfair": true,
    "EnablePaperTrading": true
  }
}
```

### Load Testing
Use the included scripts to simulate price updates:

```bash
# Publish test price update to Redis
docker exec aibetting-redis redis-cli PUBLISH "channel:price-updates" '{...}'
```

## 🐛 Troubleshooting

### Common Issues

**1. Redis connection timeout**
- Check Docker container is running: `docker ps | findstr redis`
- Verify port 16379 is accessible
- Check `appsettings.json` uses correct port

**2. Prometheus targets DOWN**
- Verify applications are running and exposing metrics
- Check firewall isn't blocking ports 5001, 5002, 5003
- Restart Prometheus: `docker restart aibetting-prometheus-v2`

**3. Grafana shows "No data"**
- Verify Prometheus datasource configured
- Check applications are generating data
- Adjust time range in Grafana

See project-specific README files for more troubleshooting tips.

## 📈 Performance

- **Explorer**: <10ms latency per price update
- **Analyst**: ~50ms per market analysis
- **Executor**: <200ms order execution (P99)
- **System**: Handles 1000+ markets concurrently

## 🔒 Security

**WARNING**: This is a development/testing setup. For production:
- Change all default passwords
- Enable Redis authentication
- Use SSL/TLS for PostgreSQL
- Implement API authentication
- Use secrets management (Azure Key Vault, HashiCorp Vault)
- Enable network isolation

## 📄 License

[Your license here]

## 👥 Contributing

[Contributing guidelines]

## 🆘 Support

For issues or questions:
1. Check documentation in `docs/`
2. Review Prometheus metrics and Grafana dashboards
3. Check application logs in `logs/` directories
4. Create an issue on GitHub

---

**Built with .NET 10, Prometheus, Grafana, Redis, PostgreSQL, and ❤️**
