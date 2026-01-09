# 🤖 Specifiche Tecniche di Progetto: AIBE-MAS
**AI Betting Multi-Agent System**

---

## 📋 Informazioni Generali

| Proprietà | Valore |
|-----------|--------|
| Nome Progetto | AIBE-MAS (AI Betting Multi-Agent System) |
| Versione | 1.0.0 |
| Target Framework | .NET 10 |
| Linguaggio | C# 14 |
| Architettura | Microservizi asincroni (Event-Driven) |
| Formato Solution | .slnx |
| Repository | https://github.com/diegolista673/AIBetting |

---

## 🎯 Obiettivi di Business
- Identificazione Surebet e opportunità di Scalping
- Latenza end-to-end < 200ms su Betfair Exchange
- Protezione capitale: Watchdog + Circuit Breaker
- Dashboard real-time dei profitti netti (post-commissioni)

### KPI Tecnici
- **Latency target**: < 200ms (E2E)
- **Uptime**: 99.5%
- **Accuracy ML**: 65–75% su trend quote; 52–60% su esito finale
- **ROI netto**: tracciamento accurato al 0.01%

---

## 🏗️ Architettura del Sistema

### Componenti
- **AIBettingExplorer**: Consumer Stream API Betfair → Redis
- **AIBettingAnalyst**: ML.NET → Genera segnali
- **AIBettingExecutor**: Ordini REST → Gestione Matched/Unmatched
- **AIBettingWatchdog**: Latenza/sicurezza → Kill-Switch
- **AIBettingAccounting**: Persistenza PostgreSQL
- **AIBettingBlazorDashboard**: Dashboard web real-time
- **AIBettingCore**: Libreria condivisa (models, interfaces, utils)

### Flusso
```
Betfair Stream → Explorer → Redis → Analyst → Segnale → Executor → Betfair REST → Conferma → AIBettingAccounting
                                                                                                        ↓
                                                                                              AIBettingBlazorDashboard
```

---

## 📦 Struttura Progetti

| Progetto | Tipo | Descrizione | Status |
|----------|------|-------------|--------|
| **AIBettingCore** | Class Library | Models, Interfaces, Utils, RiskManager | ✅ Implementato |
| **AIBettingExplorer** | Console App | Stream Betfair → Redis | ✅ Base implementata |
| **AIBettingAnalyst** | Console App | ML.NET → Segnali trading | 🔄 In sviluppo |
| **AIBettingExecutor** | Console App | Place/Cancel orders + .pfx cert | 🔄 In sviluppo |
| **AIBettingWatchdog** | Windows Service | Monitoring + Kill-Switch | 📋 Pianificato |
| **AIBettingBlazorDashboard** | Blazor Server | Dashboard web real-time | ✅ Struttura base |
| **AIBettingAccounting** | Class Library | EF Core + PostgreSQL + ROI queries | ✅ Implementato |
| **RedisSample** | Console App | Sample Redis integration | ✅ Demo |

---

## 🎲 Strategie di Trading Integrate

### 1. Market Making (Spread Back/Lay)
- Sfruttamento spread tra miglior `Back` e `Lay` nei mercati liquidi
- Posizionamento ordini su entrambi i lati per catturare tick senza rischio sull'esito
- **Target**: 0.5-1% ROI per trade, alto volume

### 2. Scalping Pre-match
- Trading direzionale su micro-inefficienze prima dell'evento
- Feature: `seconds_to_start`, `total_matched`, `WAP`, `Weight of Money`
- **Target**: 1-3% ROI per trade, segnali rapidi

### 3. Green Up
- Chiusura posizione garantita tramite lay/back calibrato
- Profitto fisso indipendentemente dall'esito
- **Target**: 2-5% ROI per mercato

### 4. Steam Movement
- Identificazione calo improvviso della quota basato su volumi e profondità del book
- Reazione rapida a informazioni non pubbliche
- **Target**: 3-8% ROI per segnale (raro)

---

## 📊 Metriche ML e Feature Engineering

### Features Principali
- **xG** (Expected Goals): per calcio, predizione statistica gol
- **xPoints**: probabilità punti finali basata su performance squadre
- **WAP** (Weighted Average Price): dalle code `available_to_back/lay`
- **CLV** (Closing Line Value): valore della quota rispetto a chiusura mercato
- **Volume Trends**: analisi velocità matched
- **Spread Analysis**: differenza back-lay
- **WoM** (Weight of Money): distribuzione volume su selezioni

### Accuratezza Attesa
- **Esito finale (1X2)**: 52–60% su top leghe (EPL, Serie A, etc.)
- **Trend quote/trading**: 65–75% (più prevedibile del risultato)

---

## 📂 Formato Dati Storici (JSON per Training)

Per ciascun mercato:

```json
{
  "market_id": "1.223456789",
  "event_name": "Inter vs Milan",
  "event_type": "Soccer",
  "market_type": "MATCH_ODDS",
  "start_time": "2023-10-27T20:45:00Z",
  "final_result": "HOME_WIN",
  "snapshots": [
    {
      "timestamp": "2023-10-27T20:00:01Z",
      "seconds_to_start": 2700,
      "total_matched": 154000.5,
      "runners": [
        {
          "selection_id": 12345,
          "runner_name": "Inter",
          "last_price_matched": 2.10,
          "ex": {
            "available_to_back": [ { "price": 2.10, "size": 500.0 } ],
            "available_to_lay":  [ { "price": 2.12, "size": 300.0 } ]
          }
        }
      ]
    }
  ]
}
```

**Motivazioni**:
- `seconds_to_start`, `total_matched`: efficienza + liquidità
- `available_to_back/lay`: profondità per WAP/WoM
- `final_result`: label per supervised learning

---

## 💧 Criteri di Liquidità

### Mercati Target
- **Focalizzazione**: `total_matched` > 50k€
- **Priorità**: Top 5 campionati europei
  - 🇮🇹 Serie A
  - 🏴 Premier League
  - 🇪🇸 La Liga
  - 🇩🇪 Bundesliga
  - 🇫🇷 Ligue 1
- **Altri sport**: Tennis (ATP/WTA), Ippica UK/Irish

---

## 📈 Strategia di Raccolta Dati

- **Data Logger**: 24/7 con snapshot ogni 5–30s (stream)
- **Timeframe storico**: 2–3 anni (ottimale 5 anni)
- **Confronto performance**: dati storici ufficiali vs dati live
- **Storage**: PostgreSQL per storico, Redis per real-time

---

## 🔐 Sicurezza e Rischi

### Autenticazione Betfair
- Certificato `.pfx` con password in Key Vault/variabili ambiente
- IP whitelisting obbligatorio per API accesso
- Session token refresh automatico

### Kill-Switch Condizioni
- Latenza media > 500ms per 5 minuti consecutivi
- Perdita giornaliera > soglia configurata
- Saldo < soglia minima sicurezza
- \>3 ordini rifiutati in 5 minuti (Circuit Breaker)
- Disconnessione Redis > 60s

### Risk Management
- **Limiti giornalieri**: MaxDailyLoss configurabile
- **Esposizione per mercato**: MaxExposurePerMarket
- **Esposizione per selezione**: MaxExposurePerSelection
- **Percentuale bankroll per trade**: MaxRiskPerTradePercent (default 2%)
- **Circuit Breaker**: 3 failures → halt trading

---

## 🗄️ Database Schema (PostgreSQL)

### Tabella `trades`
```sql
CREATE TABLE trades (
    id UUID PRIMARY KEY,
    timestamp TIMESTAMPTZ NOT NULL,
    market_id TEXT NOT NULL,
    selection_id TEXT NOT NULL,
    stake NUMERIC NOT NULL,
    odds NUMERIC NOT NULL,
    type TEXT NOT NULL CHECK (type IN ('BACK','LAY')),
    status TEXT NOT NULL CHECK (status IN ('PENDING','MATCHED','UNMATCHED','CANCELLED')),
    profit_loss NUMERIC NULL,
    commission NUMERIC NOT NULL,
    net_profit NUMERIC NULL,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Indici
CREATE INDEX idx_trades_timestamp ON trades(timestamp);
CREATE INDEX idx_trades_market_id ON trades(market_id);
CREATE INDEX idx_trades_status ON trades(status);
CREATE INDEX idx_trades_created_at ON trades(created_at);
```

### Tabella `daily_summaries`
```sql
CREATE TABLE daily_summaries (
    id SERIAL PRIMARY KEY,
    date DATE NOT NULL UNIQUE,
    total_trades INTEGER NOT NULL DEFAULT 0,
    total_stake NUMERIC NOT NULL DEFAULT 0,
    total_profit_loss NUMERIC NOT NULL DEFAULT 0,
    total_commission NUMERIC NOT NULL DEFAULT 0,
    net_profit NUMERIC NOT NULL DEFAULT 0,
    roi_percent NUMERIC NULL,
    win_rate NUMERIC NULL,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);
```

### Views & Triggers
- **v_trading_stats**: Statistiche ultimi 30 giorni
- **v_daily_pnl**: P&L giornaliero con ROI
- **trg_update_daily_summary**: Auto-update summary dopo insert/update trades

---

## 🔴 Redis Data Structures

### Keys Pattern
```
prices:{marketId}:{selectionId}        # Hash: ultimo prezzo per selezione
signals:{marketId}:{selectionId}       # Hash: segnale trading generato
orders:{orderId}                       # Hash: dettagli ordine
exposure:{marketId}                    # Hash: esposizione per selezione
positions:{marketId}                   # Hash: posizioni aperte
risk:limits                            # Hash: configurazione limiti
failed:orders                          # Sorted Set: ordini falliti (circuit breaker)
pnl:daily:{date}                       # String: P&L giornaliero
pnl:total                              # String: P&L totale cumulativo
flag:trading-enabled                   # String: kill-switch flag
latency:timestamps                     # Sorted Set: tracking latenza
```

### Pub/Sub Channels
```
channel:price-updates                  # Aggiornamenti prezzi real-time
channel:trading-signals                # Segnali trading da Analyst
channel:order-updates                  # Conferme/reject ordini
channel:kill-switch                    # Emergenza stop trading
channel:exposure-updates               # Cambio esposizione
```

---

## ⚡ Performance Targets

### Latenza
| Fase | Target | Implementazione |
|------|--------|-----------------|
| Stream → Redis | < 10ms | In-memory write |
| Redis → ML | < 50ms | Pub/Sub + async |
| Segnale → API | < 100ms | REST + validation |
| **E2E Total** | **< 200ms** | **Budget completo** |

### Throughput
- **Price updates**: > 1000 updates/sec
- **Signals generated**: 10–50 signals/min
- **Orders placed**: 5–20 ordini/min

### Risorse
- **CPU**: < 25% per servizio
- **RAM**: < 200MB per servizio
- **Network**: < 1 Mbps per stream

---

## 🧪 Testing Strategy

### Unit Testing
- **Framework**: xUnit + FluentAssertions
- **Mocking**: Moq per dipendenze esterne
- **Coverage**: > 80% su AIBettingCore

### Integration Testing
- **Redis Pub/Sub**: Testcontainers Redis
- **EF Core**: Testcontainers PostgreSQL
- **Betfair API**: Mock server con dati realistici

### Load Testing
- **Tool**: NBomber / K6
- **Scenario**: 500 price updates/sec per 5 minuti
- **Target**: Latenza < 200ms al 95° percentile

---

## 🚀 Roadmap

### ✅ Fase 1 (Completata)
- [x] Implementazione AIBettingCore (models, interfaces)
- [x] Setup AIBettingAccounting (EF Core + PostgreSQL)
- [x] Configurazione Redis
- [x] RiskManager + Circuit Breaker
- [x] Docker Compose infrastructure (PostgreSQL + Redis)

### 🔄 Fase 2 (In Corso)
- [x] Explorer: Base WebSocket → Redis
- [ ] Explorer: Parsing completo stream Betfair
- [ ] Analyst: Lettura Redis + SurebetDetector base

### 📋 Fase 3 (Pianificata)
- [ ] Executor: Certificato .pfx + authentication
- [ ] Executor: Place/Cancel/List orders
- [ ] Executor: Stati Matched/Unmatched
- [ ] Integrazione AIBettingAccounting per persist trades

### 📋 Fase 4 (Pianificata)
- [ ] ML.NET: Feature engineering completo
- [ ] ML.NET: Momentum strategy
- [ ] ML.NET: ScalpingAnalyzer
- [ ] Backtesting framework su dati storici

### 📋 Fase 5 (Pianificata)
- [ ] Watchdog: Latency monitoring
- [ ] Watchdog: Kill-Switch automatico
- [ ] Dashboard Blazor: UI real-time
- [ ] Dashboard Blazor: Grafici P&L
- [ ] Notifiche: Email/SMS/Slack

### 📋 Fase 6 (Pianificata)
- [ ] Deploy VPS Ubuntu
- [ ] Docker Compose produzione
- [ ] IP Whitelisting Betfair
- [ ] Monitoring: Grafana + Prometheus
- [ ] Backup automatico PostgreSQL
- [ ] Alert system avanzato

---

## 🌐 Betfair API

### Endpoints
- **Login**: `https://identitysso-cert.betfair.com/api/certlogin`
- **Betting JSON-RPC**: `https://api.betfair.com/exchange/betting/json-rpc/v1`
- **Stream**: `wss://stream-api.betfair.com/api/v1`

### Metodi Principali
- `listMarketCatalogue`: Lista mercati disponibili
- `listMarketBook`: Dettagli mercato + runner prices
- `placeOrders`: Piazza ordini (richiede certificato)
- `cancelOrders`: Cancella ordini pendenti
- `listCurrentOrders`: Lista ordini attivi/matched

---

## 📈 Metriche di Successo

### Trading Performance
- **ROI mensile netto**: > 5%
- **Win Rate**: > 60%
- **Avg Profit per trade**: > €2.50
- **Max Drawdown**: < 10%

### Sistema Performance
- **Latenza media**: < 150ms
- **Errori API**: < 1%
- **Ordini matched**: > 90%
- **Uptime sistema**: > 99.5%

---

## 📊 Monitoring e Reporting

```
┌─────────────────────────────────────────────────┐
│           Betfair Exchange                      │
└────────────┬────────────────────────────────────┘
             │ Stream API
             ▼
┌─────────────────────────────────────────────────┐
│      AIBettingExplorer (Stream Consumer)        │
│  • Consuma WebSocket Betfair                    │
│  • Scrive metriche → Redis + Prometheus        │
└────────────┬────────────────────────────────────┘
             │ Redis Pub/Sub
             ▼
┌─────────────────────────────────────────────────┐
│              Redis + PostgreSQL                 │
│  • Latency timestamps (Sorted Set)             │
│  • P&L counters (String)                        │
│  • Trade records (PostgreSQL)                   │
│  • Exposure tracking (Hash)                     │
└────────────┬────────────────────────────────────┘
             │ Metrics Export
             ▼
┌─────────────────────────────────────────────────┐
│            Prometheus (Metrics DB)              │
│  • Scrape Redis Exporter (port 9121)           │
│  • Scrape PostgreSQL Exporter (port 9187)      │
│  • Store 30 days metrics                        │
└────────────┬────────────────────────────────────┘
             │ Query
             ▼
┌─────────────────────────────────────────────────┐
│               Grafana (Visualization)           │
│  • Dashboards real-time (port 3000)            │
│  • Alerts (Email, Slack, SMS)                   │
│  • Historical analysis                          │
│  • Pre-built: Infrastructure Overview          │
└─────────────────────────────────────────────────┘
```

### Dashboard Pre-Configurate
- **Infrastructure Overview**: Redis + PostgreSQL metrics
- **Trading Performance** (da implementare): P&L, ROI, Win Rate
- **Latency Monitoring** (da implementare): E2E latency tracking
- **Circuit Breaker Status** (da implementare): Failed orders, kill-switch

---

## 🐳 Docker Infrastructure

### Stack Base (sempre attivo)
```yaml
services:
  postgres:     # PostgreSQL 16-alpine (port 5432)
  redis:        # Redis 7-alpine (port 6379)
```

### Stack Tools (profile: tools)
```yaml
services:
  pgadmin:      # pgAdmin4 (port 5050)
  redis-insight: # RedisInsight (port 5540)
```

### Stack Monitoring (profile: monitoring)
```yaml
services:
  prometheus:   # Prometheus (port 9090)
  redis-exporter: # Redis metrics (port 9121)
  postgres-exporter: # PostgreSQL metrics (port 9187)
  grafana:      # Grafana dashboards (port 3000)
```

### Comandi Docker
```powershell
# Stack base
docker-compose up -d

# Stack + GUI tools
docker-compose --profile tools up -d

# Stack completo + monitoring
docker-compose --profile monitoring up -d
```

---

## 🔧 Tech Stack Completo

| Categoria | Tecnologia | Versione | Note |
|-----------|-----------|----------|------|
| **Runtime** | .NET | 10.0 | Latest LTS |
| **Linguaggio** | C# | 14.0 | Language features |
| **Database** | PostgreSQL | 16-alpine | Prod-ready |
| **Cache** | Redis | 7-alpine | Pub/Sub + Cache |
| **ORM** | Entity Framework Core | 10.0 | Code-first |
| **ML** | ML.NET | Latest | Local inference |
| **Logging** | Serilog | 3.1.1 | Rolling files |
| **Testing** | xUnit + FluentAssertions | Latest | Unit + Integration |
| **Monitoring** | Grafana + Prometheus | Latest | Real-time dashboards |
| **Containerization** | Docker | 24+ | Dev + Prod |
| **Frontend** | Blazor Server | .NET 10 | Real-time UI |

---

## 📚 Documentazione Aggiuntiva

### File Guida Disponibili
- `RiskManager-Guida.md`: Risk management + Circuit Breaker
- `Docker-Infrastructure-Guide.md`: Setup Docker completo (40+ pagine)
- `PostgreSQL-Setup.md`: Installazione PostgreSQL Ubuntu
- `Grafana-Monitoring-Guide.md`: Setup monitoring stack
- `START.md`: Quick start 2 minuti
- `DOCKER-QUICKSTART.md`: Setup Docker rapido
- `README.md`: Panoramica progetto

### Script Utili
- `database-schema.sql`: Schema PostgreSQL completo
- `prometheus.yml`: Configurazione Prometheus
- `docker-compose.yml`: Infrastructure as Code
- `setup-postgresql.sh`: Auto-install PostgreSQL (Ubuntu)

---

## 🤝 Contributing

1. Fork repository
2. Crea feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add AmazingFeature'`)
4. Push al branch (`git push origin feature/AmazingFeature`)
5. Apri Pull Request

**Guidelines**:
- Segui convenzioni C# (.NET 10 + C# 14)
- Aggiungi unit tests per nuove feature
- Aggiorna documentazione se necessario
- Usa commit messages descrittivi

---

**Ultima Modifica**: 2024-01-15  
**Autore**: Diego Lista  
**Versione Documento**: 2.0.0  
**Repository**: https://github.com/diegolista673/AIBetting
