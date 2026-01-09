# AIBetting Monitoring Stack - Complete Status Check
# Verifies all components are working correctly

Write-Host "`n" -NoNewline
Write-Host "=" * 70 -ForegroundColor Cyan
Write-Host "  AIBetting Monitoring Stack - Status Check" -ForegroundColor Cyan
Write-Host "=" * 70 -ForegroundColor Cyan

$allGood = $true

# Check 1: Explorer Running
Write-Host "`n[1/6] AIBettingExplorer..." -ForegroundColor Yellow
try {
    $explorerMetrics = Invoke-WebRequest -Uri "http://localhost:5001/metrics" -UseBasicParsing -TimeoutSec 3
    if ($explorerMetrics.Content -match "aibetting_price_updates_total\s+(\d+)") {
        $updates = $matches[1]
        Write-Host "  [OK] Explorer is running - Updates: $updates" -ForegroundColor Green
    } else {
        Write-Host "  [WARNING] Explorer metrics not found" -ForegroundColor Yellow
        $allGood = $false
    }
} catch {
    Write-Host "  [ERROR] Explorer not accessible on :5001" -ForegroundColor Red
    $allGood = $false
}

# Check 2: Redis
Write-Host "`n[2/6] Redis..." -ForegroundColor Yellow
$redisContainer = docker ps --filter "name=redis" --format "{{.Names}}: {{.Status}}" 2>$null
if ($redisContainer) {
    Write-Host "  [OK] $redisContainer" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] Redis container not running" -ForegroundColor Red
    $allGood = $false
}

# Check 3: Prometheus
Write-Host "`n[3/6] Prometheus..." -ForegroundColor Yellow
$promContainer = docker ps --filter "name=prometheus" --format "{{.Names}}: {{.Status}}" 2>$null
if ($promContainer) {
    Write-Host "  [OK] $promContainer" -ForegroundColor Green
    
    # Check target
    try {
        $targets = Invoke-RestMethod -Uri "http://localhost:9090/api/v1/targets" -TimeoutSec 3
        $explorerTarget = $targets.data.activeTargets | Where-Object { $_.labels.job -eq 'aibetting-explorer' }
        
        if ($explorerTarget -and $explorerTarget.health -eq "up") {
            Write-Host "  [OK] Target 'aibetting-explorer' is UP" -ForegroundColor Green
        } else {
            Write-Host "  [WARNING] Target 'aibetting-explorer' is DOWN" -ForegroundColor Yellow
            $allGood = $false
        }
    } catch {
        Write-Host "  [WARNING] Cannot check Prometheus targets" -ForegroundColor Yellow
    }
} else {
    Write-Host "  [ERROR] Prometheus container not running" -ForegroundColor Red
    $allGood = $false
}

# Check 4: Prometheus Query
Write-Host "`n[4/6] Prometheus Data..." -ForegroundColor Yellow
try {
    $query = [System.Web.HttpUtility]::UrlEncode("aibetting_price_updates_total")
    $result = Invoke-RestMethod -Uri "http://localhost:9090/api/v1/query?query=$query" -TimeoutSec 3
    
    if ($result.data.result.Count -gt 0) {
        $value = $result.data.result[0].value[1]
        Write-Host "  [OK] Prometheus has data - aibetting_price_updates_total: $value" -ForegroundColor Green
    } else {
        Write-Host "  [WARNING] No data in Prometheus" -ForegroundColor Yellow
        $allGood = $false
    }
} catch {
    Write-Host "  [ERROR] Cannot query Prometheus" -ForegroundColor Red
    $allGood = $false
}

# Check 5: Grafana
Write-Host "`n[5/6] Grafana..." -ForegroundColor Yellow
$grafanaContainer = docker ps --filter "name=grafana" --format "{{.Names}}: {{.Status}}" 2>$null
if ($grafanaContainer) {
    Write-Host "  [OK] $grafanaContainer" -ForegroundColor Green
    
    # Check data source
    try {
        $auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:admin"))
        $headers = @{"Authorization" = "Basic $auth"}
        $dataSources = Invoke-RestMethod -Uri "http://localhost:3000/api/datasources" -Headers $headers -TimeoutSec 3
        $prometheusDs = $dataSources | Where-Object { $_.type -eq "prometheus" }
        
        if ($prometheusDs) {
            Write-Host "  [OK] Prometheus data source configured (ID: $($prometheusDs.id))" -ForegroundColor Green
        } else {
            Write-Host "  [WARNING] Prometheus data source not configured" -ForegroundColor Yellow
            $allGood = $false
        }
    } catch {
        Write-Host "  [WARNING] Cannot check Grafana configuration" -ForegroundColor Yellow
    }
} else {
    Write-Host "  [ERROR] Grafana container not running" -ForegroundColor Red
    $allGood = $false
}

# Check 6: Performance Metrics
Write-Host "`n[6/6] Performance..." -ForegroundColor Yellow
try {
    $explorerMetrics = Invoke-WebRequest -Uri "http://localhost:5001/metrics" -UseBasicParsing -TimeoutSec 3
    
    if ($explorerMetrics.Content -match "aibetting_processing_latency_seconds_sum\s+([\d.]+)") {
        $latencySum = [decimal]$matches[1]
        if ($explorerMetrics.Content -match "aibetting_processing_latency_seconds_count\s+(\d+)") {
            $latencyCount = [int]$matches[1]
            if ($latencyCount -gt 0) {
                $avgLatencyMs = ($latencySum / $latencyCount) * 1000
                $status = if ($avgLatencyMs -lt 50) { "[OK]" } elseif ($avgLatencyMs -lt 100) { "[WARNING]" } else { "[ERROR]" }
                $color = if ($avgLatencyMs -lt 50) { "Green" } elseif ($avgLatencyMs -lt 100) { "Yellow" } else { "Red" }
                Write-Host "  $status Average latency: $($avgLatencyMs.ToString('F2'))ms (Target: <50ms)" -ForegroundColor $color
            }
        }
    }
} catch {
    Write-Host "  [WARNING] Cannot calculate performance metrics" -ForegroundColor Yellow
}

# Summary
Write-Host "`n" -NoNewline
Write-Host "=" * 70 -ForegroundColor Cyan

if ($allGood) {
    Write-Host "  STATUS: ALL SYSTEMS OPERATIONAL" -ForegroundColor Green
    Write-Host "=" * 70 -ForegroundColor Cyan
    
    Write-Host "`nAccess URLs:" -ForegroundColor Cyan
    Write-Host "  - Explorer Metrics: http://localhost:5001/metrics" -ForegroundColor White
    Write-Host "  - Prometheus UI:    http://localhost:9090" -ForegroundColor White
    Write-Host "  - Grafana:          http://localhost:3000 (admin/admin)" -ForegroundColor White
    
    Write-Host "`nNext Steps:" -ForegroundColor Cyan
    Write-Host "  1. Open Grafana: http://localhost:3000" -ForegroundColor White
    Write-Host "  2. Import dashboard: grafana-dashboard-explorer.json" -ForegroundColor White
    Write-Host "  3. View real-time metrics!" -ForegroundColor White
} else {
    Write-Host "  STATUS: SOME ISSUES DETECTED" -ForegroundColor Yellow
    Write-Host "=" * 70 -ForegroundColor Cyan
    
    Write-Host "`nTroubleshooting:" -ForegroundColor Yellow
    Write-Host "  - Check logs: docker logs <container-name>" -ForegroundColor White
    Write-Host "  - Restart services: docker-compose restart" -ForegroundColor White
    Write-Host "  - See guide: MONITORING-SETUP.md" -ForegroundColor White
}

Write-Host ""
