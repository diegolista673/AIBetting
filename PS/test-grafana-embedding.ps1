# Test Grafana Embedding Configuration
Write-Host "`n🔍 Testing Grafana Embedding Configuration`n" -ForegroundColor Cyan

# Test 1: Verifica container Grafana
Write-Host "1. Checking Grafana container..." -ForegroundColor Yellow
$container = docker ps --filter "name=grafana" --format "{{.Names}}: {{.Status}}"
if ($container) {
    Write-Host "   ✅ $container" -ForegroundColor Green
} else {
    Write-Host "   ❌ Grafana container not running" -ForegroundColor Red
    exit 1
}

# Test 2: Verifica variabili ambiente
Write-Host "`n2. Checking environment variables..." -ForegroundColor Yellow
$allowEmbedding = docker exec aibetting-grafana env | Select-String "GF_SECURITY_ALLOW_EMBEDDING=true"
$anonymousEnabled = docker exec aibetting-grafana env | Select-String "GF_AUTH_ANONYMOUS_ENABLED=true"

if ($allowEmbedding) {
    Write-Host "   ✅ GF_SECURITY_ALLOW_EMBEDDING=true" -ForegroundColor Green
} else {
    Write-Host "   ❌ GF_SECURITY_ALLOW_EMBEDDING not set" -ForegroundColor Red
}

if ($anonymousEnabled) {
    Write-Host "   ✅ GF_AUTH_ANONYMOUS_ENABLED=true" -ForegroundColor Green
} else {
    Write-Host "   ❌ GF_AUTH_ANONYMOUS_ENABLED not set" -ForegroundColor Red
}

# Test 3: Verifica HTTP headers
Write-Host "`n3. Checking HTTP headers..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:3000" -Method HEAD -UseBasicParsing -TimeoutSec 5
    
    $xFrameOptions = $response.Headers.'X-Frame-Options'
    if ($xFrameOptions) {
        Write-Host "   ⚠️  X-Frame-Options: $xFrameOptions (Should be empty or SAMEORIGIN)" -ForegroundColor Yellow
    } else {
        Write-Host "   ✅ X-Frame-Options: Not set (Embedding allowed!)" -ForegroundColor Green
    }
} catch {
    Write-Host "   ❌ Cannot connect to Grafana" -ForegroundColor Red
}

# Test 4: Test accesso anonimo
Write-Host "`n4. Testing anonymous access..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:3000/api/health" -UseBasicParsing -TimeoutSec 5
    $health = $response.Content | ConvertFrom-Json
    Write-Host "   ✅ Grafana is responding - Version: $($health.version)" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Cannot access Grafana API" -ForegroundColor Red
}

# Test 5: Verifica dashboard disponibili
Write-Host "`n5. Checking dashboards..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:3000/api/search?type=dash-db" -UseBasicParsing -TimeoutSec 5
    $dashboards = $response.Content | ConvertFrom-Json
    
    if ($dashboards.Count -gt 0) {
        Write-Host "   ✅ Found $($dashboards.Count) dashboard(s):" -ForegroundColor Green
        foreach ($dash in $dashboards) {
            Write-Host "      - $($dash.title) (UID: $($dash.uid))" -ForegroundColor Gray
        }
    } else {
        Write-Host "   ⚠️  No dashboards found. Import grafana-dashboard-explorer.json" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ⚠️  Cannot fetch dashboards (might need authentication)" -ForegroundColor Yellow
}

# Summary
Write-Host "`n" -NoNewline
Write-Host "=" * 60 -ForegroundColor Cyan
Write-Host "EMBEDDING TEST SUMMARY" -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Cyan

if ($allowEmbedding -and $anonymousEnabled) {
    Write-Host "`n✅ Grafana is READY for iframe embedding!" -ForegroundColor Green
    Write-Host "`nYou can now:" -ForegroundColor Cyan
    Write-Host "  1. Start Blazor Dashboard: cd AIBettingBlazorDashboard; dotnet run" -ForegroundColor White
    Write-Host "  2. Navigate to: http://localhost:5000/monitoring" -ForegroundColor White
    Write-Host "  3. Dashboard should load in iframe without errors`n" -ForegroundColor White
    
    # Open browser to test
    $openBrowser = Read-Host "Open Blazor Dashboard in browser? (y/n)"
    if ($openBrowser -eq "y") {
        Start-Process "http://localhost:5000/monitoring"
    }
} else {
    Write-Host "`n⚠️  Grafana embedding not fully configured" -ForegroundColor Yellow
    Write-Host "`nRun: docker-compose -f docker-compose.monitoring.yml restart grafana`n" -ForegroundColor White
}

Write-Host "=" * 60 -ForegroundColor Cyan
