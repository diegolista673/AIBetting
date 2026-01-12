# 🔧 AIBettingAnalyst - Troubleshooting Guide

## ❌ **Errore Risolto: Porta 5002 già in uso**

### **Problema**
```
System.IO.IOException: Failed to bind to address http://[::]:5002: address already in use.
System.Net.Sockets.SocketException (10048): Di norma è consentito un solo utilizzo 
di ogni indirizzo di socket (protocollo/indirizzo di rete/porta).
```

### **Causa**
Un'altra istanza di AIBettingAnalyst è già in esecuzione sulla porta 5002.

### **Soluzione Applicata** ✅
```powershell
# 1. Trova processo esistente
Get-Process | Where-Object { $_.ProcessName -like "*AIBettingAnalyst*" }

# Output:
# ProcessName          Id StartTime
# AIBettingAnalyst  19320 12/01/2026 12:03:50

# 2. Ferma il processo
Stop-Process -Id 19320 -Force

# 3. Riavvia Analyst
cd AIBettingAnalyst
dotnet run
```

---

## 🚀 **Comandi Utili**

### **Verificare Processi AIBetting Attivi**
```powershell
Get-Process | Where-Object { $_.ProcessName -like "*AIBetting*" } | 
    Select-Object ProcessName, Id, StartTime, WorkingSet64 | 
    Format-Table -AutoSize
```

**Output atteso:**
```
ProcessName          Id StartTime           WorkingSet64
AIBettingExplorer  8224 12/01/2026 12:01:08    85000000
AIBettingAnalyst  19320 12/01/2026 12:03:50    75000000
```

### **Verificare Porte in Uso**
```powershell
# Verifica porta 5001 (Explorer)
Test-NetConnection localhost -Port 5001 -InformationLevel Detailed

# Verifica porta 5002 (Analyst)
Test-NetConnection localhost -Port 5002 -InformationLevel Detailed

# Lista tutte le porte AIBetting
netstat -ano | findstr "500[1-3]"
```

### **Kill Tutti i Processi AIBetting**
```powershell
Get-Process | Where-Object { $_.ProcessName -like "*AIBetting*" } | 
    Stop-Process -Force

Write-Host "All AIBetting processes stopped" -ForegroundColor Green
```

### **Restart Completo Stack**
```powershell
# Script completo restart
# 1. Stop all processes
Get-Process | Where-Object { $_.ProcessName -like "*AIBetting*" } | Stop-Process -Force
Start-Sleep -Seconds 2

# 2. Start Explorer
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd C:\Users\SMARTW\source\repos\AIBettingSolution\AIBettingExplorer; dotnet run" -WindowStyle Minimized

# 3. Wait for Explorer startup
Start-Sleep -Seconds 10

# 4. Start Analyst
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd C:\Users\SMARTW\source\repos\AIBettingSolution\AIBettingAnalyst; dotnet run" -WindowStyle Normal

Write-Host "Stack restarted successfully" -ForegroundColor Green
```

---

## 🔍 **Diagnostica Avanzata**

### **Verificare Connessione Redis**
```powershell
# Test connessione Redis
docker exec aibetting-redis redis-cli ping
# Output atteso: PONG

# Verifica chiavi Explorer
docker exec aibetting-redis redis-cli --scan --pattern "prices:*" | 
    Select-Object -First 5
```

### **Verificare Metriche**
```powershell
# Explorer metrics
curl http://localhost:5001/metrics | Select-String "aibetting_price_updates_total"

# Analyst metrics
curl http://localhost:5002/metrics | Select-String "aibetting_analyst"

# Expected output:
# aibetting_analyst_snapshots_processed_total 125
# aibetting_analyst_signals_generated_total{strategy="surebet"} 3
# aibetting_analyst_surebets_found_total 3
```

### **Verificare Prometheus Targets**
```powershell
# Check Prometheus targets status
$response = Invoke-RestMethod -Uri "http://localhost:9090/api/v1/targets"
$response.data.activeTargets | 
    Where-Object { $_.labels.service -like "aibetting*" } | 
    Select-Object @{N='Service';E={$_.labels.service}}, 
                  @{N='Status';E={$_.health}}, 
                  @{N='LastScrape';E={$_.lastScrape}} | 
    Format-Table -AutoSize
```

---

## 🐛 **Errori Comuni**

### **1. Redis Connection Timeout**
```
Error: It was not possible to connect to the redis server(s). 
ConnectTimeout: 5000ms
```

**Soluzione:**
```powershell
# Check Redis container
docker ps | findstr redis

# Restart Redis
docker restart aibetting-redis

# Test connection
docker exec aibetting-redis redis-cli ping
```

### **2. Configuration Not Found**
```
InvalidOperationException: Redis:ConnectionString not found
```

**Soluzione:**
```powershell
# Verify appsettings.json exists
Test-Path "AIBettingAnalyst\appsettings.json"

# Check content
Get-Content "AIBettingAnalyst\appsettings.json" | ConvertFrom-Json | 
    Select-Object -ExpandProperty Redis
```

### **3. Metrics Not Updating**
```
aibetting_analyst_snapshots_processed_total stays at 0
```

**Causa:** Explorer non sta pubblicando su Redis o Analyst non è subscribed.

**Soluzione:**
```powershell
# 1. Verify Explorer is publishing
docker exec aibetting-redis redis-cli MONITOR | findstr "channel:price-updates"
# Should see PUBLISH commands every 2 seconds

# 2. Check Analyst logs
Get-Content "AIBettingAnalyst\logs\analyst-*.log" -Tail 50 | 
    Select-String "Subscribing\|Snapshot\|Signal"
```

### **4. High Latency**
```
aibetting_analyst_processing_latency_seconds > 0.1 (100ms)
```

**Causa:** System overload, Redis slow, or inefficient code.

**Soluzione:**
```powershell
# Check CPU usage
Get-Process -Name "AIBettingAnalyst" | 
    Select-Object Name, CPU, WorkingSet64, Threads

# Check Redis latency
docker exec aibetting-redis redis-cli --latency

# Profile code (se persistente)
# Use dotnet-trace or Visual Studio Profiler
```

---

## 📊 **Monitoring Dashboard**

### **Create Quick Status Dashboard**
```powershell
# Save as: check-analyst-status.ps1
function Get-AnalystStatus {
    $status = @{
        ProcessRunning = $false
        RedisConnected = $false
        MetricsAvailable = $false
        PrometheusScrap = $false
    }
    
    # Check process
    $process = Get-Process -Name "AIBettingAnalyst" -ErrorAction SilentlyContinue
    $status.ProcessRunning = ($null -ne $process)
    
    # Check Redis
    try {
        $redis = docker exec aibetting-redis redis-cli ping 2>$null
        $status.RedisConnected = ($redis -eq "PONG")
    } catch {}
    
    # Check metrics
    try {
        $metrics = Invoke-WebRequest -Uri "http://localhost:5002/metrics" -UseBasicParsing -TimeoutSec 2
        $status.MetricsAvailable = ($metrics.StatusCode -eq 200)
    } catch {}
    
    # Check Prometheus
    try {
        $targets = Invoke-RestMethod -Uri "http://localhost:9090/api/v1/targets" -TimeoutSec 2
        $analyst = $targets.data.activeTargets | Where-Object { $_.labels.service -eq "analyst" }
        $status.PrometheusScrap = ($analyst.health -eq "up")
    } catch {}
    
    return $status
}

# Display status
$status = Get-AnalystStatus
Write-Host "`nAnalyst Status:" -ForegroundColor Cyan
Write-Host "================" -ForegroundColor Cyan
$status.GetEnumerator() | ForEach-Object {
    $color = if ($_.Value) { "Green" } else { "Red" }
    $symbol = if ($_.Value) { "✅" } else { "❌" }
    Write-Host "$symbol $($_.Key): $($_.Value)" -ForegroundColor $color
}
```

---

## 🎯 **Performance Tuning**

### **Optimize for Low Latency**
```json
// appsettings.json
{
  "Analyst": {
    "MinSurebetProfitPercent": 0.3,  // Lower threshold
    "WAPLevels": 5,                   // More depth
    "PrometheusMetricsPort": 5002
  }
}
```

### **Optimize for High Accuracy**
```json
{
  "Analyst": {
    "MinSurebetProfitPercent": 1.0,  // Higher threshold
    "WAPLevels": 3,                   // Standard depth
    "PrometheusMetricsPort": 5002
  }
}
```

### **Redis Connection Tuning**
```json
{
  "Redis": {
    "ConnectionString": "localhost:6379,password=***,abortConnect=false,connectRetry=5,connectTimeout=10000,syncTimeout=10000,asyncTimeout=10000"
  }
}
```

---

## 🔧 **Development Tips**

### **Run with Debug Logging**
```powershell
# Edit Program.cs temporarily
# .MinimumLevel.Information() → .MinimumLevel.Debug()

dotnet run
```

### **Test Specific Scenarios**
```powershell
# Create test snapshot in Redis
docker exec -it aibetting-redis redis-cli

# In Redis CLI:
SET "prices:1.200000000:2026-01-12T12:00:00" '{"marketId":"1.200000000","runners":[...]}'
PUBLISH "channel:price-updates" '{"marketId":"1.200000000","timestamp":"2026-01-12T12:00:00"}'
```

### **Benchmark Performance**
```powershell
# Use BenchmarkDotNet (add to test project)
dotnet add package BenchmarkDotNet

# Create benchmark class
[MemoryDiagnoser]
public class AnalystBenchmarks
{
    [Benchmark]
    public void SurebetDetection() { ... }
}
```

---

## 📞 **Quick Reference**

| Command | Purpose |
|---------|---------|
| `dotnet build` | Compile code |
| `dotnet run` | Start Analyst |
| `curl http://localhost:5002/metrics` | Check metrics |
| `docker logs aibetting-redis` | Redis logs |
| `Get-Process \| Where-Object { $_.ProcessName -like "*AIBetting*" }` | List processes |
| `Stop-Process -Name "AIBettingAnalyst" -Force` | Kill Analyst |

---

**Creato:** 2026-01-12  
**Status:** ✅ Issue resolved (porta 5002 liberata)  
**Next:** Restart Analyst con `dotnet run`
