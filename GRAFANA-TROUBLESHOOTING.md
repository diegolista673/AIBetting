# 🔧 Grafana Dashboard Analyst - Guida Troubleshooting Completa

## ❌ **PROBLEMA: Dashboard Non Mostra Dati**

---

## 🎯 **Checklist Diagnostica Rapida**

Esegui questi comandi in ordine per identificare il problema:

```powershell
# 1. Verifica Analyst attivo
Get-Process -Name "AIBettingAnalyst" -ErrorAction SilentlyContinue

# 2. Verifica metriche disponibili
curl http://localhost:5002/metrics | Select-String "aibetting_analyst_snapshots"

# 3. Verifica Prometheus target
Invoke-RestMethod "http://localhost:9090/api/v1/targets" | 
    Select-Object -ExpandProperty data | 
    Select-Object -ExpandProperty activeTargets | 
    Where-Object { $_.labels.job -eq "aibetting-analyst" }

# 4. Test query Prometheus
$query = [System.Web.HttpUtility]::UrlEncode("aibetting_analyst_snapshots_processed_total")
Invoke-RestMethod "http://localhost:9090/api/v1/query?query=$query"

# 5. Verifica dashboard esiste
$auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:admin"))
$headers = @{"Authorization" = "Basic $auth"}
Invoke-RestMethod -Uri "http://localhost:3000/api/search?type=dash-db" -Headers $headers | 
    Where-Object { $_.uid -eq "aibetting-analyst" }
```

---

## 🔍 **Possibili Cause e Soluzioni**

### **Causa 1: Dashboard Non Esiste**

**Sintomi:**
- Errore 404 su `http://localhost:3000/d/aibetting-analyst`
- Dashboard non compare nella lista

**Verifica:**
```powershell
$auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:admin"))
$headers = @{"Authorization" = "Basic $auth"}
$dashboards = Invoke-RestMethod -Uri "http://localhost:3000/api/search?type=dash-db" -Headers $headers
$dashboards | Where-Object { $_.title -like "*Analyst*" }
```

**Soluzione:**

1. **Crea il file dashboard JSON** (se non esiste):

```json
{
  "dashboard": {
    "title": "AIBetting Analyst - Real-time Performance",
    "uid": "aibetting-analyst",
    "tags": ["aibetting", "analyst"],
    "timezone": "browser",
    "schemaVersion": 38,
    "version": 1,
    "refresh": "5s",
    "panels": [
      {
        "id": 1,
        "title": "Total Snapshots Processed",
        "type": "stat",
        "targets": [
          {
            "expr": "aibetting_analyst_snapshots_processed_total",
            "refId": "A"
          }
        ],
        "gridPos": {"h": 8, "w": 6, "x": 0, "y": 0}
      },
      {
        "id": 2,
        "title": "Surebets Found",
        "type": "stat",
        "targets": [
          {
            "expr": "aibetting_analyst_surebets_found_total",
            "refId": "A"
          }
        ],
        "gridPos": {"h": 8, "w": 6, "x": 6, "y": 0}
      },
      {
        "id": 3,
        "title": "Processing Latency (p95)",
        "type": "timeseries",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, rate(aibetting_analyst_processing_latency_seconds_bucket[1m]))",
            "refId": "A"
          }
        ],
        "gridPos": {"h": 8, "w": 12, "x": 0, "y": 8}
      }
    ]
  },
  "overwrite": true
}
```

2. **Import via API:**
```powershell
$auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:admin"))
$headers = @{
    "Authorization" = "Basic $auth"
    "Content-Type" = "application/json"
}
$body = Get-Content "dashboard-analyst.json" -Raw
Invoke-RestMethod -Uri "http://localhost:3000/api/dashboards/db" -Method Post -Headers $headers -Body $body
```

3. **O import manuale:**
   - Apri http://localhost:3000
   - Click su "+" → "Import"
   - Upload il file JSON
   - Seleziona data source "Prometheus"
   - Click "Import"

---

### **Causa 2: Data Source Non Configurato**

**Sintomi:**
- Dashboard visibile ma panels vuoti
- Errore "No data" su tutti i panels
- Query non ritorna risultati

**Verifica:**
```powershell
$auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:admin"))
$headers = @{"Authorization" = "Basic $auth"}
$datasources = Invoke-RestMethod -Uri "http://localhost:3000/api/datasources" -Headers $headers
$datasources | Where-Object { $_.type -eq "prometheus" }
```

**Output atteso:**
```
id          : 1
uid         : prometheus-uid
name        : Prometheus
type        : prometheus
url         : http://prometheus:9090
isDefault   : True
```

**Soluzione se manca:**

```powershell
# Crea data source Prometheus
$auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:admin"))
$headers = @{
    "Authorization" = "Basic $auth"
    "Content-Type" = "application/json"
}

$datasource = @{
    name = "Prometheus"
    type = "prometheus"
    url = "http://prometheus:9090"
    access = "proxy"
    isDefault = $true
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:3000/api/datasources" -Method Post -Headers $headers -Body $datasource
```

---

### **Causa 3: Prometheus Target Non Configurato**

**Sintomi:**
- Target "aibetting-analyst" non presente in http://localhost:9090/targets
- O target presente ma status "DOWN"

**Verifica:**
```powershell
$targets = Invoke-RestMethod "http://localhost:9090/api/v1/targets"
$analystTarget = $targets.data.activeTargets | Where-Object { $_.labels.job -eq "aibetting-analyst" }

if ($analystTarget) {
    Write-Host "Target trovato: $($analystTarget.health)"
    Write-Host "Endpoint: $($analystTarget.scrapeUrl)"
    if ($analystTarget.lastError) {
        Write-Host "Errore: $($analystTarget.lastError)" -ForegroundColor Red
    }
} else {
    Write-Host "Target NON trovato!" -ForegroundColor Red
}
```

**Soluzione:**

1. **Verifica `prometheus.yml`:**
```yaml
scrape_configs:
  - job_name: 'aibetting-analyst'
    scrape_interval: 5s
    static_configs:
      - targets: ['192.168.208.1:5002']  # Usa IP host
        labels:
          service: 'analyst'
```

2. **Trova IP corretto:**
```powershell
# Metodo 1: Get-NetIPAddress
Get-NetIPAddress -AddressFamily IPv4 -InterfaceAlias "Ethernet*" | 
    Select-Object IPAddress

# Metodo 2: ipconfig
ipconfig | Select-String "IPv4" | Select-Object -First 1
```

3. **Riavvia Prometheus:**
```powershell
docker restart aibetting-prometheus
Start-Sleep -Seconds 10
```

4. **Verifica target UP:**
```
http://localhost:9090/targets
```

---

### **Causa 4: Analyst Non In Esecuzione**

**Sintomi:**
- Processo non attivo
- Port 5002 non risponde

**Verifica:**
```powershell
# Check processo
Get-Process -Name "AIBettingAnalyst" -ErrorAction SilentlyContinue

# Check porta
Test-NetConnection localhost -Port 5002 -InformationLevel Quiet
```

**Soluzione:**
```powershell
# Avvia Analyst
cd C:\Users\SMARTW\source\repos\AIBettingSolution\AIBettingAnalyst
dotnet run

# O in background
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd C:\Users\SMARTW\source\repos\AIBettingSolution\AIBettingAnalyst; dotnet run"
```

---

### **Causa 5: Metriche Non Incrementano**

**Sintomi:**
- Snapshots sempre a 0
- Nessun dato nuovo

**Verifica:**
```powershell
# Check valore attuale
$metrics = Invoke-WebRequest -Uri "http://localhost:5002/metrics" -UseBasicParsing
if ($metrics.Content -match "aibetting_analyst_snapshots_processed_total\s+(\d+)") {
    Write-Host "Snapshots: $($matches[1])"
}

# Attendi 10 secondi e ricontrolla
Start-Sleep -Seconds 10
$metrics2 = Invoke-WebRequest -Uri "http://localhost:5002/metrics" -UseBasicParsing
if ($metrics2.Content -match "aibetting_analyst_snapshots_processed_total\s+(\d+)") {
    Write-Host "Snapshots dopo 10s: $($matches[1])"
}
```

**Se valore non cambia:**

1. **Verifica Explorer attivo:**
```powershell
Get-Process -Name "AIBettingExplorer" -ErrorAction SilentlyContinue
```

2. **Verifica Explorer pubblica:**
```powershell
# Check Explorer metrics
curl http://localhost:5001/metrics | Select-String "aibetting_price_updates"
```

3. **Verifica connessione Redis:**
```powershell
docker exec aibetting-redis redis-cli ping
# Output atteso: PONG
```

4. **Check logs Analyst:**
```powershell
Get-Content "AIBettingAnalyst\logs\analyst-*.log" -Tail 50
```

---

### **Causa 6: Time Range Errato**

**Sintomi:**
- Dashboard mostra "No data" ma Prometheus ha dati
- Query manuale funziona

**Soluzione:**

1. Apri dashboard: http://localhost:3000/d/aibetting-analyst
2. Click su **time picker** (in alto a destra)
3. Seleziona **"Last 15 minutes"**
4. Click **"Apply"**
5. Click icona **"Refresh"**

---

### **Causa 7: Query Syntax Error**

**Sintomi:**
- Panel mostra errore query
- "Bad data" o "Parse error"

**Verifica query corrette:**

```promql
# Panel 1: Snapshots
aibetting_analyst_snapshots_processed_total

# Panel 2: Surebets
aibetting_analyst_surebets_found_total

# Panel 3: Signals
sum(aibetting_analyst_signals_generated_total)

# Panel 4: Latency p95
histogram_quantile(0.95, rate(aibetting_analyst_processing_latency_seconds_bucket[1m]))

# Panel 5: Rate
rate(aibetting_analyst_snapshots_processed_total[1m]) * 60
```

**Test query manualmente:**
```
http://localhost:9090/graph
```
Inserisci query e verifica risultato.

---

## 🚀 **Script Automatico di Fix**

Salva come `fix-grafana-analyst.ps1`:

```powershell
# fix-grafana-analyst.ps1
Write-Host "`n=== FIX GRAFANA ANALYST DASHBOARD ===" -ForegroundColor Cyan

# 1. Check Analyst processo
Write-Host "`n[1/7] Check Analyst processo..." -ForegroundColor Yellow
$proc = Get-Process -Name "AIBettingAnalyst" -ErrorAction SilentlyContinue
if (-not $proc) {
    Write-Host "   ❌ Analyst non attivo - Avvialo!" -ForegroundColor Red
    Write-Host "   Comando: cd AIBettingAnalyst; dotnet run" -ForegroundColor Yellow
    exit
}
Write-Host "   ✅ Analyst attivo (PID: $($proc.Id))" -ForegroundColor Green

# 2. Check metriche endpoint
Write-Host "`n[2/7] Check metriche endpoint..." -ForegroundColor Yellow
try {
    $metrics = Invoke-WebRequest -Uri "http://localhost:5002/metrics" -UseBasicParsing -TimeoutSec 2
    Write-Host "   ✅ Endpoint OK" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Endpoint non risponde" -ForegroundColor Red
    exit
}

# 3. Check Prometheus target
Write-Host "`n[3/7] Check Prometheus target..." -ForegroundColor Yellow
try {
    $targets = Invoke-RestMethod -Uri "http://localhost:9090/api/v1/targets" -TimeoutSec 3
    $analyst = $targets.data.activeTargets | Where-Object { $_.labels.job -eq "aibetting-analyst" }
    
    if (-not $analyst) {
        Write-Host "   ❌ Target non configurato!" -ForegroundColor Red
        Write-Host "   Fix: Verifica prometheus.yml e riavvia Prometheus" -ForegroundColor Yellow
        Write-Host "   Comando: docker restart aibetting-prometheus" -ForegroundColor Yellow
    } elseif ($analyst.health -ne "up") {
        Write-Host "   ❌ Target DOWN: $($analyst.lastError)" -ForegroundColor Red
    } else {
        Write-Host "   ✅ Target UP" -ForegroundColor Green
    }
} catch {
    Write-Host "   ❌ Prometheus non raggiungibile" -ForegroundColor Red
}

# 4. Check Prometheus data
Write-Host "`n[4/7] Check Prometheus data..." -ForegroundColor Yellow
try {
    $query = [System.Web.HttpUtility]::UrlEncode("aibetting_analyst_snapshots_processed_total")
    $result = Invoke-RestMethod -Uri "http://localhost:9090/api/v1/query?query=$query" -TimeoutSec 3
    
    if ($result.data.result.Count -gt 0) {
        $value = $result.data.result[0].value[1]
        Write-Host "   ✅ Dati disponibili (Value: $value)" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  No data - Attendi primo scrape (5s)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ❌ Query fallita" -ForegroundColor Red
}

# 5. Check Grafana data source
Write-Host "`n[5/7] Check Grafana data source..." -ForegroundColor Yellow
try {
    $auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:admin"))
    $headers = @{"Authorization" = "Basic $auth"}
    $datasources = Invoke-RestMethod -Uri "http://localhost:3000/api/datasources" -Headers $headers -TimeoutSec 3
    $promDs = $datasources | Where-Object { $_.type -eq "prometheus" }
    
    if ($promDs) {
        Write-Host "   ✅ Data source OK (ID: $($promDs.id))" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Data source mancante!" -ForegroundColor Red
    }
} catch {
    Write-Host "   ❌ Grafana non raggiungibile" -ForegroundColor Red
}

# 6. Check dashboard
Write-Host "`n[6/7] Check dashboard..." -ForegroundColor Yellow
try {
    $auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:admin"))
    $headers = @{"Authorization" = "Basic $auth"}
    $dashboards = Invoke-RestMethod -Uri "http://localhost:3000/api/search?type=dash-db" -Headers $headers -TimeoutSec 3
    $dash = $dashboards | Where-Object { $_.uid -eq "aibetting-analyst" }
    
    if ($dash) {
        Write-Host "   ✅ Dashboard trovata" -ForegroundColor Green
        Write-Host "   URL: http://localhost:3000/d/$($dash.uid)" -ForegroundColor Cyan
    } else {
        Write-Host "   ❌ Dashboard non trovata!" -ForegroundColor Red
        Write-Host "   Fix: Import grafana-dashboard-analyst.json via UI" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ❌ Errore check dashboard" -ForegroundColor Red
}

# 7. Test end-to-end
Write-Host "`n[7/7] Test query via Grafana..." -ForegroundColor Yellow
try {
    $auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:admin"))
    $headers = @{"Authorization" = "Basic $auth"}
    $datasources = Invoke-RestMethod -Uri "http://localhost:3000/api/datasources" -Headers $headers
    $promDs = $datasources | Where-Object { $_.type -eq "prometheus" }
    
    if ($promDs) {
        $testQuery = [System.Web.HttpUtility]::UrlEncode("aibetting_analyst_snapshots_processed_total")
        $proxyUrl = "http://localhost:3000/api/datasources/proxy/$($promDs.id)/api/v1/query?query=$testQuery"
        $testResult = Invoke-RestMethod -Uri $proxyUrl -Headers $headers -TimeoutSec 3
        
        if ($testResult.data.result.Count -gt 0) {
            Write-Host "   ✅ Grafana → Prometheus OK" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  Grafana query no data" -ForegroundColor Yellow
        }
    }
} catch {
    Write-Host "   ❌ Test fallito: $_" -ForegroundColor Red
}

Write-Host "`n=== DIAGNOSI COMPLETATA ===" -ForegroundColor Cyan
Write-Host ""
```

**Uso:**
```powershell
.\fix-grafana-analyst.ps1
```

---

## 📋 **Checklist Manuale Completa**

- [ ] Analyst processo attivo
- [ ] Port 5002 risponde
- [ ] Metriche disponibili su `/metrics`
- [ ] Metriche incrementano (non sempre 0)
- [ ] Prometheus target "aibetting-analyst" configurato
- [ ] Prometheus target status "UP"
- [ ] Prometheus ha dati (query manuale funziona)
- [ ] Grafana data source "Prometheus" configurato
- [ ] Grafana data source test OK
- [ ] Dashboard "aibetting-analyst" esiste
- [ ] Dashboard panels hanno query corrette
- [ ] Time range dashboard corretto (Last 15 minutes)
- [ ] Query via Grafana proxy funziona

---

## 🎯 **Quick Fix Comandi**

```powershell
# Restart completo stack
docker restart aibetting-prometheus aibetting-grafana
Get-Process | Where { $_.ProcessName -like "*AIBetting*" } | Stop-Process -Force
Start-Sleep -Seconds 5

# Riavvia Explorer
cd AIBettingExplorer; Start-Process powershell -ArgumentList "-NoExit -Command dotnet run" -WindowStyle Minimized

# Riavvia Analyst
cd AIBettingAnalyst; Start-Process powershell -ArgumentList "-NoExit -Command dotnet run"

# Attendi 30 secondi
Start-Sleep -Seconds 30

# Apri dashboard
start http://localhost:3000/d/aibetting-analyst
```

---

## 📞 **Link Utili**

| Servizio | URL |
|----------|-----|
| Dashboard Analyst | http://localhost:3000/d/aibetting-analyst |
| Grafana Dashboards | http://localhost:3000/dashboards |
| Grafana Data Sources | http://localhost:3000/datasources |
| Prometheus Targets | http://localhost:9090/targets |
| Prometheus Query | http://localhost:9090/graph |
| Analyst Metrics | http://localhost:5002/metrics |
| Explorer Metrics | http://localhost:5001/metrics |

---

**Creato:** 2026-01-12  
**Ultima Modifica:** 2026-01-12  
**Status:** Guida Completa
