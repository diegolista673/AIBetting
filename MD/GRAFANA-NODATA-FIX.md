# 🔧 Grafana Dashboard - Risoluzione "No Data"

## ❌ **PROBLEMA: Dashboard Aperta ma Mostra "No Data"**

**Sintomi:**
- Script di verifica conferma che tutto funziona ✅
- Prometheus ha dati (9275+ snapshots) ✅  
- Dashboard si apre ma panels mostrano "No data" ❌

---

## 🎯 **CAUSA PRINCIPALE: Time Range**

Il problema più comune è che il **time range** della dashboard è impostato su un periodo dove non ci sono dati storici (es: "Last 24 hours" ma Analyst è stato avviato da poche ore).

---

## ✅ **SOLUZIONE IMMEDIATA**

### **Step 1: Cambia Time Range**

1. Apri dashboard: http://localhost:3000/d/aibetting-analyst

2. **Click sul time picker** (in alto a destra, icona orologio)

3. Seleziona **"Last 15 minutes"** o **"Last 5 minutes"**

4. Click **"Apply"**

5. Click icona **"Refresh"** (freccia circolare)

**Screenshot guida:**
```
┌─────────────────────────────┐
│ [Dashboard Title]  [⏰ Last 15 minutes ▼] [🔄] │
                        ↑
                  CLICK QUI
```

---

### **Step 2: Force Refresh**

Se dopo aver cambiato il time range ancora non vedi dati:

1. **Premi `Ctrl + R`** (force refresh pagina)

2. Oppure click **"Refresh dashboard"** icon (in alto a destra)

3. Oppure cambia intervallo auto-refresh:
   - Click dropdown accanto a "Refresh"
   - Seleziona **"5s"** (ogni 5 secondi)

---

## 🔍 **VERIFICA QUERY PANELS**

Se ancora non funziona, verifica che le query nei panels siano corrette:

### **Panel 1: Total Snapshots**
```promql
aibetting_analyst_snapshots_processed_total
```

### **Panel 2: Surebets Found**
```promql
aibetting_analyst_surebets_found_total
```

### **Panel 3: Processing Latency**
```promql
histogram_quantile(0.95, rate(aibetting_analyst_processing_latency_seconds_bucket[1m]))
```

---

## 🛠️ **FIX MANUALE PANELS**

Se i panels sono vuoti o mostrano errori di query:

### **Modifica Panel:**

1. Hover sul panel
2. Click **menu (⋮)** in alto a destra del panel
3. Click **"Edit"**
4. Nella sezione **"Query"**:
   - **Data source**: Seleziona **"Prometheus"**
   - **Query**: Inserisci la query corretta (vedi sopra)
5. Click **"Apply"** (in alto a destra)

---

## 📊 **TEST QUERY MANUALMENTE**

Prima di modificare la dashboard, testa le query direttamente in Prometheus:

1. Apri: **http://localhost:9090/graph**

2. Inserisci query:
   ```
   aibetting_analyst_snapshots_processed_total
   ```

3. Click **"Execute"**

4. Dovresti vedere **Value: 9275+**

5. Vai su tab **"Graph"** per vedere il grafico

**Se questo funziona**, il problema è nella configurazione della dashboard Grafana.

---

## 🔧 **RICREA DASHBOARD (Se Necessario)**

Se nulla funziona, ricrea la dashboard da zero:

### **Step 1: Export Query da Prometheus**

```powershell
# Test che metriche esistono
curl http://localhost:5002/metrics | Select-String "aibetting_analyst"
```

### **Step 2: Crea Nuovo Panel in Grafana**

1. Apri: http://localhost:3000/dashboards

2. Click **"+ New"** → **"New Dashboard"**

3. Click **"Add visualization"**

4. Seleziona data source: **"Prometheus"**

5. Query:
   ```
   aibetting_analyst_snapshots_processed_total
   ```

6. Visualization type: **"Stat"**

7. Panel title: **"Total Snapshots Processed"**

8. Click **"Apply"**

9. **IMPORTANTE**: 
   - Time range: **"Last 15 minutes"**
   - Refresh: **"5s"**

10. Click **"Save dashboard"** (icona 💾 in alto)
    - Title: `AIBetting Analyst - Test`
    - UID: `aibetting-analyst-test`

---

## 🎯 **VERIFICA DATA SOURCE SETTINGS**

Assicurati che il data source Prometheus punti all'URL corretto:

1. Apri: http://localhost:3000/datasources

2. Click su **"Prometheus"**

3. Verifica:
   - **URL**: `http://prometheus:9090` (se in Docker)  
     O `http://localhost:9090` (se locale)
   
4. Scroll down e click **"Save & test"**

5. Dovresti vedere: ✅ **"Data source is working"**

---

## 🔍 **DEBUGGING AVANZATO**

### **Check 1: Inspect Panel Query**

1. Click panel menu (⋮) → **"Inspect"** → **"Query"**

2. Verifica:
   - **Status**: Deve essere "OK" (200)
   - **Response**: Deve contenere `"data":{"result":[...]}`
   - **Frames**: Deve mostrare dati

3. Se vedi errore 404 o 500:
   - Data source non configurato correttamente
   - URL Prometheus errato

### **Check 2: Browser Console**

1. Premi **F12** (apri Developer Tools)

2. Vai su tab **"Console"**

3. Cerca errori JavaScript o API errors

4. Cerca **"Network"** tab:
   - Filtra per "api/datasources"
   - Verifica che le chiamate ritornino 200 OK

### **Check 3: Grafana Logs**

```powershell
# Se Grafana in Docker
docker logs aibetting-grafana | Select-String "error" -Context 2
```

---

## 📋 **CHECKLIST COMPLETA**

- [ ] Time range impostato su "Last 15 minutes"
- [ ] Auto-refresh attivo (5s o 10s)
- [ ] Data source Prometheus configurato e testato
- [ ] Query panels corrette (senza typo)
- [ ] Prometheus ha dati (test manuale OK)
- [ ] Browser cache svuotata (Ctrl+Shift+R)
- [ ] Dashboard salvata dopo modifiche

---

## 🚀 **QUICK FIX - Script PowerShell**

```powershell
# quick-fix-dashboard.ps1
Write-Host "`n=== QUICK FIX GRAFANA DASHBOARD ===" -ForegroundColor Cyan

# 1. Verifica dati esistono
Write-Host "`n[1] Verifica dati Prometheus..." -ForegroundColor Yellow
$q = [System.Web.HttpUtility]::UrlEncode("aibetting_analyst_snapshots_processed_total")
$r = Invoke-RestMethod "http://localhost:9090/api/v1/query?query=$q"
if ($r.data.result.Count -gt 0) {
    $value = $r.data.result[0].value[1]
    Write-Host "   OK - Dati disponibili: $value snapshots" -ForegroundColor Green
} else {
    Write-Host "   ERROR - Nessun dato in Prometheus!" -ForegroundColor Red
    exit
}

# 2. Test data source Grafana
Write-Host "`n[2] Test Grafana data source..." -ForegroundColor Yellow
try {
    $auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:admin"))
    $headers = @{"Authorization" = "Basic $auth"}
    $ds = Invoke-RestMethod "http://localhost:3000/api/datasources" -Headers $headers
    $promDs = $ds | Where-Object { $_.type -eq "prometheus" } | Select-Object -First 1
    
    if ($promDs) {
        Write-Host "   OK - Data source trovato (ID: $($promDs.id))" -ForegroundColor Green
        
        # Test query via Grafana
        $testQuery = [System.Web.HttpUtility]::UrlEncode("aibetting_analyst_snapshots_processed_total")
        $now = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
        $proxyUrl = "http://localhost:3000/api/datasources/proxy/$($promDs.id)/api/v1/query?query=$testQuery&time=$now"
        $testResult = Invoke-RestMethod $proxyUrl -Headers $headers
        
        if ($testResult.data.result.Count -gt 0) {
            Write-Host "   OK - Query via Grafana funziona: $($testResult.data.result[0].value[1])" -ForegroundColor Green
        } else {
            Write-Host "   ERROR - Query via Grafana senza dati!" -ForegroundColor Red
        }
    } else {
        Write-Host "   ERROR - Data source Prometheus non trovato!" -ForegroundColor Red
    }
} catch {
    Write-Host "   ERROR - Grafana non risponde: $_" -ForegroundColor Red
}

# 3. Suggerimenti
Write-Host "`n=== SUGGERIMENTI ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Se vedi 'OK' sopra ma la dashboard mostra 'No data':" -ForegroundColor Yellow
Write-Host "  1. Apri dashboard: http://localhost:3000/d/aibetting-analyst" -ForegroundColor White
Write-Host "  2. Cambia Time Range: 'Last 15 minutes'" -ForegroundColor White
Write-Host "  3. Click Refresh (icona freccia circolare)" -ForegroundColor White
Write-Host "  4. Premi Ctrl+R per force refresh browser" -ForegroundColor White
Write-Host ""
```

**Salva come `quick-fix-dashboard.ps1` ed esegui:**
```powershell
.\quick-fix-dashboard.ps1
```

---

## ✅ **SOLUZIONE GARANTITA**

Se **NULLA** funziona, usa questo approccio step-by-step:

1. **Apri Prometheus UI**: http://localhost:9090/graph
   - Query: `aibetting_analyst_snapshots_processed_total`
   - Verifica che ritorna dati

2. **Se Prometheus ha dati ma Grafana no**:
   - Problema è nel data source Grafana
   - Fix: Ricrea data source con URL corretto

3. **Se Grafana query funziona ma dashboard no**:
   - Problema è nel time range o panel config
   - Fix: Cambia time range a "Last 15 minutes"

4. **Se time range è corretto ma ancora no data**:
   - Problema è cache browser
   - Fix: `Ctrl+Shift+R` (hard refresh)

---

## 📞 **LINK UTILI**

| Risorsa | URL |
|---------|-----|
| **Dashboard** | http://localhost:3000/d/aibetting-analyst |
| Prometheus Graph | http://localhost:9090/graph |
| Prometheus Targets | http://localhost:9090/targets |
| Grafana Data Sources | http://localhost:3000/datasources |
| Analyst Metrics | http://localhost:5002/metrics |

---

## 🎯 **VALORE ATTESO**

Una volta risolto, dovresti vedere:

```
┌─────────────────────────────────────┐
│ Total Snapshots Processed           │
│ 9275                         ✅     │
├─────────────────────────────────────┤
│ Surebets Found                      │
│ 0                                   │
├─────────────────────────────────────┤
│ Processing Latency (p95)            │
│ [Grafico con linea ~15-20ms]       │
└─────────────────────────────────────┘
```

**Auto-refresh ogni 5 secondi** dovrebbe aggiornare i valori in real-time.

---

**Creato:** 2026-01-12 14:50  
**Status:** Guida Completa Risoluzione "No Data"  
**Dati Confermati:** Prometheus ha 9275+ snapshots
