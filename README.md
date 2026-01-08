# ?? AIBetting - AI Multi-Agent Betting System

Sistema di trading automatizzato su Betfair Exchange con architettura microservizi, machine learning e latenza < 200ms.

[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-blue)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-7-red)](https://redis.io/)
[![Docker](https://img.shields.io/badge/Docker-Ready-blue)](https://www.docker.com/)

---

## ?? Quick Start (2 minuti)

```powershell
# 1. Clone repository
git clone https://github.com/diegolista673/AIBetting
cd AIBetting

# 2. Avvia infrastructure (PostgreSQL + Redis)
docker-compose up -d

# 3. Verifica
docker ps
dotnet build
```

?? **Guida completa**: [START.md](START.md)

---

## ?? Architettura

```
???????????????????????????????????????????????????????????????
?                     Betfair Exchange                        ?
?          (Stream API + REST API + Certificate .pfx)         ?
???????????????????????????????????????????????????????????????
                  ? WebSocket (prices)        ? REST (orders)
                  ?                           ?
???????????????????????????????????????????????????????????????
?              AIBettingExplorer (Console)                    ?
?   Consuma stream Betfair ? Pubblica su Redis Pub/Sub       ?
???????????????????????????????????????????????????????????????
                  ? Redis Pub/Sub
                  ?
???????????????????????????????????????????????????????????????
?                  Redis (Cache + Pub/Sub)                    ?
?   Keys: prices:{marketId}, signals:{marketId}, orders:{id}  ?
?   Channels: price-updates, trading-signals, kill-switch     ?
???????????????????????????????????????????????????????????????
                  ?
         ???????????????????
         ?                 ?
????????????????????  ????????????????????
?  AIBettingAnalyst?  ? AIBettingExecutor?
?   (ML.NET)       ?  ?  (Orders + Risk) ?
?  Genera segnali  ?  ?  Place/Cancel    ?
?  trading         ?  ?  RiskManager     ?
????????????????????  ????????????????????
         ?                     ?
         ???????????????????????
                    ?
         ???????????????????????
         ? AIBettingAccounting ?
         ?   (PostgreSQL)      ?
         ?   P&L, ROI, Trades  ?
         ???????????????????????
                    ?
                    ?
         ??????????????????????
         ?  AIBettingWatchdog ?
         ?  (Kill-Switch +    ?
         ?   Circuit Breaker) ?
         ??????????????????????
```

---

## ?? Features

### ? Implementato

- ? **Core Domain Models** (MarketId, SelectionId, TradeRecord)
- ? **Redis Integration** (Pub/Sub + Cache)
- ? **PostgreSQL Schema** (Trades + Daily Summaries con trigger automatici)
- ? **RiskManager** (7 controlli: stake, exposure, circuit breaker, P&L)
- ? **Docker Infrastructure** (PostgreSQL + Redis + GUI tools)
- ? **Validazione Input** (RedisKeys con ArgumentNullException)
- ? **Circuit Breaker** (3 failures in 5 min ? kill-switch)

### ?? In Sviluppo

- ?? **Betfair Stream Client** (WebSocket ? Redis)
- ?? **ML.NET Models** (Momentum, Scalping, Surebet detection)
- ?? **Order Executor** (Place/Cancel con certificato .pfx)
- ?? **Blazor Dashboard** (Real-time monitoring)

### ?? Roadmap

- [ ] **Watchdog Service** (Latency monitoring + Kill-switch)
- [ ] **Feature Engineering** (WAP, WoM, CLV, xG)
- [ ] **Backtesting Framework** (2-3 anni dati storici)
- [ ] **Strategie Avanzate** (Market Making, Green Up, Steam Movement)
- [ ] **Deploy VPS** (Docker Compose + IP Whitelisting)

---

## ?? Progetti

| Progetto | Tipo | Descrizione |
|----------|------|-------------|
| **AIBettingCore** | Class Library | Models, Interfaces, RedisKeys, RiskManager |
| **AIBettingExplorer** | Console App | Stream consumer Betfair ? Redis |
| **AIBettingAnalyst** | Console App | ML.NET ? Genera segnali trading |
| **AIBettingExecutor** | Console App | Place/Cancel orders + RiskManager |
| **AIBettingAccounting** | Class Library | EF Core + PostgreSQL persistence |
| **AIBettingWatchdog** | Service | Monitoring + Kill-switch |
| **AIBettingBlazorDashboard** | Blazor App | Dashboard real-time |

---

## ??? Tech Stack

| Categoria | Tecnologia | Versione |
|-----------|-----------|----------|
| **Framework** | .NET | 10.0 |
| **Linguaggio** | C# | 14.0 |
| **Database** | PostgreSQL | 16 |
| **Cache/Pub-Sub** | Redis | 7 |
| **ML** | ML.NET | Latest |
| **ORM** | EF Core | 10.0 |
| **Testing** | xUnit + FluentAssertions | Latest |
| **Container** | Docker | 24+ |
| **Frontend** | Blazor Server | .NET 10 |

---

## ?? Sicurezza

- ? **Certificate Betfair** (.pfx con password in Key Vault/Env)
- ? **Redis Password** protetto
- ? **PostgreSQL** con autenticazione md5
- ? **Git Ignore** per secrets (.pfx, .env, passwords)
- ? **Kill-Switch** multi-condizione (latency, losses, failures)
- ? **Circuit Breaker** (3 failures ? halt trading)
- ? **Risk Limits** (exposure per market/selection, daily loss)

---

## ?? KPI Tecnici (Target)

| Metrica | Target | Attuale |
|---------|--------|---------|
| Latenza E2E | < 200ms | In implementazione |
| Uptime | 99.5% | - |
| Accuracy ML (trend) | 65-75% | In training |
| Accuracy ML (esito) | 52-60% | In training |
| ROI tracking | ± 0.01% | ? Implementato |
| Throughput | > 1000 updates/sec | Redis ready |

---

## ?? Documentazione

### Quick Start
- ?? **[START.md](START.md)** - Avvio rapido (2 minuti)
- ?? **[DOCKER-QUICKSTART.md](DOCKER-QUICKSTART.md)** - Setup Docker dettagliato

### Guide Complete
- ?? **[Documentazione/Specifiche.md](Documentazione/Specifiche.md)** - Specifiche tecniche complete
- ?? **[Documentazione/Docker-Infrastructure-Guide.md](Documentazione/Docker-Infrastructure-Guide.md)** - Guida Docker 40+ pagine
- ?? **[Documentazione/RiskManager-Guida.md](Documentazione/RiskManager-Guida.md)** - Risk management e circuit breaker
- ?? **[Documentazione/PostgreSQL-Setup.md](Documentazione/PostgreSQL-Setup.md)** - Setup PostgreSQL Ubuntu

### Database
- ?? **[Documentazione/database-schema.sql](Documentazione/database-schema.sql)** - Schema SQL completo

---

## ?? Testing

```powershell
# Unit tests
dotnet test

# Integration tests (richiede Docker)
docker-compose up -d
dotnet test --filter Category=Integration

# Load tests
# NBomber/K6 con 500 updates/sec per 5 minuti
```

---

## ?? Deploy Produzione

```bash
# Su VPS Ubuntu
git clone https://github.com/diegolista673/AIBetting
cd AIBetting

# Setup .env con password produzione
cp .env.example .env
nano .env

# Avvia stack
docker-compose up -d

# Verifica
docker ps
docker logs -f aibetting_postgres
docker logs -f aibetting_redis
```

---

## ?? Contributing

1. Fork il repository
2. Crea feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add AmazingFeature'`)
4. Push al branch (`git push origin feature/AmazingFeature`)
5. Apri Pull Request

**Guidelines**:
- Segui convenzioni C# (.NET 10)
- Aggiungi unit tests per nuove feature
- Aggiorna documentazione se necessario
- Usa commit messages descrittivi

---

## ?? Performance

### Latency Budget (Target)

```
Stream ? Redis:     < 10ms  ? (in-memory)
Redis ? ML:         < 50ms  ?? (dipende da modello)
Segnale ? API:      < 100ms ? (REST + validation)
?????????????????????????????
E2E Total:          < 200ms ??
```

### Throughput

- **Redis**: > 100k ops/sec (molto oltre necessità)
- **PostgreSQL**: > 5k TPS (sufficiente per logging asincrono)
- **Target sistema**: 1000 price updates/sec, 10-50 segnali/min

---

## ?? Disclaimer

Questo software è fornito **"as is"** per scopi educativi e di ricerca.

**ATTENZIONE**:
- Il trading comporta rischi finanziari elevati
- Testa SEMPRE in ambiente demo prima di usare capitale reale
- Usa il RiskManager e limiti di esposizione conservativi
- Monitora costantemente il sistema in produzione
- L'autore non è responsabile per perdite finanziarie

---

## ?? Supporto

- **Issues**: https://github.com/diegolista673/AIBetting/issues
- **Wiki**: https://github.com/diegolista673/AIBetting/wiki
- **Documentazione**: Cartella `Documentazione/`

---

## ?? License

[Specificare licenza - es. MIT, GPL, Proprietary]

---

## ?? Autore

**Diego Lista**
- GitHub: [@diegolista673](https://github.com/diegolista673)
- Progetto: [AIBetting](https://github.com/diegolista673/AIBetting)

---

## ?? Acknowledgments

- **Betfair API** - Exchange API e Stream
- **ML.NET** - Machine Learning framework
- **PostgreSQL** - Database robusto e performante
- **Redis** - Cache e pub/sub ultra-veloce
- **Docker** - Containerization semplificata

---

**Version**: 1.0.0  
**Last Updated**: 2024-01-15  
**Status**: ?? In Active Development

---

? **Se trovi utile questo progetto, lascia una stella!** ?
