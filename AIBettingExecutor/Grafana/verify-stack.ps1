# ============================================
# AIBetting Monitoring Stack Verification
# ============================================

Write-Host "🔍 Verifying AIBetting Monitoring Stack..." -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Check Docker Compose status
Write-Host "📦 Container Status:" -ForegroundColor Yellow
docker compose ps
Write-Host ""

# Health checks
Write-Host "🏥 Service Health Checks:" -ForegroundColor Yellow

# Prometheus
try {
    $promHealth = docker exec aibetting-prometheus-v2 wget -qO- http://localhost:9090/-/healthy 2>$null
    if ($promHealth -match "Healthy") {
        Write-Host "  ✅ Prometheus: HEALTHY" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Prometheus: UNHEALTHY" -ForegroundColor Red
    }
} catch {
    Write-Host "  ❌ Prometheus: ERROR" -ForegroundColor Red
}

# Grafana
try {
    $grafanaHealth = docker exec aibetting-grafana wget -qO- http://localhost:3000/api/health 2>$null
    if ($grafanaHealth -match "ok") {
        Write-Host "  ✅ Grafana: HEALTHY" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Grafana: UNHEALTHY" -ForegroundColor Red
    }
} catch {
    Write-Host "  ❌ Grafana: ERROR" -ForegroundColor Red
}

# Alertmanager
try {
    $amHealth = docker exec aibetting-alertmanager wget -qO- http://localhost:9093/-/healthy 2>$null
    if ($amHealth -match "OK") {
        Write-Host "  ✅ Alertmanager: HEALTHY" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Alertmanager: UNHEALTHY" -ForegroundColor Red
    }
} catch {
    Write-Host "  ❌ Alertmanager: ERROR" -ForegroundColor Red
}

# Redis
try {
    $redisHealth = docker exec aibetting-redis redis-cli ping 2>$null
    if ($redisHealth -match "PONG") {
        Write-Host "  ✅ Redis: HEALTHY" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Redis: UNHEALTHY" -ForegroundColor Red
    }
} catch {
    Write-Host "  ❌ Redis: ERROR" -ForegroundColor Red
}

# PostgreSQL
try {
    $pgHealth = docker exec aibetting-postgres pg_isready -U aibetting 2>$null
    if ($pgHealth -match "accepting connections") {
        Write-Host "  ✅ PostgreSQL: HEALTHY" -ForegroundColor Green
    } else {
        Write-Host "  ❌ PostgreSQL: UNHEALTHY" -ForegroundColor Red
    }
} catch {
    Write-Host "  ❌ PostgreSQL: ERROR" -ForegroundColor Red
}

Write-Host ""

# Service endpoints
Write-Host "🌐 Service Endpoints:" -ForegroundColor Yellow
Write-Host "  📊 Prometheus:         http://localhost:9090" -ForegroundColor Cyan
Write-Host "  📈 Grafana:            http://localhost:3000 (admin/admin)" -ForegroundColor Cyan
Write-Host "  🔔 Alertmanager:       http://localhost:9093" -ForegroundColor Cyan
Write-Host "  🗄️  Redis:             localhost:16379" -ForegroundColor Cyan
Write-Host "  🐘 PostgreSQL:         localhost:15432 (user: aibetting, db: aibetting_accounting)" -ForegroundColor Cyan
Write-Host "  📡 Redis Exporter:     http://localhost:9122/metrics" -ForegroundColor Cyan
Write-Host "  📡 Postgres Exporter:  http://localhost:9187/metrics" -ForegroundColor Cyan
Write-Host "  📡 Node Exporter:      http://localhost:9100/metrics" -ForegroundColor Cyan
Write-Host ""

# Volume information
Write-Host "💾 Persistent Volumes:" -ForegroundColor Yellow
docker volume ls | Select-String "grafana"
Write-Host ""

Write-Host "✅ Stack verification complete!" -ForegroundColor Green
Write-Host ""
Write-Host "📝 Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Open Grafana: http://localhost:3000" -ForegroundColor White
Write-Host "  2. Login with admin/admin" -ForegroundColor White
Write-Host "  3. Add Prometheus data source: http://prometheus:9090" -ForegroundColor White
Write-Host "  4. Check Prometheus targets: http://localhost:9090/targets" -ForegroundColor White
Write-Host "  5. Verify alerts: http://localhost:9090/alerts" -ForegroundColor White
Write-Host ""
