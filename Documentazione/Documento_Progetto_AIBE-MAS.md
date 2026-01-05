📘 Documento di Progetto: AIBE-MAS
Versione: 1.0

Linguaggio: C# .NET 10

Architettura: Microservizi asincroni basati su Redis.

1. Visione e Obiettivi
Sviluppare un sistema di trading sportivo ad alte prestazioni capace di:

Identificare Surebet e opportunità di Scalping tramite Machine Learning.

Eseguire ordini su Betfair Exchange con latenza minima (Target < 200ms).

Garantire la sicurezza del capitale tramite un modulo Watchdog e un Circuit Breaker.

Monitorare profitti netti (dedotte le commissioni) tramite una dashboard in tempo reale.

2. Stack Tecnologico Consigliato
Runtime: .NET 10 con compilazione Native AOT (per la massima velocità).

Database Real-time: Redis (per segnali e prezzi live).

Database Storico: SQLite o PostgreSQL (per lo storico scommesse e il ROI).

AI Engine: ML.NET (per il caricamento dei modelli predittivi).

Interfaccia di Monitoraggio: Blazor WebApp (condivisione del codice C# tra bot e dashboard).

3. Architettura dei Moduli (Struttura della Solution)
La Solution Visual Studio dovrà essere divisa in progetti separati:

AIBetting.Core (Class Library): Modelli dati comuni (TradeRecord, MatchData), interfacce e logiche matematiche (calcolo commissioni, calcolo Green Up).

AIBetting.Explorer (Console App): Gestore della connessione WebSocket alle Betfair Stream API. Scrive i prezzi grezzi su Redis.

AIBetting.Analyst (Console App): Il "Cervello". Legge da Redis, applica i modelli ML.NET e decide quando entrare/uscire.

AIBetting.Executor (Console App): Il "Braccio". Gestisce l'autenticazione SSL (.pfx) e invia i PlaceOrder. Gestisce le conferme (Matched/Unmatched).

AIBetting.Watchdog (Console App/Service): Monitora la latenza, il saldo e attiva il Kill-Switch se necessario.

AIBetting.Dashboard (Blazor App): Visualizzazione dello storico, profitti e operazioni pending.

4. Logica di Esecuzione e Flusso Dati
Snippet di codice

sequenceDiagram
    participant BF as Betfair API
    participant EX as Explorer (C#)
    participant RD as Redis
    participant AN as Analyst (ML)
    participant EXE as Executor (C#)

    BF->>EX: Prezzi Live (Stream)
    EX->>RD: Aggiorna Order Book
    AN->>RD: Legge dati + WoM (Weight of Money)
    AN->>RD: Segnale di Trading (Punta/Banca)
    RD->>EXE: Trigger Ordine
    EXE->>BF: Place Order (Request)
    BF-->>EXE: Order Confirmation (Response)
Punti Chiave della Logica:
Surebet & Scalping: L'AI non cerca solo vincitori, ma analizza lo Spread e il Weight of Money per prevedere se la quota scenderà di 1-2 tick per chiudere una posizione in profitto garantito.

Gestione Ordini: Gli ordini non abbinati (Unmatched) vengono monitorati dall'Executor. Se non abbinati entro un timeout (es. 10s), vengono cancellati per liberare il saldo.

Commissioni: Ogni calcolo di profitto sottrae automaticamente la commissione Betfair (es. 5%) per mostrare solo il guadagno reale.

5. Sicurezza e Controllo Rischi
PFX Authentication: Utilizzo di certificati client per la comunicazione sicura.

Daily Loss Limit: Limite di perdita giornaliera codificato nel core.

IP Whitelisting: Accesso API limitato solo all'IP della VPS.

Exposure Control: Il bot non può impegnare più del X% del bankroll su un singolo mercato.

6. Roadmap di Sviluppo (Task List)
[ ] Setup Ambiente: Configurazione VPS e conversione certificati in .pfx.

[ ] Infrastructure: Installazione Redis e creazione Database SQLite tramite Entity Framework Core.

[ ] Connectivity: Sviluppo dell'Explorer per ricevere lo stream dei prezzi.

[ ] Execution: Sviluppo dell'Executor con gestione degli stati Matched/Unmatched.

[ ] Intelligence: Addestramento e caricamento modello ML.NET per il momentum.

[ ] Accounting: Implementazione del TradeLogger per il salvataggio dei profitti netti.

Prossimo Passo Consigliato
Il progetto è ora definito in ogni dettaglio. Vuoi che iniziamo scrivendo lo script C# per la TradeLogger e la configurazione del database SQLite? 
Questo ti permetterà di avere la base solida per monitorare i primi test che farai.