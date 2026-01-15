# ============================================
# End-to-End Stack Test
# ============================================

Write-Host "🧪 AIBetting Stack End-to-End Test" -ForegroundColor Cyan
Write-Host "==================================`n" -ForegroundColor Cyan

$allPassed = $true

# Test 1: Docker Containers
Write-Host "1️⃣  Testing Docker Containers..." -ForegroundColor Yellow
$containers = @("aibetting-prometheus-v2", "aibetting-grafana", "aibetting-redis", "aibetting-postgres", "aibetting-alertmanager")
foreach ($container in $containers) {
    $status = docker ps --filter "name=$container" --format "{{.Status}}"
    if ($status -match "Up") {
        Write-Host "   ✅ $container is running" -ForegroundColor Green
    } else {
        Write-Host "   ❌ $container is NOT running" -ForegroundColor Red
        $allPassed = $false
    }
}
Write-Host ""

# Test 2: Redis Connectivity
Write-Host "2️⃣  Testing Redis (port 16379)..." -ForegroundColor Yellow
try {
    $redisPing = docker exec aibetting-redis redis-cli ping
    if ($redisPing -match "PONG") {
        Write-Host "   ✅ Redis PING successful" -ForegroundColor Green
        
        # Test set/get
        docker exec aibetting-redis redis-cli set "test:e2e" "success" | Out-Null
        $value = docker exec aibetting-redis redis-cli get "test:e2e"
        if ($value -match "success") {
            Write-Host "   ✅ Redis SET/GET successful" -ForegroundColor Green
        }
    }
} catch {
    Write-Host "   ❌ Redis connection failed" -ForegroundColor Red
    $allPassed = $false
}
Write-Host ""

# Test 3: PostgreSQL Connectivity
Write-Host "3️⃣  Testing PostgreSQL (port 15432)..." -ForegroundColor Yellow
try {
    $pgReady = docker exec aibetting-postgres pg_isready -U aibetting
    if ($pgReady -match "accepting connections") {
        Write-Host "   ✅ PostgreSQL is accepting connections" -ForegroundColor Green
        
        # Test database exists
        $dbExists = docker exec aibetting-postgres psql -U aibetting -d aibetting_accounting -c "\l" 2>&1
        if ($dbExists -match "aibetting_accounting") {
            Write-Host "   ✅ Database 'aibetting_accounting' exists" -ForegroundColor Green
        }
    }
} catch {
    Write-Host "   ❌ PostgreSQL connection failed" -ForegroundColor Red
    $allPassed = $false
}
Write-Host ""

# Test 4: Prometheus Targets
Write-Host "4️⃣  Testing Prometheus..." -ForegroundColor Yellow
try {
    $promHealth = docker exec aibetting-prometheus-v2 wget -qO- http://localhost:9090/-/healthy
    if ($promHealth -match "Healthy") {
        Write-Host "   ✅ Prometheus is healthy" -ForegroundColor Green
        Write-Host "   ℹ️  Check targets at: http://localhost:9090/targets" -ForegroundColor Cyan
    }
} catch {
    Write-Host "   ❌ Prometheus health check failed" -ForegroundColor Red
    $allPassed = $false
}
Write-Host ""

# Test 5: Grafana
Write-Host "5️⃣  Testing Grafana..." -ForegroundColor Yellow
try {
    $grafanaHealth = docker exec aibetting-grafana wget -qO- http://localhost:3000/api/health 2>$null
    if ($grafanaHealth -match "ok") {
        Write-Host "   ✅ Grafana is healthy" -ForegroundColor Green
        Write-Host "   ℹ️  Access Grafana at: http://localhost:3000 (admin/admin)" -ForegroundColor Cyan
    }
} catch {
    Write-Host "   ❌ Grafana health check failed" -ForegroundColor Red
    $allPassed = $false
}
Write-Host ""

# Test 6: Exporters
Write-Host "6️⃣  Testing Metric Exporters..." -ForegroundColor Yellow
try {
    # Redis Exporter
    $redisMetrics = Invoke-WebRequest -Uri "http://localhost:9122/metrics" -TimeoutSec 5 -UseBasicParsing
    if ($redisMetrics.StatusCode -eq 200) {
        Write-Host "   ✅ Redis Exporter responding (port 9122)" -ForegroundColor Green
    }
} catch {
    Write-Host "   ⚠️  Redis Exporter not responding" -ForegroundColor Yellow
}

try {
    # PostgreSQL Exporter
    $pgMetrics = Invoke-WebRequest -Uri "http://localhost:9187/metrics" -TimeoutSec 5 -UseBasicParsing
    if ($pgMetrics.StatusCode -eq 200) {
        Write-Host "   ✅ PostgreSQL Exporter responding (port 9187)" -ForegroundColor Green
    }
} catch {
    Write-Host "   ⚠️  PostgreSQL Exporter not responding" -ForegroundColor Yellow
}

try {
    # Node Exporter
    $nodeMetrics = Invoke-WebRequest -Uri "http://localhost:9100/metrics" -TimeoutSec 5 -UseBasicParsing
    if ($nodeMetrics.StatusCode -eq 200) {
        Write-Host "   ✅ Node Exporter responding (port 9100)" -ForegroundColor Green
    }
} catch {
    Write-Host "   ⚠️  Node Exporter not responding" -ForegroundColor Yellow
}
Write-Host ""

# Test 7: Configuration Files
Write-Host "7️⃣  Verifying Configuration Files..." -ForegroundColor Yellow
$configFiles = @(
    "AIBettingExplorer\appsettings.json",
    "AIBettingAnalyst\appsettings.json",
    "AIBettingExecutor\appsettings.json",
    "AIBettingAccounting\appsettings.json"
)

foreach ($file in $configFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        $hasRedis16379 = $content -match "16379"
        $hasPg15432 = $content -match "15432"
        
        if ($hasRedis16379 -or $hasPg15432) {
            Write-Host "   ✅ $file configured for Docker services" -ForegroundColor Green
        } else {
            Write-Host "   ℹ️  $file may not use Docker services" -ForegroundColor Cyan
        }
    }
}
Write-Host ""

# Final Summary
Write-Host "========================================" -ForegroundColor Cyan
if ($allPassed) {
    Write-Host "✅ ALL TESTS PASSED!" -ForegroundColor Green
} else {
    Write-Host "⚠️  SOME TESTS FAILED - Check above for details" -ForegroundColor Yellow
}
Write-Host "========================================`n" -ForegroundColor Cyan

Write-Host "📝 Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Start AIBettingExplorer:" -ForegroundColor White
Write-Host "     cd AIBettingExplorer; dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "  2. Start AIBettingAnalyst:" -ForegroundColor White
Write-Host "     cd AIBettingAnalyst; dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "  3. Start AIBettingExecutor:" -ForegroundColor White
Write-Host "     cd AIBettingExecutor; dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "  4. Check Prometheus targets:" -ForegroundColor White
Write-Host "     http://localhost:9090/targets" -ForegroundColor Cyan
Write-Host ""
Write-Host "  5. Access Grafana dashboards:" -ForegroundColor White
Write-Host "     http://localhost:3000 (admin/admin)" -ForegroundColor Cyan
Write-Host ""

Write-Host "📚 Documentation:" -ForegroundColor Yellow
Write-Host "  • Full setup guide: AIBettingExecutor\Grafana\STACK_RESTORATION_COMPLETE.md" -ForegroundColor Gray
Write-Host "  • Configuration changes: AIBettingExecutor\Grafana\CONFIGURATION_CHANGES.md" -ForegroundColor Gray
Write-Host ""
