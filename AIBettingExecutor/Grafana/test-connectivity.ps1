# ============================================
# Test Redis Connectivity
# ============================================

Write-Host "🔍 Testing Redis Connectivity..." -ForegroundColor Cyan
Write-Host "=================================`n" -ForegroundColor Cyan

# Test Docker Redis (port 16379)
Write-Host "📍 Testing Docker Redis (localhost:16379)..." -ForegroundColor Yellow
try {
    $result = docker exec aibetting-redis redis-cli ping
    if ($result -match "PONG") {
        Write-Host "  ✅ Docker Redis: CONNECTED" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Docker Redis: NO RESPONSE" -ForegroundColor Red
    }
} catch {
    Write-Host "  ❌ Docker Redis: ERROR - $($_.Exception.Message)" -ForegroundColor Red
}

# Test if local Redis is running (port 6379)
Write-Host "`n📍 Checking for local Redis (localhost:6379)..." -ForegroundColor Yellow
try {
    $tcpClient = New-Object System.Net.Sockets.TcpClient
    $tcpClient.Connect("localhost", 6379)
    $tcpClient.Close()
    Write-Host "  ⚠️  Local Redis detected on port 6379" -ForegroundColor Yellow
    Write-Host "     Your applications are now configured to use Docker Redis on port 16379" -ForegroundColor Yellow
} catch {
    Write-Host "  ✅ No local Redis detected (port 6379 free)" -ForegroundColor Green
}

# Test Docker Redis port accessibility
Write-Host "`n📍 Testing Docker Redis port accessibility (localhost:16379)..." -ForegroundColor Yellow
try {
    $tcpClient = New-Object System.Net.Sockets.TcpClient
    $tcpClient.Connect("localhost", 16379)
    $tcpClient.Close()
    Write-Host "  ✅ Port 16379: ACCESSIBLE" -ForegroundColor Green
} catch {
    Write-Host "  ❌ Port 16379: NOT ACCESSIBLE" -ForegroundColor Red
    Write-Host "     Make sure Docker Redis container is running:" -ForegroundColor Yellow
    Write-Host "     cd AIBettingExecutor\Grafana; docker compose up -d" -ForegroundColor White
}

# Show Redis info
Write-Host "`n📊 Docker Redis Information:" -ForegroundColor Yellow
try {
    $info = docker exec aibetting-redis redis-cli info server
    $version = ($info | Select-String "redis_version").ToString()
    $uptime = ($info | Select-String "uptime_in_seconds").ToString()
    Write-Host "  $version" -ForegroundColor Cyan
    Write-Host "  $uptime" -ForegroundColor Cyan
} catch {
    Write-Host "  ❌ Could not retrieve Redis info" -ForegroundColor Red
}

# Test PostgreSQL connectivity
Write-Host "`n📍 Testing Docker PostgreSQL (localhost:15432)..." -ForegroundColor Yellow
try {
    $result = docker exec aibetting-postgres pg_isready -U aibetting
    if ($result -match "accepting connections") {
        Write-Host "  ✅ Docker PostgreSQL: CONNECTED" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Docker PostgreSQL: NO RESPONSE" -ForegroundColor Red
    }
} catch {
    Write-Host "  ❌ Docker PostgreSQL: ERROR - $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n✅ Connectivity test complete!" -ForegroundColor Green
Write-Host "`n📝 Configuration Summary:" -ForegroundColor Yellow
Write-Host "  Redis Connection String:      localhost:16379" -ForegroundColor White
Write-Host "  PostgreSQL Connection String: Host=localhost;Port=15432;Database=aibetting_accounting;Username=aibetting;Password=AIBetting2024!" -ForegroundColor White
Write-Host "`n  All appsettings.json files have been updated to use these Docker services." -ForegroundColor Cyan
Write-Host ""
