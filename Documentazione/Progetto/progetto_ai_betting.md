# Progetto: AI Betting Exchange Multi-Agent System (AIBE-MAS)

## 1. Visione del Progetto
Creazione di un framework multi-agente per l'automazione del trading sportivo su Betfair Exchange. Il sistema mira a identificare discrepanze di prezzo (Surebet) e gestire posizioni di trading (Green Up) attraverso modelli predittivi e algoritmi di esecuzione rapida.

---

## 2. Architettura del Sistema
Il sistema è diviso in tre agenti principali che operano in parallelo:

### A. Agente Esploratore (Data Sentinel)
* **Monitoraggio:** Utilizza le Betfair Stream API per dati in tempo reale.
* **Funzione:** Scansiona mercati con alta liquidità e confronta le quote con bookmaker esterni (via API o scraping).
* **Output:** Segnali di potenziali Surebet o anomalie di volume.

### B. Agente Analista (Intelligence Engine)
* **Previsione Trend:** Utilizza modelli LSTM (Long Short-Term Memory) per prevedere la direzione delle quote (Steamers/Drifters).
* **Calcolo Valore:** Valuta l'Expected Value ($EV$) e le probabilità reali tramite modelli statistici (es. Poisson per il calcio).
* **Decision Making:** Decide se entrare in una posizione "Back" per uscire in "Lay" (Trading).

### C. Agente Esecutore (Execution & Risk)
* **Piazzamento Ordini:** Gestisce le API di scommessa con logica di retry e controllo latenza.
* **Money Management:** Implementa il Criterio di Kelly Frazionato.
* **Safety:** Kill-switch automatico se la perdita massima giornaliera viene raggiunta.

---

## 3. Logica Matematica Core

### Formula per Surebet (Arbitraggio)
Un'opportunità esiste se la somma delle probabilità inverse è minore di 1:
$$P_{total} = \frac{1}{Q_{back1}} + \frac{1}{Q_{back2}} + ... < 1$$

### Calcolo Green Up (Profitto Garantito nel Trading)
Per uscire da una posizione con lo stesso profitto indipendentemente dal risultato:
$$\text{Stake Lay} = \frac{\text{Stake Back} \times \text{Quota Back}}{\text{Quota Lay}}$$

---

## 4. Stack Tecnologico Consigliato
* **Linguaggio:** Python 3.10+
* **Interfaccia API:** `betfairlightweight`
* **Analisi Dati:** `pandas`, `numpy`
* **Machine Learning:** `scikit-learn`, `tensorflow` (per modelli predittivi)
* **Database:** Redis (per memorizzare lo stato dei mercati in tempo reale)

---

## 5. Script di Esempio (Connessione e Monitoraggio)

#python
import betfairlightweight
from betfairlightweight import filters

# Configurazione (Sostituire con i propri dati)
APP_KEY = "tua_app_key"
USERNAME = "tuo_username"
CERT_PATH = "/percorso/certificati/"

client = betfairlightweight.APIClient(USERNAME, "password", app_key=APP_KEY, certs_path=CERT_PATH)

def fetch_market_prices(market_id):
    """Esempio di recupero prezzi per l'Agente Esploratore"""
    market_book = client.betting.list_market_book(
        market_ids=[market_id],
        price_projection={'priceData': ['EX_BEST_OFFERS']}
    )
    if market_book:
        runner = market_book[0].runners[0]
        best_back = runner.ex.available_to_back[0]['price']
        best_lay = runner.ex.available_to_lay[0]['price']
        return best_back, best_lay
    return None

# Esecuzione Login
# client.login()

Analisi Architetturale: Agenti AI vs Software Verticale
Nel mondo del betting exchange, la scelta dell'architettura determina il successo della strategia. 
Esistono due strade principali, con una terza via "ibrida" che rappresenta lo stato dell'arte.

1. Software Verticale (Approccio Deterministico)Un unico blocco di codice ottimizzato per la velocità, basato su regole fisse (if-then-else).
Punti di Forza: * Latenza Quasi-Zero: Ideale per lo scalping dove i millisecondi contano.
Determinismo: Sai sempre esattamente perché il bot ha effettuato una scommessa.
Punti di Debolezza: * Fragilità: Non si adatta a mercati volatili o eventi imprevisti.
Limiti Matematici: Difficilmente riesce a calcolare variabili complesse (es. il "valore" reale di una quota basato su trend storici).

2. Sistema Multi-Agente (Approccio Probabilistico)
Componenti indipendenti che comunicano tra loro, spesso basati su modelli di Machine Learning.
Punti di Forza:Adattabilità: L'agente analista può imparare dai mercati passati.
Modularità: È possibile aggiornare l'algoritmo di analisi senza toccare il codice che gestisce i soldi.
Punti di Debolezza:Overhead: La comunicazione tra agenti può introdurre latenza (millisecondi preziosi).

3. L'Architettura Consigliata: Il Modello IbridoLa soluzione migliore è separare il "Corpo" (Velocità) dal "Cervello" (Strategia).
Struttura IbridaComponenteTipoFunzionePrioritàExecution CoreVerticaleConnessione API, piazzamento ordini, Kill-switch.
LatenzaStrategy AgentAI/AnaliticoAnalisi trend, calcolo EV, aggiornamento parametri.
IntelligenzaCome Funzionano InsiemeL'Execution Core corre in un loop infinito a 100ms, leggendo i prezzi.
L'Strategy Agent gira in background, analizza i dati ogni 2-5 secondi e aggiorna una "Tabella di Comando" (es. in Redis).
L'Execution Core consulta la tabella: se l'agente ha marcato una quota come "Value", il core la colpisce istantaneamente.


4. Aggiornamento Logica di Gestione (MD)Strategia Surebet (Esecuzione Verticale)Input: API Betfair + API Bookmaker esterno.
Logica: Verifica istantanea $1/Q_1 + 1/Q_2 < 1$.Azione: Esecuzione atomica (punta e banca simultaneamente).
Strategia Posizioni Vincenti (Gestione Agente AI)Input: Flusso volumi (Traded Volume) + Time-to-match.
Logica AI: Modello di Reinforcement Learning che valuta il rischio/beneficio di restare nel mercato.
Azione: Invia segnale di "Green Up" se la probabilità che la quota salga è superiore al 65%.

Conclusioni per lo SviluppoNon costruire un "unico agente" che fa tutto. 
Costruisci un motore di esecuzione ultra-veloce e "inietta" l'intelligenza tramite un agente analista separato. 
Questo ti permette di:Non perdere le Surebet per colpa della lentezza dell'AI.
Non perdere soldi nel trading per colpa della stupidità di un software fisso.

6. Roadmap di Sviluppo
[ ] Configurazione ambiente e autenticazione API.

[ ] Sviluppo del modulo di scraping/confronto quote per Surebet.

[ ] Creazione del database storico per il training dell'AI.

[ ] Test in modalità "Dry Run" (senza soldi reali).

[ ] Deploy finale su VPS.

### Strategia: Arbitraggio Interno (Market Making)
**Obiettivo:** Guadagnare dallo spread tra Back e Lay sullo stesso mercato.

**Algoritmo di Esecuzione:**
1. Identifica mercato con Spread > 2 tick e Volume > €100.000.
2. Inserisci ordine LAY al prezzo 'Best Back'.
3. Inserisci ordine BACK al prezzo 'Best Lay'.
4. Se (Prezzo_Back - Prezzo_Lay) > Commissione_Betfair -> Profitto Certo.

**Gestione Rischio Live:**
- Se un lato dell'ordine viene abbinato e l'altro no entro 30 secondi, l'AI deve eseguire un "Hedge" (copertura) immediata al prezzo corrente per limitare la perdita (Stop Loss).


### Modulo: Gestione Uscite (Green Up Optimizer)
**Logica:** Massimizzazione del rendimento per trade riducendo l'esposizione temporale.

**Parametri di Input:**
- `entry_price`: Quota di acquisto.
- `current_market_price`: Quota attuale live.
- `target_roi`: ROI minimo desiderato per attivare il Green Up (es. +15%).
- `risk_alert`: Segnale dall'AI analista di potenziale inversione di trend.

**Azione:**
- Se `current_market_price` <= `entry_price` * 0.7 (Crollo del 30%) -> Esegui Green Up.
- Se `risk_alert` == True -> Esegui Green Up immediato indipendentemente dal profitto.

### Modulo: AI Momentum Analyzer
**Obiettivo:** Identificare discrepanze tra l'intensità del gioco e la quota live.

**Metriche Chiave:**
- **Dangerous Attack Ratio:** Rapporto tra attacchi totali e attacchi negli ultimi 5 minuti.
- **Shot Conversion Probability:** Stima della qualità dei tiri effettuati (xG live).
- **Market Lag:** Tempo di reazione della quota Betfair rispetto all'evento statistico rilevato.

**Logica di Ingresso:**
Se (Momentum_Score > Soglia_X) E (Quota_Betfair > Quota_Modello_AI + 10%) -> Esegui PUNTATA (Value Bet Live).


Certamente. Per visualizzare come interagiscono tutti i componenti che abbiamo analizzato (dalla raccolta dati al piazzamento della scommessa), il modo migliore è utilizzare un **Workflow Ciclico**.

Questo grafico rappresenta il flusso logico del sistema che stiamo progettando.

---

## Workflow del Sistema AIBE-MAS

Il processo può essere riassunto in quattro fasi principali che girano in loop continuo:

### 1. Ingestione Dati (Input)

* **Modulo:** Agente Esploratore.
* **Flussi:**
* API Betfair (Quote in tempo reale).
* API Statistiche (Tiri, possesso, corner live).
* Storico JSON (Dati passati per il contesto).



### 2. Elaborazione Strategica (Cervello)

* **Modulo:** Agente Analista AI.
* **Operazioni:**
* Calcolo del **Momentum Score**.
* Stima del **Fair Price** (Quota reale).
* Definizione del Target di uscita (**Green Up**).


* **Output:** Invia i parametri a **Redis**.

### 3. Sincronizzazione (Ponte)

* **Modulo:** **Redis Database**.
* **Funzione:** Agisce come memoria a breve termine. Permette all'analista di essere "intelligente" (più lento) e all'esecutore di essere "reattivo" (velocissimo) senza che uno blocchi l'altro.

### 4. Esecuzione e Monitoraggio (Azione)

* **Modulo:** Execution Core + Watchdog.
* **Operazioni:**
* Confronto tra Quota Betfair e Target su Redis.
* Piazzamento ordine atomico.
* Controllo dei rischi e **Stop Loss**.



---

## Riassunto per lo Sviluppo (Roadmap Finale)

Se dovessi iniziare a scrivere il codice domani, questo sarebbe l'ordine di priorità:

1. **Infrastruttura:** Installa Redis e configura le API di Betfair con i certificati SSL.
2. **Data Logger:** Crea lo script che salva i dati in JSON (fondamentale per addestrare l'AI).
3. **Execution Core:** Scrivi il bot "stupido" che punta e banca solo quando riceve un comando manuale su Redis.
4. **Agente Analista:** Implementa la logica matematica del Green Up e, successivamente, il modello ML per il Momentum.
5. **Watchdog:** Aggiungi lo script di sicurezza che chiude tutto in caso di errore.

**Ti piacerebbe che traducessi questo workflow in una struttura di cartelle e file Python pronta per essere creata sul tuo PC?**