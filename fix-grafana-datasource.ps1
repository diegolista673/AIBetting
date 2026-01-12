# Fix Grafana Dashboard - Configura Data Source e Refresh
# Questo script risolve il problema della dashboard vuota

Write-Host ""
Write-Host "========================================"  -ForegroundColor Cyan
Write-Host "  Fix Grafana Dashboard Vuota"  -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Verifica Prometheus data source
Write-Host "[1/3] Verifico data source Prometheus..." -ForegroundColor Yellow
$auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:admin"))
$headers = @{
    "Authorization" = "Basic $auth"
    "Content-Type" = "application/json"
}

$dataSources = Invoke-RestMethod -Uri "http://localhost:3000/api/datasources" -Headers $headers
$prometheusDs = $dataSources | Where-Object { $_.type -eq "prometheus" }

if ($prometheusDs) {
    Write-Host "  [OK] Data source Prometheus esiste (ID: $($prometheusDs.id))" -ForegroundColor Green
} else {
    Write-Host "  [FIX] Creando data source Prometheus..." -ForegroundColor Yellow
    
    $dsConfig = @{
        name = "Prometheus"
        type = "prometheus"
        url = "http://prometheus:9090"
        access = "proxy"
        isDefault = $true
        jsonData = @{
            httpMethod = "POST"
            timeInterval = "5s"
        }
    } | ConvertTo-Json
    
    try {
        $result = Invoke-RestMethod -Uri "http://localhost:3000/api/datasources" -Method Post -Headers $headers -Body $dsConfig
        Write-Host "  [OK] Data source creato (ID: $($result.id))" -ForegroundColor Green
        $prometheusDs = $result
    } catch {
        Write-Host "  [ERROR] Impossibile creare data source: $_" -ForegroundColor Red
        exit 1
    }
}

# Step 2: Test connessione
Write-Host "`n[2/3] Test connessione data source..." -ForegroundColor Yellow
try {
    $testQuery = "aibetting_price_updates_total"
    $testUrl = "http://localhost:3000/api/datasources/proxy/$($prometheusDs.id)/api/v1/query?query=$testQuery"
    $testResult = Invoke-RestMethod -Uri $testUrl -Headers $headers -TimeoutSec 5
    
    if ($testResult.data.result.Count -gt 0) {
        $value = $testResult.data.result[0].value[1]
        Write-Host "  [OK] Data source funzionante - aibetting_price_updates_total: $value" -ForegroundColor Green
    } else {
        Write-Host "  [WARNING] Data source OK ma no data (normale se appena avviato)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  [ERROR] Test connessione fallito: $_" -ForegroundColor Red
}

# Step 3: Aggiorna dashboard per usare il data source
Write-Host "`n[3/3] Aggiornamento dashboard..." -ForegroundColor Yellow

try {
    # Get dashboard
    $dashboard = Invoke-RestMethod -Uri "http://localhost:3000/api/dashboards/uid/aibetting-explorer" -Headers $headers
    
    # Update all panels data source
    $updated = $false
    foreach ($panel in $dashboard.dashboard.panels) {
        if ($panel.datasource -and $panel.datasource.type -eq "prometheus") {
            $panel.datasource.uid = $prometheusDs.uid
            $updated = $true
        }
    }
    
    if ($updated) {
        # Save dashboard
        $savePayload = @{
            dashboard = $dashboard.dashboard
            overwrite = $true
        } | ConvertTo-Json -Depth 20
        
        $saveResult = Invoke-RestMethod -Uri "http://localhost:3000/api/dashboards/db" -Method Post -Headers $headers -Body $savePayload
        Write-Host "  [OK] Dashboard aggiornata con data source corretto" -ForegroundColor Green
    } else {
        Write-Host "  [INFO] Dashboard già configurata correttamente" -ForegroundColor Cyan
    }
} catch {
    Write-Host "  [WARNING] Impossibile aggiornare dashboard automaticamente" -ForegroundColor Yellow
    Write-Host "           Dovrai configurare manualmente i panels" -ForegroundColor Gray
}

# Summary
Write-Host ""
Write-Host "========================================"  -ForegroundColor Cyan
Write-Host "  Fix Completato!"  -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Prossimi passi:" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Apri Grafana dashboard:" -ForegroundColor White
Write-Host "   http://localhost:3000/d/aibetting-explorer" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Refresh page (Ctrl+F5)" -ForegroundColor White
Write-Host ""
Write-Host "3. Dovresti vedere i grafici con dati!" -ForegroundColor White
Write-Host ""
Write-Host "Se panels ancora vuoti:" -ForegroundColor Yellow
Write-Host "  a. Click su panel title -> Edit" -ForegroundColor White
Write-Host "  b. Data Source dropdown -> Seleziona 'Prometheus'" -ForegroundColor White
Write-Host "  c. Click 'Apply' poi 'Save dashboard'" -ForegroundColor White
Write-Host ""
Write-Host "Blazor Dashboard:" -ForegroundColor Cyan
Write-Host "  http://localhost:5000/monitoring" -ForegroundColor Gray
Write-Host ""

$openNow = Read-Host "Aprire Grafana dashboard ora? (y/n)"
if ($openNow -eq "y") {
    Start-Process "http://localhost:3000/d/aibetting-explorer"
}

Write-Host ""
