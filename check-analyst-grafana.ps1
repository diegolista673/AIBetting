# check-analyst-grafana.ps1
# Verifica completa stato Analyst + Grafana Dashboard

Write-Host "`n=== VERIFICA ANALYST GRAFANA ===" -ForegroundColor Cyan
Write-Host ""

# 1. Check processo
Write-Host "[1] Processo Analyst:" -ForegroundColor Yellow
$proc = Get-Process -Name "AIBettingAnalyst" -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "   ✅ RUNNING (PID: $($proc.Id), Memory: $([math]::Round($proc.WorkingSet64/1MB,2)) MB)" -ForegroundColor Green
} else {
    Write-Host "   ❌ NOT RUNNING" -ForegroundColor Red
    Write-Host "   Fix: cd AIBettingAnalyst; dotnet run" -ForegroundColor Yellow
    exit
}

# 2. Check metrics endpoint
Write-Host "`n[2] Metrics Endpoint:" -ForegroundColor Yellow
try {
    $metrics = Invoke-WebRequest -Uri "http://localhost:5002/metrics" -UseBasicParsing -TimeoutSec 2
    Write-Host "   ✅ REACHABLE (http://localhost:5002/metrics)" -ForegroundColor Green
    
    if ($metrics.Content -match "aibetting_analyst_snapshots_processed_total\s+(\d+)") {
        Write-Host "   📊 Snapshots Processed: $($matches[1])" -ForegroundColor Cyan
    }
    
    if ($metrics.Content -match "aibetting_analyst_surebets_found_total\s+(\d+)") {
        Write-Host "   💰 Surebets Found: $($matches[1])" -ForegroundColor Cyan
    }
} catch {
    Write-Host "   ❌ UNREACHABLE" -ForegroundColor Red
    Write-Host "   Error: $_" -ForegroundColor Gray
}

# 3. Check Prometheus target
Write-Host "`n[3] Prometheus Target:" -ForegroundColor Yellow
try {
    $targets = Invoke-RestMethod -Uri "http://localhost:9090/api/v1/targets" -TimeoutSec 3
    $analyst = $targets.data.activeTargets | Where-Object { $_.labels.job -eq 'aibetting-analyst' }
    
    if ($analyst) {
        $color = if ($analyst.health -eq 'up') { 'Green' } else { 'Red' }
        $symbol = if ($analyst.health -eq 'up') { '✅' } else { '❌' }
        Write-Host "   $symbol Status: $($analyst.health.ToUpper())" -ForegroundColor $color
        Write-Host "   Endpoint: $($analyst.scrapeUrl)" -ForegroundColor Gray
        
        if ($analyst.lastError) {
            Write-Host "   Last Error: $($analyst.lastError)" -ForegroundColor Red
        }
    } else {
        Write-Host "   ❌ TARGET NOT FOUND" -ForegroundColor Red
        Write-Host "   Fix: docker restart aibetting-prometheus" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ❌ PROMETHEUS UNREACHABLE" -ForegroundColor Red
    Write-Host "   Fix: docker-compose --profile monitoring up -d" -ForegroundColor Yellow
}

# 4. Check Prometheus data
Write-Host "`n[4] Prometheus Data:" -ForegroundColor Yellow
try {
    $query = [System.Web.HttpUtility]::UrlEncode("aibetting_analyst_snapshots_processed_total")
    $result = Invoke-RestMethod -Uri "http://localhost:9090/api/v1/query?query=$query" -TimeoutSec 3
    
    if ($result.data.result.Count -gt 0) {
        $value = $result.data.result[0].value[1]
        Write-Host "   ✅ DATA AVAILABLE" -ForegroundColor Green
        Write-Host "   Value: $value snapshots" -ForegroundColor Cyan
    } else {
        Write-Host "   ⚠️  NO DATA (attendi primo scrape - 5s interval)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ❌ QUERY FAILED" -ForegroundColor Red
    Write-Host "   Error: $_" -ForegroundColor Gray
}

# 5. Check Grafana dashboard
Write-Host "`n[5] Grafana Dashboard:" -ForegroundColor Yellow
try {
    $auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:admin"))
    $headers = @{"Authorization" = "Basic $auth"}
    $dashboards = Invoke-RestMethod -Uri "http://localhost:3000/api/search?type=dash-db" -Headers $headers -TimeoutSec 3
    $dash = $dashboards | Where-Object { $_.uid -eq "aibetting-analyst" }
    
    if ($dash) {
        Write-Host "   ✅ DASHBOARD FOUND" -ForegroundColor Green
        Write-Host "   Title: $($dash.title)" -ForegroundColor White
        Write-Host "   URL: http://localhost:3000/d/$($dash.uid)" -ForegroundColor Cyan
        
        # Test query via Grafana
        Write-Host "`n   Testing dashboard queries..." -ForegroundColor Gray
        $dsResult = Invoke-RestMethod -Uri "http://localhost:3000/api/datasources" -Headers $headers
        $promDs = $dsResult | Where-Object { $_.type -eq "prometheus" }
        
        if ($promDs) {
            Write-Host "   ✅ Prometheus datasource configured (ID: $($promDs.id))" -ForegroundColor Green
        }
    } else {
        Write-Host "   ❌ DASHBOARD NOT FOUND" -ForegroundColor Red
        Write-Host "   Fix: Import grafana-dashboard-analyst.json via Grafana UI" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ❌ GRAFANA UNREACHABLE" -ForegroundColor Red
    Write-Host "   Fix: docker restart aibetting-grafana" -ForegroundColor Yellow
}

# 6. Summary
Write-Host "`n=== RIEPILOGO ===" -ForegroundColor Cyan

$allGood = $true

if (-not $proc) { $allGood = $false }

try {
    $metricsTest = Invoke-WebRequest -Uri "http://localhost:5002/metrics" -UseBasicParsing -TimeoutSec 1 -ErrorAction Stop
} catch {
    $allGood = $false
}

try {
    $promTest = Invoke-RestMethod -Uri "http://localhost:9090/api/v1/targets" -TimeoutSec 1 -ErrorAction Stop
    $analystTarget = $promTest.data.activeTargets | Where-Object { $_.labels.job -eq 'aibetting-analyst' }
    if (-not $analystTarget -or $analystTarget.health -ne 'up') { $allGood = $false }
} catch {
    $allGood = $false
}

if ($allGood) {
    Write-Host "`n✅ TUTTO FUNZIONANTE!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Dashboard disponibile su:" -ForegroundColor Cyan
    Write-Host "  http://localhost:3000/d/aibetting-analyst" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host "`n⚠️  ALCUNI PROBLEMI RILEVATI" -ForegroundColor Yellow
    Write-Host "Consulta l'output sopra per i dettagli" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "=== VERIFICA COMPLETATA ===" -ForegroundColor Cyan
Write-Host ""
