# Grafana Configuration Script for AIBetting Explorer
# Configures Prometheus data source and imports dashboard

$grafanaUrl = "http://localhost:3000"
$grafanaUser = "admin"
$grafanaPassword = "admin"

# Create base64 auth header
$base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${grafanaUser}:${grafanaPassword}"))
$headers = @{
    "Authorization" = "Basic $base64AuthInfo"
    "Content-Type" = "application/json"
}

Write-Host "`n🔧 Configurazione Grafana per AIBetting Explorer" -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Cyan

# Step 1: Check if Grafana is accessible
Write-Host "`n1️⃣  Verifica connessione a Grafana..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$grafanaUrl/api/health" -Method Get
    Write-Host "   ✅ Grafana raggiungibile - Version: $($health.version)" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Grafana non raggiungibile su $grafanaUrl" -ForegroundColor Red
    Write-Host "      Assicurati che il container sia attivo: docker ps | Select-String grafana" -ForegroundColor Yellow
    exit 1
}

# Step 2: Check if Prometheus data source already exists
Write-Host "`n2️⃣  Verifica data source Prometheus..." -ForegroundColor Yellow
try {
    $dataSources = Invoke-RestMethod -Uri "$grafanaUrl/api/datasources" -Headers $headers -Method Get
    $prometheusDs = $dataSources | Where-Object { $_.type -eq "prometheus" -and $_.name -eq "Prometheus" }
    
    if ($prometheusDs) {
        Write-Host "   ℹ️  Data source 'Prometheus' già esistente (ID: $($prometheusDs.id))" -ForegroundColor Cyan
        $datasourceId = $prometheusDs.id
    } else {
        Write-Host "   📊 Creazione data source Prometheus..." -ForegroundColor Yellow
        
        $datasourceConfig = @{
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
        
        $newDs = Invoke-RestMethod -Uri "$grafanaUrl/api/datasources" -Headers $headers -Method Post -Body $datasourceConfig
        Write-Host "   ✅ Data source Prometheus creato (ID: $($newDs.id))" -ForegroundColor Green
        $datasourceId = $newDs.id
    }
} catch {
    Write-Host "   ❌ Errore durante la configurazione del data source" -ForegroundColor Red
    Write-Host "      $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 3: Test Prometheus connection
Write-Host "`n3️⃣  Test connessione Prometheus..." -ForegroundColor Yellow
try {
    $testResult = Invoke-RestMethod -Uri "$grafanaUrl/api/datasources/$datasourceId/health" -Headers $headers -Method Get
    if ($testResult.status -eq "OK") {
        Write-Host "   ✅ Connessione a Prometheus funzionante!" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  Connessione Prometheus: $($testResult.message)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ⚠️  Test connessione non disponibile, procedo comunque..." -ForegroundColor Yellow
}

# Step 4: Import dashboard
Write-Host "`n4️⃣  Import dashboard AIBetting Explorer..." -ForegroundColor Yellow

if (Test-Path "grafana-dashboard-explorer.json") {
    try {
        $dashboardJson = Get-Content "grafana-dashboard-explorer.json" -Raw | ConvertFrom-Json
        
        # Prepare dashboard for import
        $importPayload = @{
            dashboard = $dashboardJson
            overwrite = $true
            inputs = @(
                @{
                    name = "DS_PROMETHEUS"
                    type = "datasource"
                    pluginId = "prometheus"
                    value = "Prometheus"
                }
            )
        } | ConvertTo-Json -Depth 10
        
        $importResult = Invoke-RestMethod -Uri "$grafanaUrl/api/dashboards/import" -Headers $headers -Method Post -Body $importPayload
        Write-Host "   ✅ Dashboard importata con successo!" -ForegroundColor Green
        Write-Host "      URL: $grafanaUrl/d/$($importResult.uid)" -ForegroundColor Gray
        
        # Open dashboard in browser
        Start-Process "$grafanaUrl/d/$($importResult.uid)"
    } catch {
        Write-Host "   ⚠️  Errore durante l'import della dashboard" -ForegroundColor Yellow
        Write-Host "      $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "      Puoi importarla manualmente da Grafana UI" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ⚠️  File grafana-dashboard-explorer.json non trovato" -ForegroundColor Yellow
    Write-Host "      Importa manualmente la dashboard dalla UI di Grafana" -ForegroundColor Yellow
}

# Summary
Write-Host "`n" + "=" * 60 -ForegroundColor Cyan
Write-Host "✅ CONFIGURAZIONE COMPLETATA!" -ForegroundColor Green
Write-Host "=" * 60 -ForegroundColor Cyan

Write-Host "`n📊 Informazioni:" -ForegroundColor Cyan
Write-Host "   • Grafana URL: $grafanaUrl" -ForegroundColor White
Write-Host "   • Data Source: Prometheus ($grafanaUrl/datasources)" -ForegroundColor White
Write-Host "   • Dashboards: $grafanaUrl/dashboards" -ForegroundColor White

Write-Host "`n🎯 Query Prometheus disponibili:" -ForegroundColor Cyan
Write-Host "   • aibetting_price_updates_total" -ForegroundColor White
Write-Host "   • rate(aibetting_price_updates_total[1m])" -ForegroundColor White
Write-Host "   • histogram_quantile(0.95, rate(aibetting_processing_latency_seconds_bucket[1m]))" -ForegroundColor White

Write-Host "`n📈 Panels nella dashboard:" -ForegroundColor Cyan
Write-Host "   1. Total Price Updates - Contatore totale snapshots" -ForegroundColor White
Write-Host "   2. Price Updates Rate - Updates al secondo" -ForegroundColor White
Write-Host "   3. Processing Latency (p50/p95/p99) - Grafici latenza" -ForegroundColor White
Write-Host "   4. Average Latency - Latenza media" -ForegroundColor White
Write-Host "   5. p95 Latency - 95° percentile (target <50ms)" -ForegroundColor White
Write-Host "   6. Total Snapshots Processed - Contatore elaborati`n" -ForegroundColor White
