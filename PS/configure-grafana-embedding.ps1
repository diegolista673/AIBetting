# Grafana Configuration Script - Enable Embedding for Blazor Integration
# This script updates Grafana container to allow iframe embedding

Write-Host "Configuring Grafana for iframe embedding..." -ForegroundColor Cyan

# Stop Grafana container
Write-Host "Stopping Grafana container..." -ForegroundColor Yellow
docker stop aibetting_grafana 2>$null
docker rm aibetting_grafana 2>$null

# Update docker-compose.monitoring.yml to add embedding settings
Write-Host "Updating Grafana environment variables..." -ForegroundColor Yellow

$dockerComposeContent = Get-Content "docker-compose.monitoring.yml" -Raw

if ($dockerComposeContent -notlike "*GF_SECURITY_ALLOW_EMBEDDING*") {
    Write-Host "Adding GF_SECURITY_ALLOW_EMBEDDING=true..." -ForegroundColor Green
    
    # Add after GF_SERVER_ROOT_URL
    $dockerComposeContent = $dockerComposeContent -replace `
        "(- GF_SERVER_ROOT_URL=http://localhost:3000)", `
        "`$1`n      - GF_SECURITY_ALLOW_EMBEDDING=true`n      - GF_AUTH_ANONYMOUS_ENABLED=true`n      - GF_AUTH_ANONYMOUS_ORG_ROLE=Viewer"
    
    Set-Content "docker-compose.monitoring.yml" $dockerComposeContent
    Write-Host "Configuration updated!" -ForegroundColor Green
} else {
    Write-Host "Embedding already enabled!" -ForegroundColor Green
}

# Restart Grafana
Write-Host "`nRestarting Grafana with new configuration..." -ForegroundColor Yellow
docker-compose -f docker-compose.monitoring.yml up -d grafana

# Wait for Grafana to be ready
Write-Host "`nWaiting for Grafana to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Test Grafana
try {
    $health = Invoke-WebRequest -Uri "http://localhost:3000/api/health" -UseBasicParsing -TimeoutSec 5
    Write-Host "`n✅ Grafana is running and ready!" -ForegroundColor Green
    Write-Host "   URL: http://localhost:3000" -ForegroundColor White
    Write-Host "   Login: admin / admin" -ForegroundColor White
} catch {
    Write-Host "`n⚠️  Grafana might still be starting..." -ForegroundColor Yellow
    Write-Host "   Wait 30 seconds and try: http://localhost:3000" -ForegroundColor White
}

Write-Host "`n✅ Grafana configured for embedding!" -ForegroundColor Green
Write-Host "`nYou can now embed Grafana dashboards in Blazor!" -ForegroundColor Cyan
Write-Host "Start Blazor Dashboard and navigate to /monitoring`n" -ForegroundColor White
