# Complete Setup Script - Grafana Dashboard Import
# This script imports the Explorer dashboard into Grafana and verifies everything works

Write-Host "`n" -NoNewline
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Grafana Dashboard Setup - AIBetting" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Check prerequisites
Write-Host "[1/6] Checking prerequisites..." -ForegroundColor Yellow

# Check Grafana
try {
    $grafana = Invoke-WebRequest -Uri "http://localhost:3000/api/health" -UseBasicParsing -TimeoutSec 3
    Write-Host "  OK Grafana is running" -ForegroundColor Green
} catch {
    Write-Host "  ERROR Grafana not accessible" -ForegroundColor Red
    Write-Host "  Start with: docker-compose --profile monitoring up -d grafana" -ForegroundColor White
    exit 1
}

# Check Explorer
Write-Host "`n[2/6] Checking Explorer metrics..." -ForegroundColor Yellow
try {
    $metrics = Invoke-WebRequest -Uri "http://localhost:5001/metrics" -UseBasicParsing -TimeoutSec 3
    
    if ($metrics.Content -match "aibetting_price_updates_total\s+(\d+)") {
        $count = $matches[1]
        Write-Host "  OK Explorer is running - Updates: $count" -ForegroundColor Green
    } else {
        Write-Host "  WARNING Explorer running but no aibetting metrics found" -ForegroundColor Yellow
        Write-Host "  You may need to restart Explorer" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  ERROR Explorer not accessible on :5001" -ForegroundColor Red
    Write-Host "  Start with: cd AIBettingExplorer; dotnet run" -ForegroundColor White
    exit 1
}

# Step 3: Check Prometheus
Write-Host "`n[3/6] Checking Prometheus..." -ForegroundColor Yellow
try {
    $prom = Invoke-WebRequest -Uri "http://localhost:9090/api/v1/targets" -UseBasicParsing -TimeoutSec 3
    $targets = $prom.Content | ConvertFrom-Json
    $explorerTarget = $targets.data.activeTargets | Where-Object { $_.labels.job -eq 'aibetting-explorer' }
    
    if ($explorerTarget -and $explorerTarget.health -eq "up") {
        Write-Host "  OK Prometheus target 'aibetting-explorer' is UP" -ForegroundColor Green
    } else {
        Write-Host "  WARNING Target 'aibetting-explorer' is DOWN or not found" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  ERROR Prometheus not accessible" -ForegroundColor Red
    exit 1
}

# Step 4: Check if dashboard exists
Write-Host "`n[4/6] Checking existing dashboards..." -ForegroundColor Yellow
$dashboardExists = $false
try {
    $auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:admin"))
    $headers = @{
        "Authorization" = "Basic $auth"
        "Content-Type" = "application/json"
    }
    
    $dashboards = Invoke-RestMethod -Uri "http://localhost:3000/api/search?type=dash-db" -Headers $headers
    $explorerDash = $dashboards | Where-Object { $_.uid -eq "aibetting-explorer" }
    
    if ($explorerDash) {
        Write-Host "  INFO Dashboard 'aibetting-explorer' already exists" -ForegroundColor Cyan
        $dashboardExists = $true
    } else {
        Write-Host "  INFO Dashboard not found, will import" -ForegroundColor Cyan
    }
} catch {
    Write-Host "  WARNING Cannot check dashboards (authentication issue?)" -ForegroundColor Yellow
}

# Step 5: Import dashboard
Write-Host "`n[5/6] Importing dashboard..." -ForegroundColor Yellow

if (-not (Test-Path "grafana-dashboard-explorer.json")) {
    Write-Host "  ERROR grafana-dashboard-explorer.json not found" -ForegroundColor Red
    Write-Host "  Make sure you're in the solution root directory" -ForegroundColor White
    exit 1
}

try {
    $dashboardJson = Get-Content "grafana-dashboard-explorer.json" -Raw | ConvertFrom-Json
    
    # Ensure UID is set
    if (-not $dashboardJson.uid) {
        $dashboardJson | Add-Member -MemberType NoteProperty -Name "uid" -Value "aibetting-explorer" -Force
    }
    
    $importPayload = @{
        dashboard = $dashboardJson
        overwrite = $true
        inputs = @()
    } | ConvertTo-Json -Depth 20
    
    $result = Invoke-RestMethod -Uri "http://localhost:3000/api/dashboards/db" -Method Post -Headers $headers -Body $importPayload
    
    Write-Host "  OK Dashboard imported successfully!" -ForegroundColor Green
    Write-Host "     UID: $($result.uid)" -ForegroundColor Gray
    Write-Host "     URL: http://localhost:3000/d/$($result.uid)" -ForegroundColor Gray
    
} catch {
    Write-Host "  ERROR Failed to import dashboard" -ForegroundColor Red
    Write-Host "  $_" -ForegroundColor Red
    Write-Host "`n  Manual import instructions:" -ForegroundColor Yellow
    Write-Host "  1. Open http://localhost:3000" -ForegroundColor White
    Write-Host "  2. Login: admin / admin" -ForegroundColor White
    Write-Host "  3. Dashboards -> Import" -ForegroundColor White
    Write-Host "  4. Upload grafana-dashboard-explorer.json" -ForegroundColor White
}

# Step 6: Verify data in Prometheus
Write-Host "`n[6/6] Verifying data in Prometheus..." -ForegroundColor Yellow
try {
    $query = [System.Web.HttpUtility]::UrlEncode("aibetting_price_updates_total")
    $promData = Invoke-RestMethod -Uri "http://localhost:9090/api/v1/query?query=$query" -TimeoutSec 3
    
    if ($promData.data.result.Count -gt 0) {
        $value = $promData.data.result[0].value[1]
        Write-Host "  OK Prometheus has data - aibetting_price_updates_total: $value" -ForegroundColor Green
    } else {
        Write-Host "  WARNING No data in Prometheus yet" -ForegroundColor Yellow
        Write-Host "  Wait a few seconds and check again" -ForegroundColor White
    }
} catch {
    Write-Host "  WARNING Cannot query Prometheus" -ForegroundColor Yellow
}

# Summary
Write-Host "`n" -NoNewline
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Setup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Open Blazor Dashboard: http://localhost:5000/monitoring" -ForegroundColor White
Write-Host "2. Select 'Explorer Metrics' from dropdown" -ForegroundColor White
Write-Host "3. You should see live graphs updating!" -ForegroundColor White
Write-Host ""
Write-Host "Troubleshooting URLs:" -ForegroundColor Cyan
Write-Host "- Grafana UI: http://localhost:3000/d/aibetting-explorer" -ForegroundColor White
Write-Host "- Prometheus: http://localhost:9090/graph" -ForegroundColor White
Write-Host "- Explorer metrics: http://localhost:5001/metrics" -ForegroundColor White
Write-Host ""

$openBrowser = Read-Host "Open Blazor monitoring page now? (y/n)"
if ($openBrowser -eq "y") {
    Start-Sleep -Seconds 2
    Start-Process "http://localhost:5000/monitoring"
}
