param()

function Test-ExplorerMetrics {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5001/metrics" -UseBasicParsing -TimeoutSec 3
        $content = $response.Content
        
        if ($content -match "aibetting_price_updates_total\s+(\d+)") {
            Write-Host "[OK] Explorer metriche attive - Updates: $($matches[1])" -ForegroundColor Green
            return $true
        } else {
            Write-Host "[ERROR] Metriche aibetting_* NON TROVATE" -ForegroundColor Red
            Write-Host "  FIX: Riavvia Explorer con: cd AIBettingExplorer; dotnet run" -ForegroundColor Yellow
            return $false
        }
    } catch {
        Write-Host "[ERROR] Explorer non raggiungibile su :5001" -ForegroundColor Red
        Write-Host "  FIX: Avvia Explorer con: cd AIBettingExplorer; dotnet run" -ForegroundColor Yellow
        return $false
    }
}

function Test-PrometheusContainer {
    $container = docker ps --filter "name=prometheus" --format "{{.Names}}" 2>$null
    if ($container) {
        Write-Host "[OK] Prometheus container attivo: $container" -ForegroundColor Green
        return $true
    } else {
        Write-Host "[ERROR] Prometheus container non attivo" -ForegroundColor Red
        Write-Host "  FIX: docker-compose --profile monitoring up -d prometheus" -ForegroundColor Yellow
        return $false
    }
}

function Test-PrometheusTarget {
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:9090/api/v1/targets" -TimeoutSec 3
        $target = $response.data.activeTargets | Where-Object { $_.labels.job -eq 'aibetting-explorer' }
        
        if ($target) {
            if ($target.health -eq "up") {
                Write-Host "[OK] Prometheus target 'aibetting-explorer' UP" -ForegroundColor Green
                return $true
            } else {
                Write-Host "[ERROR] Prometheus target 'aibetting-explorer' DOWN" -ForegroundColor Red
                Write-Host "  Status: $($target.health)" -ForegroundColor Yellow
                Write-Host "  Error: $($target.lastError)" -ForegroundColor Yellow
                Write-Host "  FIX: Verifica prometheus.yml e IP host" -ForegroundColor Yellow
                return $false
            }
        } else {
            Write-Host "[ERROR] Target 'aibetting-explorer' non trovato" -ForegroundColor Red
            Write-Host "  FIX: Verifica prometheus.yml contiene job: aibetting-explorer" -ForegroundColor Yellow
            return $false
        }
    } catch {
        Write-Host "[ERROR] Prometheus non raggiungibile su :9090" -ForegroundColor Red
        return $false
    }
}

function Test-PrometheusData {
    try {
        $query = [System.Web.HttpUtility]::UrlEncode("aibetting_price_updates_total")
        $response = Invoke-RestMethod -Uri "http://localhost:9090/api/v1/query?query=$query" -TimeoutSec 3
        
        if ($response.data.result.Count -gt 0) {
            $value = $response.data.result[0].value[1]
            Write-Host "[OK] Prometheus ha dati - aibetting_price_updates_total: $value" -ForegroundColor Green
            return $true
        } else {
            Write-Host "[ERROR] Nessun dato in Prometheus per aibetting_price_updates_total" -ForegroundColor Red
            Write-Host "  FIX: Aspetta 30-60 secondi che Prometheus faccia scraping" -ForegroundColor Yellow
            return $false
        }
    } catch {
        Write-Host "[ERROR] Impossibile query Prometheus" -ForegroundColor Red
        return $false
    }
}

function Test-GrafanaContainer {
    $container = docker ps --filter "name=grafana" --format "{{.Names}}" 2>$null
    if ($container) {
        Write-Host "[OK] Grafana container attivo: $container" -ForegroundColor Green
        return $true
    } else {
        Write-Host "[ERROR] Grafana container non attivo" -ForegroundColor Red
        Write-Host "  FIX: docker-compose --profile monitoring up -d grafana" -ForegroundColor Yellow
        return $false
    }
}

function Test-GrafanaDashboard {
    try {
        $auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:admin"))
        $headers = @{ "Authorization" = "Basic $auth" }
        
        $dashboards = Invoke-RestMethod -Uri "http://localhost:3000/api/search?type=dash-db" -Headers $headers -TimeoutSec 3
        $dashboard = $dashboards | Where-Object { $_.uid -eq "aibetting-explorer" }
        
        if ($dashboard) {
            Write-Host "[OK] Dashboard 'aibetting-explorer' trovata" -ForegroundColor Green
            Write-Host "  URL: http://localhost:3000/d/$($dashboard.uid)" -ForegroundColor Gray
            return $true
        } else {
            Write-Host "[ERROR] Dashboard 'aibetting-explorer' NON importata" -ForegroundColor Red
            Write-Host "  FIX: Importa dashboard manualmente:" -ForegroundColor Yellow
            Write-Host "    1. Apri http://localhost:3000 (admin/admin)" -ForegroundColor White
            Write-Host "    2. Dashboards -> Import" -ForegroundColor White
            Write-Host "    3. Upload grafana-dashboard-explorer.json" -ForegroundColor White
            Write-Host "    4. UID deve essere: aibetting-explorer" -ForegroundColor White
            return $false
        }
    } catch {
        Write-Host "[ERROR] Impossibile verificare dashboard (autenticazione?)" -ForegroundColor Red
        return $false
    }
}

# Main
Write-Host ""
Write-Host "========================================"  -ForegroundColor Cyan
Write-Host "  Diagnostica Monitoring Stack"  -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$results = @()

Write-Host "[1/6] Test Explorer metriche..." -ForegroundColor Yellow
$results += Test-ExplorerMetrics
Write-Host ""

Write-Host "[2/6] Test Prometheus container..." -ForegroundColor Yellow
$results += Test-PrometheusContainer
Write-Host ""

Write-Host "[3/6] Test Prometheus target..." -ForegroundColor Yellow
$results += Test-PrometheusTarget
Write-Host ""

Write-Host "[4/6] Test dati in Prometheus..." -ForegroundColor Yellow
$results += Test-PrometheusData
Write-Host ""

Write-Host "[5/6] Test Grafana container..." -ForegroundColor Yellow
$results += Test-GrafanaContainer
Write-Host ""

Write-Host "[6/6] Test dashboard Grafana..." -ForegroundColor Yellow
$results += Test-GrafanaDashboard
Write-Host ""

# Summary
Write-Host "========================================"  -ForegroundColor Cyan
$passCount = ($results | Where-Object { $_ -eq $true }).Count
$failCount = ($results | Where-Object { $_ -eq $false }).Count

if ($failCount -eq 0) {
    Write-Host "  TUTTI I TEST PASSATI! ($passCount/6)" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Dashboard dovrebbe funzionare:" -ForegroundColor Cyan
    Write-Host "  Grafana UI: http://localhost:3000/d/aibetting-explorer" -ForegroundColor White
    Write-Host "  Blazor UI:  http://localhost:5000/monitoring" -ForegroundColor White
    Write-Host ""
    Write-Host "Se dashboard ancora vuota:" -ForegroundColor Yellow
    Write-Host "  1. Aspetta 10-20 secondi" -ForegroundColor White
    Write-Host "  2. Refresh browser (Ctrl+F5)" -ForegroundColor White
    Write-Host "  3. Verifica panel -> Edit -> Data Source = Prometheus" -ForegroundColor White
} else {
    Write-Host "  $failCount TEST FALLITI su 6" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Segui i FIX indicati sopra per risolvere i problemi" -ForegroundColor Yellow
}

Write-Host ""
