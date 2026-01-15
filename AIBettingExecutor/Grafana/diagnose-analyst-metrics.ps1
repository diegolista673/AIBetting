# ============================================
# Diagnose AIBettingAnalyst Metrics Issue
# ============================================

Write-Host "Diagnosing AIBettingAnalyst Metrics" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Check if Analyst is running
Write-Host "1. Checking Analyst process..." -ForegroundColor Yellow
$analystProcess = Get-Process -Name "AIBettingAnalyst" -ErrorAction SilentlyContinue
if ($analystProcess) {
    Write-Host "   OK - AIBettingAnalyst is running (PID: $($analystProcess.Id))" -ForegroundColor Green
    Write-Host "   Memory: $([math]::Round($analystProcess.WorkingSet64/1MB, 2)) MB" -ForegroundColor Gray
    Write-Host "   CPU Time: $([math]::Round($analystProcess.CPU, 2))s" -ForegroundColor Gray
} else {
    Write-Host "   ERROR - AIBettingAnalyst is not running!" -ForegroundColor Red
    Write-Host "   Start it with: cd AIBettingAnalyst; dotnet run" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Step 2: Test metrics endpoint
Write-Host "2. Testing metrics endpoint (localhost:5002/metrics)..." -ForegroundColor Yellow
try {
    $metrics = Invoke-WebRequest -Uri "http://localhost:5002/metrics" -UseBasicParsing -ErrorAction Stop
    Write-Host "   OK - Metrics endpoint is accessible" -ForegroundColor Green
    Write-Host "   Status: $($metrics.StatusCode)" -ForegroundColor Gray
    Write-Host "   Content Length: $($metrics.Content.Length) bytes" -ForegroundColor Gray
} catch {
    Write-Host "   ERROR - Cannot reach metrics endpoint" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 3: Check for AIBetting custom metrics
Write-Host "3. Checking for AIBetting custom metrics..." -ForegroundColor Yellow
$customMetrics = $metrics.Content | Select-String "aibetting_analyst" -AllMatches

if ($customMetrics.Matches.Count -gt 0) {
    Write-Host "   OK - Found $($customMetrics.Matches.Count) AIBetting metrics" -ForegroundColor Green
    
    # List metrics
    $metricNames = $metrics.Content -split "`n" | Select-String "^# TYPE aibetting_analyst" | ForEach-Object { 
        $_ -replace "^# TYPE ", "" -replace " .*", ""
    }
    
    foreach ($metric in $metricNames | Select-Object -Unique) {
        Write-Host "   - $metric" -ForegroundColor Cyan
    }
} else {
    Write-Host "   WARNING - No AIBetting custom metrics found!" -ForegroundColor Yellow
    Write-Host "   This means the Analyst is running but not processing data yet." -ForegroundColor Yellow
}

Write-Host ""

# Step 4: Check Prometheus can scrape
Write-Host "4. Checking Prometheus target status..." -ForegroundColor Yellow
try {
    $promTargets = Invoke-RestMethod -Uri "http://localhost:9090/api/v1/targets" -Method Get
    $analystTarget = $promTargets.data.activeTargets | Where-Object { $_.labels.job -eq "aibetting-analyst" }
    
    if ($analystTarget) {
        if ($analystTarget.health -eq "up") {
            Write-Host "   OK - Prometheus is scraping Analyst successfully" -ForegroundColor Green
            Write-Host "   Last scrape: $($analystTarget.lastScrapeDuration)s ago" -ForegroundColor Gray
        } else {
            Write-Host "   ERROR - Prometheus target is DOWN" -ForegroundColor Red
            Write-Host "   Error: $($analystTarget.lastError)" -ForegroundColor Red
        }
    } else {
        Write-Host "   WARNING - Analyst target not found in Prometheus" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   WARNING - Cannot check Prometheus status" -ForegroundColor Yellow
}

Write-Host ""

# Step 5: Check if Explorer is running (data source)
Write-Host "5. Checking data pipeline..." -ForegroundColor Yellow
$explorerProcess = Get-Process -Name "AIBettingExplorer" -ErrorAction SilentlyContinue
if ($explorerProcess) {
    Write-Host "   OK - AIBettingExplorer is running (sending data)" -ForegroundColor Green
} else {
    Write-Host "   WARNING - AIBettingExplorer is not running!" -ForegroundColor Yellow
    Write-Host "   Analyst needs Explorer to send price updates" -ForegroundColor Yellow
    Write-Host "   Start it with: cd AIBettingExplorer; dotnet run" -ForegroundColor Cyan
}

Write-Host ""

# Step 6: Check Redis for price updates
Write-Host "6. Checking Redis for recent price updates..." -ForegroundColor Yellow
try {
    $redisKeys = docker exec aibetting-redis redis-cli --scan --pattern "prices:*" --count 5 2>$null
    if ($redisKeys) {
        $keyCount = ($redisKeys | Measure-Object).Count
        Write-Host "   OK - Found $keyCount price snapshot(s) in Redis" -ForegroundColor Green
        Write-Host "   Recent key: $($redisKeys | Select-Object -First 1)" -ForegroundColor Gray
    } else {
        Write-Host "   WARNING - No price snapshots found in Redis" -ForegroundColor Yellow
        Write-Host "   This means Explorer is not publishing data" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   WARNING - Cannot check Redis" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "====================================" -ForegroundColor Cyan
Write-Host "Diagnosis Summary" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

if ($customMetrics.Matches.Count -gt 0) {
    Write-Host "RESULT: Metrics are working correctly!" -ForegroundColor Green
    Write-Host ""
    Write-Host "If Grafana shows 'No data':" -ForegroundColor Yellow
    Write-Host "  1. Check time range (try 'Last 5 minutes')" -ForegroundColor White
    Write-Host "  2. Verify query matches metric names" -ForegroundColor White
    Write-Host "  3. Wait a few minutes for data to accumulate" -ForegroundColor White
} else {
    Write-Host "RESULT: Metrics exist but no data yet" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Root Cause: Analyst is running but not receiving price updates" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Solutions:" -ForegroundColor Cyan
    Write-Host "  1. Start AIBettingExplorer to generate price updates:" -ForegroundColor White
    Write-Host "     cd AIBettingExplorer; dotnet run" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  2. Verify Redis is accessible:" -ForegroundColor White
    Write-Host "     docker exec aibetting-redis redis-cli ping" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  3. Check Analyst logs for errors:" -ForegroundColor White
    Write-Host "     Check AIBettingAnalyst\logs\analyst-*.log" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Query Example for Grafana:" -ForegroundColor Cyan
Write-Host "  rate(aibetting_analyst_snapshots_processed_total[5m])" -ForegroundColor White
Write-Host "  aibetting_analyst_signals_generated_total" -ForegroundColor White
Write-Host "  aibetting_analyst_strategy_avg_confidence" -ForegroundColor White
Write-Host ""
