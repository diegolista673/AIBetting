# 🎯 Guida Rapida - Fix Dashboard Vuota in Blazor

## ❌ Problema
La dashboard Grafana si apre nell'iframe Blazor ma **non mostra dati/grafici**.

---

## ✅ Soluzione in 3 Step (5 minuti)

### **Step 1: Riavvia Explorer con Metriche** (2 minuti)

Explorer deve essere riavviato con il codice aggiornato che espone le metriche Prometheus.

```powershell
# Opzione A: Script automatico
powershell -ExecutionPolicy Bypass -File restart-explorer.ps1

# Opzione B: Manuale
# Terminal 1
cd AIBettingExplorer
dotnet run
```

**Verifica:**
```powershell
curl http://localhost:5001/metrics | Select-String "aibetting"
```

**Output atteso:**
```
aibetting_price_updates_total 45
aibetting_processing_latency_seconds_count 45
```

Se non vedi questo output, Explorer non sta generando metriche. Riavvialo.

---

### **Step 2: Importa Dashboard in Grafana** (2 minuti)

#### **Opzione A: Script Automatico (Consigliato)**

```powershell
powershell -ExecutionPolicy Bypass -File setup-complete-monitoring.ps1
```

Lo script:
1. ✅ Verifica prerequisiti
2. ✅ Importa dashboard automaticamente
3. ✅ Verifica dati in Prometheus
4. ✅ Apre browser su pagina monitoring

#### **Opzione B: Import Manuale**

1. **Apri Grafana:** http://localhost:3000
   - Login: `admin` / `admin`

2. **Vai su Dashboards → Import**
   - Click su "+" in sidebar
   - Click "Import"

3. **Upload JSON File**
   - Click "Upload JSON file"
   - Seleziona: `grafana-dashboard-explorer.json`
   - (Path: `C:\Users\SMARTW\source\repos\AIBettingSolution\grafana-dashboard-explorer.json`)

4. **Configura Import**
   - **Name:** AIBetting Explorer - Real-time Metrics (già precompilato)
   - **UID:** `aibetting-explorer` (IMPORTANTE! Non cambiare!)
   - **Folder:** General (o crea una nuova)
   - **Prometheus:** Seleziona "Prometheus" dal dropdown

5. **Click "Import"**
   - La dashboard si aprirà automaticamente
   - Dovresti vedere 6 panels con grafici

---

### **Step 3: Verifica in Blazor** (1 minuto)

1. **Apri Blazor Dashboard:**
   ```
   http://localhost:5000/monitoring
   ```

2. **Seleziona Dashboard:**
   - Nel dropdown, seleziona "Explorer Metrics"

3. **Verifica Grafici:**
   - Dovresti vedere grafici che si aggiornano ogni 5 secondi
   - Total Updates dovrebbe crescere
   - Rate dovrebbe essere ~2.5/sec

---

## 🔍 Troubleshooting

### ❌ Dashboard ancora vuota dopo import

**Causa:** Prometheus non sta ricevendo dati da Explorer

**Verifica:**
```powershell
# 1. Explorer genera metriche?
curl http://localhost:5001/metrics | Select-String "aibetting_price_updates_total"

# 2. Prometheus target UP?
start http://localhost:9090/targets
# Cerca "aibetting-explorer" → deve essere verde (UP)

# 3. Prometheus ha dati?
start http://localhost:9090/graph
# Query: aibetting_price_updates_total
# Click "Execute" → Dovresti vedere un numero
```

**Soluzione:**
```powershell
# Se Explorer metrics mancano → Riavvia Explorer
powershell -ExecutionPolicy Bypass -File restart-explorer.ps1

# Se Prometheus target DOWN → Riavvia Prometheus
docker restart aibetting-prometheus

# Aspetta 30 secondi e riprova
```

---

### ❌ "Dashboard not found" o UID errato

**Causa:** UID in `appsettings.json` non corrisponde a Grafana

**Verifica UID in Grafana:**
1. Apri dashboard in Grafana
2. Guarda URL: `/d/{UID}/...`
3. UID deve essere: `aibetting-explorer`

**Fix:**
```json
// AIBettingBlazorDashboard\appsettings.json
{
  "Monitoring": {
    "Dashboards": {
      "explorer": {
        "uid": "aibetting-explorer",  // ← Deve corrispondere!
        "name": "Explorer Metrics"
      }
    }
  }
}
```

Dopo la modifica, riavvia Blazor Dashboard.

---

### ❌ Iframe bloccato / "Connection refused"

**Causa:** Grafana non permette embedding

**Verifica:**
```powershell
docker exec aibetting-grafana env | Select-String "GF_SECURITY_ALLOW_EMBEDDING"
# Output atteso: GF_SECURITY_ALLOW_EMBEDDING=true
```

**Fix:**
```powershell
# Se la variabile non c'è, riavvia Grafana
docker-compose -f docker-compose.monitoring.yml down grafana
docker-compose -f docker-compose.monitoring.yml up -d grafana
```

---

### ❌ Grafici "No data" in Grafana

**Causa:** Dashboard configurata male o Prometheus data source sbagliato

**Fix:**
1. Apri dashboard in Grafana UI: http://localhost:3000/d/aibetting-explorer
2. Click su titolo di un panel → **Edit**
3. Verifica **Data Source** = "Prometheus"
4. Verifica **Query** = `aibetting_price_updates_total`
5. Click **Apply** e **Save dashboard**

---

## 🎯 Checklist Completa

Usa questa checklist per verificare tutto:

- [ ] **Explorer in esecuzione**
  ```powershell
  Get-Process | Where-Object { $_.ProcessName -like "*AIBetting*" }
  ```

- [ ] **Metriche esposte**
  ```powershell
  curl http://localhost:5001/metrics | Select-String "aibetting"
  # Deve mostrare almeno 3 metriche
  ```

- [ ] **Prometheus target UP**
  ```
  http://localhost:9090/targets → aibetting-explorer = UP (verde)
  ```

- [ ] **Prometheus ha dati**
  ```
  http://localhost:9090/graph → Query: aibetting_price_updates_total
  # Deve restituire un valore > 0
  ```

- [ ] **Dashboard importata in Grafana**
  ```
  http://localhost:3000/dashboards → "AIBetting Explorer" presente
  ```

- [ ] **UID corretto**
  ```
  http://localhost:3000/d/aibetting-explorer → Dashboard si apre
  ```

- [ ] **Embedding abilitato**
  ```powershell
  docker exec aibetting-grafana env | Select-String "ALLOW_EMBEDDING"
  # Output: GF_SECURITY_ALLOW_EMBEDDING=true
  ```

- [ ] **Blazor appsettings.json configurato**
  ```json
  "Monitoring": { "Dashboards": { "explorer": { "uid": "aibetting-explorer" }}}
  ```

- [ ] **Dashboard visibile in Blazor**
  ```
  http://localhost:5000/monitoring → Grafici visibili e si aggiornano
  ```

---

## 🚀 Quick Fix - All-in-One

Se vuoi risolvere tutto in un colpo:

```powershell
# 1. Riavvia Explorer
powershell -ExecutionPolicy Bypass -File restart-explorer.ps1

# 2. Aspetta 10 secondi
Start-Sleep -Seconds 10

# 3. Setup completo
powershell -ExecutionPolicy Bypass -File setup-complete-monitoring.ps1

# 4. Apri Blazor
start http://localhost:5000/monitoring
```

**Tempo totale:** 3-5 minuti

---

## 📊 Output Atteso (Quando Tutto Funziona)

### Blazor Dashboard - http://localhost:5000/monitoring

```
┌────────────────────────────────────────────┐
│ System Monitoring                          │
│ [Refresh] [Open in Grafana] [Fullscreen]  │
│                                             │
│ Dashboard: [Explorer Metrics ▼]            │
│                                             │
│ ┌────────────────────────────────────────┐ │
│ │ Total Price Updates                    │ │
│ │ 245                                    │ │
│ ├────────────────────────────────────────┤ │
│ │ Price Updates Rate                     │ │
│ │ [Grafico linea crescente ~2.5/sec]    │ │
│ ├────────────────────────────────────────┤ │
│ │ Processing Latency (p50/p95/p99)       │ │
│ │ [3 linee: 3ms, 12ms, 25ms]            │ │
│ ├────────────────────────────────────────┤ │
│ │ Average Latency: 3.5ms                 │ │
│ │ p95 Latency: 12ms ✅                   │ │
│ └────────────────────────────────────────┘ │
└────────────────────────────────────────────┘
```

---

## 📞 Se Ancora Non Funziona

Se dopo tutti questi step la dashboard è ancora vuota:

1. **Cattura Screenshot:**
   - Blazor page con iframe
   - Grafana standalone (http://localhost:3000/d/aibetting-explorer)
   - Browser DevTools Console (F12)

2. **Raccogli Logs:**
   ```powershell
   # Explorer logs
   Get-Content "AIBettingExplorer\logs\explorer-*.log" -Tail 50

   # Grafana logs
   docker logs aibetting-grafana --tail 50
   ```

3. **Verifica Network:**
   - F12 → Network tab
   - Filtra per "aibetting" o "metrics"
   - Verifica HTTP status codes

---

**Creato:** 2026-01-09  
**Versione:** 1.0  
**Tempo Stimato:** 5 minuti
