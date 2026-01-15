# ============================================
# AIBetting Monitoring Stack - Complete Reset
# ============================================

param(
    [switch]$KeepData = $false
)

$ErrorActionPreference = "SilentlyContinue"

Write-Host ""
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "  AIBetting Monitoring Stack - Complete Reset  " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

# Navigate to directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

Write-Host "Working directory: $scriptPath" -ForegroundColor Gray
Write-Host ""

# ============================================
# STEP 1: Stop All AIBetting Containers
# ============================================
Write-Host "STEP 1: Stopping all AIBetting containers..." -ForegroundColor Yellow
Write-Host "----------------------------------------"

$allContainers = docker ps -a --filter "name=aibetting" --format "{{.Names}}"

if ($allContainers) {
    foreach ($container in $allContainers) {
        Write-Host "  Stopping: $container" -ForegroundColor Gray
        docker stop $container 2>&1 | Out-Null
    }
    Write-Host "[OK] All containers stopped" -ForegroundColor Green
} else {
    Write-Host "[INFO] No running containers found" -ForegroundColor Gray
}

Start-Sleep -Seconds 2

# ============================================
# STEP 2: Remove All AIBetting Containers
# ============================================
Write-Host ""
Write-Host "STEP 2: Removing all AIBetting containers..." -ForegroundColor Yellow
Write-Host "----------------------------------------"

$allContainers = docker ps -a --filter "name=aibetting" --format "{{.Names}}"

if ($allContainers) {
    foreach ($container in $allContainers) {
        Write-Host "  Removing: $container" -ForegroundColor Gray
        docker rm -f $container 2>&1 | Out-Null
    }
    Write-Host "[OK] All containers removed" -ForegroundColor Green
} else {
    Write-Host "[INFO] No containers to remove" -ForegroundColor Gray
}

# ============================================
# STEP 3: Clean Networks
# ============================================
Write-Host ""
Write-Host "STEP 3: Cleaning networks..." -ForegroundColor Yellow
Write-Host "----------------------------------------"

$networks = @("aibetting-monitoring", "aibetting-monitoring-v2", "aibetting_default")

foreach ($network in $networks) {
    $exists = docker network ls --filter "name=$network" --format "{{.Name}}"
    if ($exists) {
        Write-Host "  Removing network: $network" -ForegroundColor Gray
        docker network rm $network 2>&1 | Out-Null
    }
}

Write-Host "[OK] Networks cleaned" -ForegroundColor Green

# ============================================
# STEP 4: Clean Volumes (Optional)
# ============================================
if (-not $KeepData) {
    Write-Host ""
    Write-Host "STEP 4: Cleaning volumes..." -ForegroundColor Yellow
    Write-Host "----------------------------------------"
    
    $volumes = docker volume ls --filter "name=grafana" --format "{{.Name}}"
    
    if ($volumes) {
        foreach ($volume in $volumes) {
            Write-Host "  Removing volume: $volume" -ForegroundColor Gray
            docker volume rm $volume 2>&1 | Out-Null
        }
        Write-Host "[OK] Volumes cleaned" -ForegroundColor Green
    } else {
        Write-Host "[INFO] No volumes to remove" -ForegroundColor Gray
    }
} else {
    Write-Host ""
    Write-Host "STEP 4: Keeping data volumes (--KeepData flag)" -ForegroundColor Cyan
}

# ============================================
# STEP 5: System Cleanup
# ============================================
Write-Host ""
Write-Host "STEP 5: System cleanup..." -ForegroundColor Yellow
Write-Host "----------------------------------------"

docker system prune -f 2>&1 | Out-Null
Write-Host "[OK] Cleanup complete" -ForegroundColor Green

Start-Sleep -Seconds 2

# ============================================
# STEP 6: Verify Cleanup
# ============================================
Write-Host ""
Write-Host "STEP 6: Verifying cleanup..." -ForegroundColor Yellow
Write-Host "----------------------------------------"

$remaining = docker ps -a --filter "name=aibetting" --format "{{.Names}}"

if ($remaining) {
    Write-Host "[WARNING] Some containers still exist:" -ForegroundColor Red
    foreach ($container in $remaining) {
        Write-Host "    - $container" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "Try running this script as Administrator" -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "[OK] All containers successfully removed" -ForegroundColor Green
}

# ============================================
# STEP 7: Start Fresh Stack
# ============================================
Write-Host ""
Write-Host "STEP 7: Starting fresh monitoring stack..." -ForegroundColor Cyan
Write-Host "----------------------------------------"

# Check if docker-compose.yml exists
if (-not (Test-Path "docker-compose.yml")) {
    Write-Host "[ERROR] docker-compose.yml not found!" -ForegroundColor Red
    Write-Host "   Make sure you're in the correct directory." -ForegroundColor Yellow
    exit 1
}

# Start services
Write-Host "Starting services..." -ForegroundColor Gray
docker-compose up -d

# Wait for services to initialize
Write-Host "Waiting for services to start..." -ForegroundColor Gray
Start-Sleep -Seconds 8

# ============================================
# STEP 8: Status Check
# ============================================
Write-Host ""
Write-Host "STEP 8: Container status..." -ForegroundColor Cyan
Write-Host "----------------------------------------"

docker-compose ps

# ============================================
# STEP 9: Health Checks
# ============================================
Write-Host ""
Write-Host "STEP 9: Health checks..." -ForegroundColor Cyan
Write-Host "----------------------------------------"

$services = @(
    @{Name="Prometheus"; URL="http://localhost:9090/-/healthy"; Port=9090},
    @{Name="Grafana"; URL="http://localhost:3000/api/health"; Port=3000},
    @{Name="Alertmanager"; URL="http://localhost:9093/-/healthy"; Port=9093}
)

foreach ($service in $services) {
    try {
        $response = Invoke-WebRequest -Uri $service.URL -TimeoutSec 5 -UseBasicParsing 2>&1
        if ($response.StatusCode -eq 200) {
            Write-Host "  [OK] $($service.Name) is healthy" -ForegroundColor Green
        } else {
            Write-Host "  [WARN] $($service.Name) returned status: $($response.StatusCode)" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "  [ERROR] $($service.Name) is not responding" -ForegroundColor Red
    }
}

# ============================================
# STEP 10: Final Summary
# ============================================
Write-Host ""
Write-Host "=================================================" -ForegroundColor Green
Write-Host "            Setup Complete!                     " -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green

Write-Host ""
Write-Host "Access URLs:" -ForegroundColor Cyan
Write-Host "  Grafana:       http://localhost:3000" -ForegroundColor White
Write-Host "  Login:         admin / admin" -ForegroundColor Gray
Write-Host ""
Write-Host "  Prometheus:    http://localhost:9090" -ForegroundColor White
Write-Host "  Targets:       http://localhost:9090/targets" -ForegroundColor Gray
Write-Host ""
Write-Host "  Alertmanager:  http://localhost:9093" -ForegroundColor White
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Open Grafana (http://localhost:3000)" -ForegroundColor White
Write-Host "  2. Add Prometheus data source" -ForegroundColor White
Write-Host "  3. Import dashboard from: executor-dashboard.json" -ForegroundColor White
Write-Host "  4. Start AIBettingExecutor: dotnet run" -ForegroundColor White
Write-Host ""

# Check for errors in logs
Write-Host "Checking for errors in logs..." -ForegroundColor Yellow
$errors = docker-compose logs --tail=20 2>&1 | Select-String -Pattern "error|fatal|fail" -CaseSensitive:$false

if ($errors) {
    Write-Host "[WARNING] Found potential issues:" -ForegroundColor Yellow
    $errors | Select-Object -First 5 | ForEach-Object {
        Write-Host "   $_" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "View full logs with: docker-compose logs" -ForegroundColor Yellow
} else {
    Write-Host "[OK] No errors found in recent logs" -ForegroundColor Green
}

Write-Host ""
Write-Host "Monitoring stack is ready!" -ForegroundColor Green
Write-Host ""
