# ?? Specifiche Tecniche di Progetto: AIBE-MAS
**AI Betting Multi-Agent System**

---

## ?? Informazioni Generali

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

## ?? Obiettivi di Business
- Identificazione Surebet e opportunità di Scalping
- Latenza end-to-end < 200ms su Betfair Exchange
- Protezione capitale: Watchdog + Circuit Breaker
- Dashboard real-time dei profitti netti (post-commissioni)

KPI Tecnici
- Latency target: < 200ms (E2E)
- Uptime: 99.5%
- Accuracy ML: 65–75% su trend quote; 52–60% su esito finale
- ROI netto: tracciamento accurato al 0.01%

---

## ??? Architettura del Sistema

Componenti
- AIBettingExplorer: Consumer Stream API Betfair ? Redis
- AIBettingAnalyst: ML.NET ? Genera segnali
- AIBettingExecutor: Ordini REST ? Gestione Matched/Unmatched
- AIBettingWatchdog: Latenza/sicurezza ? Kill-Switch
- AIBettingAccounting: Persistenza PostgreSQL

Flusso
Betfair Stream ? Explorer ? Redis ? Analyst ? Segnale ? Executor ? Betfair REST ? Conferma ? AIBettingAccounting

---

## ?? Struttura Progetti (sintesi)
- AIBettingCore (da creare): modelli, interfacce, utils
- AIBettingExplorer (console): stream ? Redis
- AIBettingAnalyst (console): ML.NET ? segnali
- AIBettingExecutor (console): ordini + certificato .pfx
- AIBettingWatchdog (console/service): monitoraggio e kill-switch
- AIBettingDashboard (Blazor, da creare): monitoraggio
- AIBettingAccounting (class library): EF Core (Npgsql) + query ROI

---

## ?? Strategie di Trading integrate

Market Making (Spread Back/Lay)
- Sfruttamento spread tra miglior `Back` e `Lay` nei mercati liquidi.
- Posizionamento ordini su entrambi i lati per catturare tick senza rischio sull’esito.

Scalping Pre-match
- Trading direzionale su micro-inefficienze prima dell’evento.
- Uso di feature: `seconds_to_start`, `total_matched`, `WAP`, `Weight of Money`.

Green Up
- Chiusura posizione garantita tramite lay/back calibrato.

Steam Movement
- Identificazione calo improvviso della quota basato su volumi e profondità del book.

---

## ?? Metriche ML e Feature Engineering
- xG (Expected Goals), xPoints per calcio
- WAP (Weighted Average Price) dalle code `available_to_back/lay`
- CLV (Closing Line Value)
- Volume Trends, Spread Analysis, WoM (Weight of Money)

Accuratezza attesa
- Esito finale (1X2): 52–60% su top leghe
- Trend quote/trading: 65–75%

---

## ??? Formato dati storici (JSON per training)
Per ciascun mercato:

```
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

Motivazioni
- `seconds_to_start`, `total_matched`: efficienza + liquidità
- `available_to_back/lay`: profondità per WAP/WoM
- `final_result`: label per supervised learning

---

## ?? Criteri di liquidità
- Focalizzarsi su mercati con `total_matched` > 50k€
- Priorità: Top 5 campionati europei (Serie A, Premier, Liga, Bundesliga, Ligue 1)
- Altri sport: Tennis (ATP/WTA), Ippica UK/Irish

---

## ?? Strategia di raccolta dati
- Data Logger 24/7 con snapshot ogni 5–30s (stream)
- Timeframe storico consigliato: 2–3 anni (ottimale 5 anni)
- Confronto performance: dati storici ufficiali vs dati live

---

## ?? Sicurezza e Rischi
- Certificato Betfair `.pfx`, password in Key Vault/variabili ambiente
- IP whitelisting
- Kill-Switch condizioni: latenza media > 500ms (5 min), perdita giornaliera > soglia, saldo < soglia, >3 ordini rifiutati, disconnessione Redis > 60s

Risk management (estratto)
- Limiti giornalieri, esposizione per mercato, percentuale bankroll per trade

---

## ??? Database Schema (PostgreSQL)
- Tabella `trades`: campi
  - `id` UUID PRIMARY KEY
  - `timestamp` TIMESTAMPTZ NOT NULL
  - `market_id` TEXT NOT NULL
  - `selection_id` TEXT NOT NULL
  - `stake` NUMERIC NOT NULL
  - `odds` NUMERIC NOT NULL
  - `type` TEXT NOT NULL CHECK (type IN ('BACK','LAY'))
  - `status` TEXT NOT NULL CHECK (status IN ('PENDING','MATCHED','UNMATCHED','CANCELLED'))
  - `profit_loss` NUMERIC NULL
  - `commission` NUMERIC NOT NULL
  - `net_profit` NUMERIC NULL
  - `created_at` TIMESTAMPTZ DEFAULT NOW()
- Indici: su `timestamp`, `market_id`, `status`
- Tabella `daily_summaries`: aggregati giornalieri (ROI, NetProfit, Commission)

---

## ?? Redis Data Structures
- Hash: `prices:{marketId}:{selectionId}`
- Hash: `signals:{marketId}:{selectionId}`
- Hash: `orders:{orderId}`
- String: `flag:trading-enabled`
- Sorted Set: `latency:timestamps`
- Channels: `channel:price-updates`, `channel:trading-signals`, `channel:order-updates`, `channel:kill-switch`

---

## ? Performance
- Stream ? Redis: < 10ms; Redis ? ML: < 50ms; Segnale ? API: < 100ms; E2E: < 200ms
- Throughput: > 1000 updates/sec; 10–50 signals/min; 5–20 ordini/min
- Risorse: CPU < 25%, RAM < 200MB/servizio

---

## ?? Testing
- Unit: xUnit, FluentAssertions; Mocking: Moq
- Integrazione: Redis Pub/Sub, EF Core (PostgreSQL con Testcontainer), Betfair mock server
- Load: NBomber/K6 con 500 updates/sec (5 min)

---

## ?? Roadmap
Fase 1
- Implementazione AIBettingCore (models, interfaces)
- Setup AIBettingAccounting (EF Core + PostgreSQL)
- Configurazione Redis

Fase 2
- Explorer: WebSocket ? Redis; parsing stream
- Analyst: lettura Redis; SurebetDetector (base)

Fase 3
- Executor: certificato .pfx; Place/Cancel/List orders
- Stati Matched/Unmatched; integrazione AIBettingAccounting

Fase 4
- ML.NET momentum; ScalpingAnalyzer; backtesting

Fase 5
- Watchdog + Kill-Switch; notifiche; Dashboard (Blazor)

Fase 6
- Deploy VPS; Docker Compose; IP Whitelisting; Monitoring

---

## ?? Betfair API (estratto)
- Login: `https://identitysso-cert.betfair.com/api/certlogin`
- Betting JSON-RPC: `https://api.betfair.com/exchange/betting/json-rpc/v1`
- Stream: `wss://stream-api.betfair.com/api/v1`

Metodi
- `listMarketCatalogue`, `listMarketBook`, `placeOrders`, `cancelOrders`, `listCurrentOrders`

---

## ?? Metriche di successo
- ROI mensile netto > 5%
- Win Rate > 60%
- Latenza media < 150ms
- Errori API < 1%
- Ordini matched > 90%

---

Ultima Modifica: 2026-01-05
Autore: Diego Lista
Versione Documento: 1.0.0
