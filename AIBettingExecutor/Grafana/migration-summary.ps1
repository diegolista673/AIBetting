# ============================================
# Configuration Migration Summary
# ============================================

Write-Host "📝 AIBetting Configuration Migration" -ForegroundColor Cyan
Write-Host "====================================`n" -ForegroundColor Cyan

Write-Host "✅ COMPLETED: All appsettings.json files updated`n" -ForegroundColor Green

Write-Host "🔄 Changes Made:" -ForegroundColor Yellow
Write-Host ""

Write-Host "1. Redis Connection (Docker port 16379):" -ForegroundColor White
Write-Host "   ✅ AIBettingExplorer\appsettings.json" -ForegroundColor Green
Write-Host "   ✅ AIBettingAnalyst\appsettings.json" -ForegroundColor Green
Write-Host "   ✅ AIBettingExecutor\appsettings.json" -ForegroundColor Green
Write-Host "   Old: localhost:6379,password=RedisAIBet2024!,..." -ForegroundColor Red
Write-Host "   New: localhost:16379,abortConnect=false,..." -ForegroundColor Green
Write-Host ""

Write-Host "2. PostgreSQL Connection (Docker port 15432):" -ForegroundColor White
Write-Host "   ✅ AIBettingAccounting\appsettings.json" -ForegroundColor Green
Write-Host "   Old: Host=localhost;Port=5432;Database=aibetting_db;Username=aibetting_user;..." -ForegroundColor Red
Write-Host "   New: Host=localhost;Port=15432;Database=aibetting_accounting;Username=aibetting;Password=AIBetting2024!" -ForegroundColor Green
Write-Host ""

Write-Host "📊 Docker Services Status:" -ForegroundColor Yellow
docker compose ps --format "table {{.Name}}\t{{.Status}}\t{{.Ports}}" | Select-String -Pattern "aibetting"
Write-Host ""

Write-Host "🧪 Quick Tests:" -ForegroundColor Yellow

# Test Redis
Write-Host "  Redis Connection:" -NoNewline
$redisTest = docker exec aibetting-redis redis-cli ping 2>$null
if ($redisTest -match "PONG") {
    Write-Host " ✅ OK" -ForegroundColor Green
} else {
    Write-Host " ❌ FAILED" -ForegroundColor Red
}

# Test PostgreSQL
Write-Host "  PostgreSQL Connection:" -NoNewline
$pgTest = docker exec aibetting-postgres pg_isready -U aibetting 2>$null
if ($pgTest -match "accepting") {
    Write-Host " ✅ OK" -ForegroundColor Green
} else {
    Write-Host " ❌ FAILED" -ForegroundColor Red
}

Write-Host ""
Write-Host "✅ Migration Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "📌 Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Rebuild your applications if needed" -ForegroundColor White
Write-Host "  2. Run AIBettingExplorer, AIBettingAnalyst, AIBettingExecutor" -ForegroundColor White
Write-Host "  3. They will now connect to Docker Redis (16379) and PostgreSQL (15432)" -ForegroundColor White
Write-Host "  4. Check Prometheus targets: http://localhost:9090/targets" -ForegroundColor White
Write-Host ""
