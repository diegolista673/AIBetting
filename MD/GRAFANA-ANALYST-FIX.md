# 🔧 Grafana Analyst Dashboard - Fix Completo

## ✅ **PROBLEMA RISOLTO**

### **Causa Root**
Prometheus **non aveva caricato** la configurazione del target `aibetting-analyst` perché il container non era stato riavviato dopo l'aggiunta della configurazione.

---

## 🎯 **Soluzione Applicata**

### **Step 1: Riavvio Prometheus**
```powershell
docker restart aibetting-prometheus
```

### **Step 2: Verifica Target**
```powershell
# Verifica target attivi
$targets = Invoke-RestMethod -Uri "http://localhost:9090/api/v1/targets"
$targets.data.activeTargets | Where-Object { $_.labels.job -eq 'aibetting-analyst' }
```

**Risultato:**
```
✅ Target 'aibetting-analyst' ATTIVO
   Health: up
   Endpoint: http://192.168.208.1:5002/metrics
```

### **Step 3: Verifica Metriche in Prometheus**
```powershell
$query = "aibetting_analyst_snapshots_processed_total"
$result = Invoke-RestMethod -Uri "http://localhost:9090/api/v1/query?query=$query"
```

**Risultato:**
```
✅ Dati disponibili: 335 snapshots processati
```

---

## 📊 **Dashboard Grafana Funzionante**

### **Accesso Dashboard**
```
URL: http://localhost:3000/d/aibetting-analyst
Title: AIBetting Analyst - Real-time Performance
UID: aibetting-analyst
```

### **Metriche Disponibili**

| Metrica | Descrizione | Valore Attuale |
|---------|-------------|----------------|
| `aibetting_analyst_snapshots_processed_total` | Snapshots analizzati | 335+ |
| `aibetting_analyst_surebets_found_total` | Surebets rilevati | 0 |
| `aibetting_analyst_signals_generated_total` | Segnali generati | 0 |
| `aibetting_analyst_processing_latency_seconds` | Latenza processing | < 50ms |
| `aibetting_analyst_average_expected_roi` | ROI medio atteso | N/A |

---

## 🔍 **Troubleshooting Futuro**

### **Se Dashboard Mostra "No Data"**

#### **1. Verifica Prometheus Target**
```powershell
# Apri Prometheus UI
start http://localhost:9090/targets

# Cerca 'aibetting-analyst'
# Stato deve essere: UP (verde)
```

#### **2. Verifica Metriche Endpoint**
```powershell
curl http://localhost:5002/metrics | Select-String "aibetting_analyst"
```

**Output atteso:**
```
aibetting_analyst_snapshots_processed_total 335
aibetting_analyst_surebets_found_total 0
aibetting_analyst_processing_latency_seconds_sum ...
aibetting_analyst_processing_latency_seconds_count ...
```

#### **3. Riavvia Prometheus**
```powershell
docker restart aibetting-prometheus
Start-Sleep -Seconds 10
```

#### **4. Verifica Data Source Grafana**
```
1. Apri: http://localhost:3000/datasources
2. Seleziona: Prometheus
3. Click: "Test" button
4. Deve mostrare: "Data source is working"
```

#### **5. Forza Refresh Dashboard**
```
1. Apri dashboard: http://localhost:3000/d/aibetting-analyst
2. Click icona refresh (in alto a destra)
3. Seleziona range time: "Last 15 minutes"
4. Click "Refresh"
```

---

## 🚀 **Verifica Completa Sistema**

### **Script PowerShell Automatico**

```powershell
# check-analyst-grafana.ps1

Write-Host "`n=== VERIFICA ANALYST GRAFANA ===" -ForegroundColor Cyan

# 1. Check processo
Write-Host "`n[1] Processo Analyst:" -ForegroundColor Yellow
$proc = Get-Process -Name "AIBettingAnalyst" -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "   ✅ RUNNING (PID: $($proc.Id))" -ForegroundColor Green
} else {
    Write-Host "   ❌ NOT RUNNING" -ForegroundColor Red
    exit
}

# 2. Check metrics endpoint
Write-Host "`n[2] Metrics Endpoint:" -ForegroundColor Yellow
try {
    $metrics = Invoke-WebRequest -Uri "http://localhost:5002/metrics" -UseBasicParsing -TimeoutSec 2
    Write-Host "   ✅ REACHABLE" -ForegroundColor Green
    
    if ($metrics.Content -match "aibetting_analyst_snapshots_processed_total\s+(\d+)") {
        Write-Host "   Snapshots: $($matches[1])" -ForegroundColor Cyan
    }
} catch {
    Write-Host "   ❌ UNREACHABLE" -ForegroundColor Red
}

# 3. Check Prometheus target
Write-Host "`n[3] Prometheus Target:" -ForegroundColor Yellow
try {
    $targets = Invoke-RestMethod -Uri "http://localhost:9090/api/v1/targets"
    $analyst = $targets.data.activeTargets | Where-Object { $_.labels.job -eq 'aibetting-analyst' }
    
    if ($analyst) {
        $color = if ($analyst.health -eq 'up') { 'Green' } else { 'Red' }
        Write-Host "   Status: $($analyst.health.ToUpper())" -ForegroundColor $color
    } else {
        Write-Host "   ❌ TARGET NOT FOUND" -ForegroundColor Red
        Write-Host "   Fix: docker restart aibetting-prometheus" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ❌ PROMETHEUS UNREACHABLE" -ForegroundColor Red
}

# 4. Check Prometheus data
Write-Host "`n[4] Prometheus Data:" -ForegroundColor Yellow
try {
    $query = [System.Web.HttpUtility]::UrlEncode("aibetting_analyst_snapshots_processed_total")
    $result = Invoke-RestMethod -Uri "http://localhost:9090/api/v1/query?query=$query"
    
    if ($result.data.result.Count -gt 0) {
        $value = $result.data.result[0].value[1]
        Write-Host "   ✅ DATA AVAILABLE (Value: $value)" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  NO DATA (attendi primo scrape)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ❌ QUERY FAILED" -ForegroundColor Red
}

# 5. Check Grafana dashboard
Write-Host "`n[5] Grafana Dashboard:" -ForegroundColor Yellow
try {
    $auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:admin"))
    $headers = @{"Authorization" = "Basic $auth"}
    $dashboards = Invoke-RestMethod -Uri "http://localhost:3000/api/search?type=dash-db" -Headers $headers
    $dash = $dashboards | Where-Object { $_.uid -eq "aibetting-analyst" }
    
    if ($dash) {
        Write-Host "   ✅ DASHBOARD FOUND" -ForegroundColor Green
        Write-Host "   URL: http://localhost:3000/d/$($dash.uid)" -ForegroundColor Cyan
    } else {
        Write-Host "   ❌ DASHBOARD NOT FOUND" -ForegroundColor Red
        Write-Host "   Fix: Import grafana-dashboard-analyst.json" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ❌ GRAFANA UNREACHABLE" -ForegroundColor Red
}

Write-Host "`n=== VERIFICA COMPLETATA ===" -ForegroundColor Cyan
Write-Host ""
```

**Uso:**
```powershell
.\check-analyst-grafana.ps1
```

---

## 📋 **Panels Dashboard**

### **Panel 1: Total Snapshots Processed**
```promql
aibetting_analyst_snapshots_processed_total
```

### **Panel 2: Surebets Found (Total)**
```promql
aibetting_analyst_surebets_found_total
```

### **Panel 3: Signals Generated (Total)**
```promql
sum(aibetting_analyst_signals_generated_total)
```

### **Panel 4: Average Expected ROI**
```promql
aibetting_analyst_average_expected_roi
```

### **Panel 5: Signals Rate (per minute)**
```promql
rate(aibetting_analyst_signals_generated_total[1m]) * 60
```

### **Panel 6: Surebets Detection Rate**
```promql
rate(aibetting_analyst_surebets_found_total[1m]) * 60
```

### **Panel 7: Processing Latency (p50/p95/p99)**
```promql
# p50
histogram_quantile(0.50, rate(aibetting_analyst_processing_latency_seconds_bucket[1m]))

# p95
histogram_quantile(0.95, rate(aibetting_analyst_processing_latency_seconds_bucket[1m]))

# p99
histogram_quantile(0.99, rate(aibetting_analyst_processing_latency_seconds_bucket[1m]))
```

---

## ⚠️ **Known Issues**

### **Issue 1: "No data" dopo restart container**
**Causa:** Prometheus perde metriche temporanee (< retention)  
**Fix:** Attendi 15 secondi per nuovo scrape

### **Issue 2: Dashboard panels vuoti**
**Causa:** Time range troppo vecchio  
**Fix:** 
1. Click time picker (top right)
2. Select "Last 15 minutes"
3. Click "Apply"

### **Issue 3: Query timeout**
**Causa:** Prometheus overload o disconnesso  
**Fix:**
```powershell
docker restart aibetting-prometheus
```

---

## 🎯 **Quick Commands**

### **Restart Stack Monitoring**
```powershell
docker restart aibetting-prometheus
docker restart aibetting-grafana
Start-Sleep -Seconds 15
```

### **Check All Targets**
```powershell
start http://localhost:9090/targets
```

### **Open Analyst Dashboard**
```powershell
start http://localhost:3000/d/aibetting-analyst
```

### **Check Metrics Raw**
```powershell
start http://localhost:5002/metrics
```

---

## ✅ **Stato Finale**

```
✅ Analyst Process: RUNNING
✅ Metrics Endpoint: http://localhost:5002/metrics
✅ Prometheus Target: UP
✅ Prometheus Data: 335+ snapshots
✅ Grafana Dashboard: http://localhost:3000/d/aibetting-analyst
✅ Dashboard Panels: 7 panels configurati
```

---

**Creato:** 2026-01-12  
**Status:** ✅ RISOLTO  
**Fix:** Restart Prometheus per caricare configurazione target
