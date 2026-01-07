# Dimensionamento iniziale (stima)

Queste stime sono di base e vanno validate con test di carico (NBomber/K6) e profiling.

## Redis
- Dataset chiavi live
  - `prices:{market}:{selection}`: ~1–5 KB/selection; 1000 mercati × 3 selezioni ? 3–15 MB
  - `signals`, `orders`, `flags`: trascurabili (<<1 MB)
  - `latency:timestamps` (Sorted Set): buffer 10k eventi ? 1–5 MB
- Overhead + Pub/Sub: 50–150 MB
- RAM consigliata
  - Dev: 256–512 MB
  - Prod: 1–2 GB (headroom per spike)
- Performance
  - Throughput: >1000 msg/sec gestibile su singolo nodo
  - Persistenza: AOF disabilitato o fsync ogni 1s; RDB snapshot ogni 5–15 min
- Note operative
  - TTL per chiavi volatili; partizionare per mercato se necessario
  - Monitorare latenza per hop e backpressure

## PostgreSQL
- Tabella `trades`
  - Tasso ordini: 5–20/min ? 300–1200/h ? 7k–29k/g ? 200k–800k/mese
  - Dimensione record: ~150–300 B (senza indici)
  - Dati grezzi: 30–240 MB/mese
  - Indici (timestamp, market_id, status): +50–100% ? 45–480 MB/mese
- Tabella `daily_summaries`: <10 MB/anno
- Storage totale: 5–20 GB/anno con margine
- Risorse
  - RAM: 2–4 GB
  - CPU: 2–4 vCPU
- Parametri consigliati
  - `shared_buffers` ~25% RAM
  - `effective_cache_size` ~50–75% RAM
  - `wal_level=replica`, `max_wal_size` 1–2 GB
  - `maintenance_work_mem` 256–512 MB
  - Autovacuum con scale factors ridotti su `trades`
- Operatività
  - Backup base + WAL (giornaliero/settimanale)
  - Partizionamento mensile di `trades`; valutare compressione/archiviazione

## Validazione e tuning
- Eseguire test: 500–1000 updates/sec (5–15 min) e 10–50 segnali/min
- Raccogliere metriche: latenza E2E (<200ms), CPU/RAM, IOPS, throughput
- Adattare risorse e parametri in base a risultati (canary rollout)
