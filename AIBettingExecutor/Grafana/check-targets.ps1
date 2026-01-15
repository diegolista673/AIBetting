# ============================================
# Check Prometheus Targets Status
# ============================================

Write-Host "Checking Prometheus Targets..." -ForegroundColor Cyan
Write-Host "===============================" -ForegroundColor Cyan
Write-Host ""

try {
    # Get targets from Prometheus API
    $response = Invoke-RestMethod -Uri "http://localhost:9090/api/v1/targets" -Method Get
    
    if ($response.status -ne "success") {
        Write-Host "ERROR - Failed to get targets from Prometheus" -ForegroundColor Red
        exit 1
    }
    
    $targets = $response.data.activeTargets
    
    # Group targets by health status
    $upTargets = $targets | Where-Object { $_.health -eq "up" }
    $downTargets = $targets | Where-Object { $_.health -eq "down" }
    
    Write-Host "Summary:" -ForegroundColor Yellow
    Write-Host "  Total Targets: $($targets.Count)" -ForegroundColor White
    Write-Host "  UP:            $($upTargets.Count)" -ForegroundColor Green
    Write-Host "  DOWN:          $($downTargets.Count)" -ForegroundColor $(if ($downTargets.Count -gt 0) { "Red" } else { "Green" })
    Write-Host ""
    
    # Display UP targets
    if ($upTargets.Count -gt 0) {
        Write-Host "UP Targets:" -ForegroundColor Green
        foreach ($target in $upTargets) {
            $job = $target.labels.job
            $instance = if ($target.labels.instance) { $target.labels.instance } else { $target.scrapeUrl }
            Write-Host "  + $job" -ForegroundColor Green
            Write-Host "    Instance: $instance" -ForegroundColor Gray
            Write-Host "    Last Scrape: $($target.lastScrapeDuration)s ago" -ForegroundColor Gray
        }
        Write-Host ""
    }
    
    # Display DOWN targets
    if ($downTargets.Count -gt 0) {
        Write-Host "DOWN Targets:" -ForegroundColor Red
        foreach ($target in $downTargets) {
            $job = $target.labels.job
            $instance = if ($target.labels.instance) { $target.labels.instance } else { $target.scrapeUrl }
            $error = if ($target.lastError) { $target.lastError } else { "Unknown error" }
            
            Write-Host "  - $job" -ForegroundColor Red
            Write-Host "    Instance: $instance" -ForegroundColor Gray
            Write-Host "    Error: $error" -ForegroundColor Yellow
        }
        Write-Host ""
        
        Write-Host "Troubleshooting DOWN Targets:" -ForegroundColor Yellow
        Write-Host ""
        
        # Check AIBetting apps
        $aiBettingDown = $downTargets | Where-Object { $_.labels.job -match "aibetting" }
        if ($aiBettingDown.Count -gt 0) {
            Write-Host "AIBetting Applications are DOWN:" -ForegroundColor Yellow
            Write-Host "  The applications need to be running and exposing metrics" -ForegroundColor White
            Write-Host ""
            Write-Host "  To start them:" -ForegroundColor Cyan
            Write-Host "    cd AIBettingExplorer;  dotnet run  # Port 5001" -ForegroundColor Gray
            Write-Host "    cd AIBettingAnalyst;   dotnet run  # Port 5002" -ForegroundColor Gray
            Write-Host "    cd AIBettingExecutor;  dotnet run  # Port 5003" -ForegroundColor Gray
            Write-Host ""
            Write-Host "  Once started, check their metrics endpoints:" -ForegroundColor Cyan
            Write-Host "    curl http://localhost:5001/metrics" -ForegroundColor Gray
            Write-Host "    curl http://localhost:5002/metrics" -ForegroundColor Gray
            Write-Host "    curl http://localhost:5003/metrics" -ForegroundColor Gray
            Write-Host ""
        }
        
        # Check Docker services
        $dockerDown = $downTargets | Where-Object { $_.labels.job -notmatch "aibetting" }
        if ($dockerDown.Count -gt 0) {
            Write-Host "Docker Services are DOWN:" -ForegroundColor Yellow
            Write-Host "  Check Docker containers:" -ForegroundColor White
            Write-Host "    docker compose ps" -ForegroundColor Gray
            Write-Host ""
            Write-Host "  Restart if needed:" -ForegroundColor White
            Write-Host "    cd AIBettingExecutor\Grafana" -ForegroundColor Gray
            Write-Host "    docker compose restart" -ForegroundColor Gray
            Write-Host ""
        }
    } else {
        Write-Host "All targets are UP!" -ForegroundColor Green
    }
    
    Write-Host "===============================" -ForegroundColor Cyan
    Write-Host "View full details: http://localhost:9090/targets" -ForegroundColor Cyan
    Write-Host ""
    
} catch {
    Write-Host "ERROR - Could not connect to Prometheus" -ForegroundColor Red
    Write-Host "  Make sure Prometheus is running:" -ForegroundColor Yellow
    Write-Host "    docker ps | findstr prometheus" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  Check Prometheus logs:" -ForegroundColor Yellow
    Write-Host "    docker logs aibetting-prometheus-v2 --tail=50" -ForegroundColor Gray
    Write-Host ""
    exit 1
}
