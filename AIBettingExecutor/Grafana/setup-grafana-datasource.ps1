# ============================================
# Grafana Prometheus Data Source Setup
# ============================================

Write-Host "Configuring Grafana Prometheus Data Source..." -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# Grafana configuration
$grafanaUrl = "http://localhost:3000"
$username = "admin"
$password = "admin"

# Create base64 auth header
$base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${username}:${password}"))
$headers = @{
    "Authorization" = "Basic $base64AuthInfo"
    "Content-Type" = "application/json"
}

Write-Host "1. Checking Grafana health..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$grafanaUrl/api/health" -Method Get
    Write-Host "   OK - Grafana is healthy (version: $($health.version))" -ForegroundColor Green
} catch {
    Write-Host "   ERROR - Cannot connect to Grafana" -ForegroundColor Red
    Write-Host "   Make sure Grafana is running: docker ps | findstr grafana" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "2. Checking existing data sources..." -ForegroundColor Yellow
try {
    $dataSources = Invoke-RestMethod -Uri "$grafanaUrl/api/datasources" -Method Get -Headers $headers
    $prometheusDs = $dataSources | Where-Object { $_.type -eq "prometheus" -and $_.name -eq "Prometheus" }
    
    if ($prometheusDs) {
        Write-Host "   INFO - Prometheus data source already exists (ID: $($prometheusDs.id))" -ForegroundColor Cyan
        Write-Host "   Deleting old data source..." -ForegroundColor Yellow
        $null = Invoke-RestMethod -Uri "$grafanaUrl/api/datasources/$($prometheusDs.id)" -Method Delete -Headers $headers
        Write-Host "   OK - Old data source deleted" -ForegroundColor Green
    }
} catch {
    Write-Host "   INFO - No existing Prometheus data source found" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "3. Creating new Prometheus data source..." -ForegroundColor Yellow

$dataSourceConfig = @{
    name = "Prometheus"
    type = "prometheus"
    access = "proxy"
    url = "http://prometheus:9090"
    isDefault = $true
    jsonData = @{
        httpMethod = "POST"
        timeInterval = "15s"
    }
} | ConvertTo-Json

try {
    $result = Invoke-RestMethod -Uri "$grafanaUrl/api/datasources" -Method Post -Headers $headers -Body $dataSourceConfig
    Write-Host "   OK - Prometheus data source created successfully!" -ForegroundColor Green
    Write-Host "   ID: $($result.id)" -ForegroundColor Cyan
    Write-Host "   Name: $($result.name)" -ForegroundColor Cyan
    Write-Host "   URL: $($result.url)" -ForegroundColor Cyan
} catch {
    Write-Host "   ERROR - Failed to create data source" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "4. Testing Prometheus data source..." -ForegroundColor Yellow
Start-Sleep -Seconds 2
try {
    $testUrl = "$grafanaUrl/api/datasources/proxy/$($result.id)/api/v1/query?query=up"
    $testResult = Invoke-RestMethod -Uri $testUrl -Method Get -Headers $headers -ErrorAction Stop
    if ($testResult.status -eq "success") {
        Write-Host "   OK - Data source test successful!" -ForegroundColor Green
        Write-Host "   Prometheus is reachable from Grafana" -ForegroundColor Green
    }
} catch {
    Write-Host "   WARNING - Could not verify data source (might still work)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Grafana configuration complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Open Grafana: http://localhost:3000" -ForegroundColor White
Write-Host "  2. Login: admin/admin" -ForegroundColor White
Write-Host "  3. Go to Configuration -> Data Sources to verify" -ForegroundColor White
Write-Host "  4. Create or import dashboards" -ForegroundColor White
Write-Host ""
Write-Host "Prometheus Targets:" -ForegroundColor Yellow
Write-Host "  http://localhost:9090/targets" -ForegroundColor Cyan
Write-Host ""
