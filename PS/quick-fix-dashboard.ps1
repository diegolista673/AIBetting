# quick-fix-dashboard.ps1
# Quick fix per dashboard Grafana che mostra "No data"

Write-Host "`n=== QUICK FIX GRAFANA DASHBOARD ===" -ForegroundColor Cyan

$hasData = $false

# 1. Verifica dati esistono in Prometheus
Write-Host "`n[1] Verifica dati Prometheus..." -ForegroundColor Yellow
try {
    $q = [System.Web.HttpUtility]::UrlEncode("aibetting_analyst_snapshots_processed_total")
    $r = Invoke-RestMethod "http://localhost:9090/api/v1/query?query=$q" -TimeoutSec 3
    if ($r.data.result.Count -gt 0) {
        $value = $r.data.result[0].value[1]
        Write-Host "   OK - Dati disponibili: $value snapshots" -ForegroundColor Green
        $hasData = $true
    } else {
        Write-Host "   ERROR - Nessun dato in Prometheus!" -ForegroundColor Red
    }
} catch {
    Write-Host "   ERROR - Prometheus non risponde: $_" -ForegroundColor Red
}

if (-not $hasData) {
    Write-Host "`nImpossibile continuare senza dati in Prometheus." -ForegroundColor Red
    exit
}

# 2. Test data source Grafana
Write-Host "`n[2] Test Grafana data source..." -ForegroundColor Yellow
try {
    $auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:admin"))
    $headers = @{"Authorization" = "Basic $auth"}
    $ds = Invoke-RestMethod "http://localhost:3000/api/datasources" -Headers $headers -TimeoutSec 3
    $promDs = $ds | Where-Object { $_.type -eq "prometheus" } | Select-Object -First 1
    
    if ($promDs) {
        Write-Host "   OK - Data source trovato (ID: $($promDs.id))" -ForegroundColor Green
        Write-Host "   URL: $($promDs.url)" -ForegroundColor Gray
        
        # Test query via Grafana
        $testQuery = [System.Web.HttpUtility]::UrlEncode("aibetting_analyst_snapshots_processed_total")
        $now = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
        $proxyUrl = "http://localhost:3000/api/datasources/proxy/$($promDs.id)/api/v1/query?query=$testQuery&time=$now"
        $testResult = Invoke-RestMethod $proxyUrl -Headers $headers -TimeoutSec 3
        
        if ($testResult.data.result.Count -gt 0) {
            Write-Host "   OK - Query via Grafana: $($testResult.data.result[0].value[1])" -ForegroundColor Green
        } else {
            Write-Host "   WARNING - Query via Grafana senza dati!" -ForegroundColor Yellow
        }
    } else {
        Write-Host "   ERROR - Data source Prometheus non trovato!" -ForegroundColor Red
    }
} catch {
    Write-Host "   ERROR - Grafana non risponde: $_" -ForegroundColor Red
}

# 3. Check dashboard
Write-Host "`n[3] Verifica dashboard..." -ForegroundColor Yellow
try {
    $auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:admin"))
    $headers = @{"Authorization" = "Basic $auth"}
    $dashboards = Invoke-RestMethod "http://localhost:3000/api/search?type=dash-db" -Headers $headers -TimeoutSec 3
    $dash = $dashboards | Where-Object { $_.uid -eq "aibetting-analyst" }
    
    if ($dash) {
        Write-Host "   OK - Dashboard trovata" -ForegroundColor Green
        Write-Host "   UID: $($dash.uid)" -ForegroundColor Gray
        Write-Host "   URL: http://localhost:3000/d/$($dash.uid)" -ForegroundColor Cyan
    } else {
        Write-Host "   ERROR - Dashboard 'aibetting-analyst' non trovata!" -ForegroundColor Red
    }
} catch {
    Write-Host "   ERROR - Check dashboard fallito" -ForegroundColor Red
}

# 4. Suggerimenti
Write-Host "`n=== DIAGNOSI COMPLETA ===" -ForegroundColor Cyan
Write-Host ""

if ($hasData) {
    Write-Host "DATI DISPONIBILI IN PROMETHEUS: SI" -ForegroundColor Green
    Write-Host ""
    Write-Host "Se la dashboard mostra 'No data', segui questi step:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  1. Apri dashboard:" -ForegroundColor White
    Write-Host "     http://localhost:3000/d/aibetting-analyst" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  2. CAMBIA TIME RANGE (importante!):" -ForegroundColor White
    Write-Host "     - Click icona orologio (in alto a destra)" -ForegroundColor Gray
    Write-Host "     - Seleziona: 'Last 15 minutes'" -ForegroundColor Gray
    Write-Host "     - Click 'Apply'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  3. FORCE REFRESH:" -ForegroundColor White
    Write-Host "     - Click icona refresh (freccia circolare)" -ForegroundColor Gray
    Write-Host "     - Oppure premi: Ctrl+R" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  4. ATTIVA AUTO-REFRESH:" -ForegroundColor White
    Write-Host "     - Click dropdown 'Off' accanto a refresh" -ForegroundColor Gray
    Write-Host "     - Seleziona: '5s' (refresh ogni 5 secondi)" -ForegroundColor Gray
    Write-Host ""
    
    $openNow = Read-Host "`nVuoi aprire la dashboard ora? (s/n)"
    if ($openNow -eq "s") {
        start http://localhost:3000/d/aibetting-analyst
        Write-Host "`nDashboard aperta. Ricorda di:" -ForegroundColor Cyan
        Write-Host "  - Cambiare time range a 'Last 15 minutes'" -ForegroundColor Yellow
        Write-Host "  - Attivare auto-refresh a '5s'" -ForegroundColor Yellow
    }
} else {
    Write-Host "PROBLEMA: Dati non disponibili in Prometheus" -ForegroundColor Red
    Write-Host ""
    Write-Host "Verifica che:" -ForegroundColor Yellow
    Write-Host "  1. Analyst sia attivo: Get-Process -Name AIBettingAnalyst" -ForegroundColor Gray
    Write-Host "  2. Explorer sia attivo: Get-Process -Name AIBettingExplorer" -ForegroundColor Gray
    Write-Host "  3. Prometheus target sia UP: http://localhost:9090/targets" -ForegroundColor Gray
}

Write-Host ""
Write-Host "=== FINE ===" -ForegroundColor Cyan
Write-Host ""
