# Restart Explorer with Metrics
Write-Host "`nRestarting AIBettingExplorer with Prometheus metrics...`n" -ForegroundColor Cyan

# Stop existing Explorer process
Write-Host "[1/3] Stopping existing Explorer..." -ForegroundColor Yellow
$existingProcess = Get-Process -Name "AIBettingExplorer" -ErrorAction SilentlyContinue
if ($existingProcess) {
    Stop-Process -Name "AIBettingExplorer" -Force
    Start-Sleep -Seconds 2
    Write-Host "  OK Explorer stopped" -ForegroundColor Green
} else {
    Write-Host "  INFO No Explorer process running" -ForegroundColor Cyan
}

# Build latest code
Write-Host "`n[2/3] Building latest code..." -ForegroundColor Yellow
Push-Location "AIBettingExplorer"
try {
    $buildOutput = dotnet build --configuration Release 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  OK Build successful" -ForegroundColor Green
    } else {
        Write-Host "  ERROR Build failed" -ForegroundColor Red
        Write-Host $buildOutput
        Pop-Location
        exit 1
    }
} finally {
    Pop-Location
}

# Start Explorer in background
Write-Host "`n[3/3] Starting Explorer..." -ForegroundColor Yellow
Write-Host "  Starting in background (check logs for output)" -ForegroundColor Cyan

Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd AIBettingExplorer; dotnet run" -WindowStyle Minimized

Write-Host "`nWaiting for Explorer to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Verify metrics
Write-Host "`nVerifying metrics endpoint..." -ForegroundColor Yellow
$attempts = 0
$maxAttempts = 6
$success = $false

while ($attempts -lt $maxAttempts -and -not $success) {
    try {
        $metrics = Invoke-WebRequest -Uri "http://localhost:5001/metrics" -UseBasicParsing -TimeoutSec 2
        if ($metrics.Content -match "aibetting_price_updates_total") {
            Write-Host "  OK Metrics endpoint is working!" -ForegroundColor Green
            $success = $true
            
            # Show current value
            if ($metrics.Content -match "aibetting_price_updates_total\s+(\d+)") {
                Write-Host "  Current updates: $($matches[1])" -ForegroundColor Gray
            }
        } else {
            throw "Metrics not found"
        }
    } catch {
        $attempts++
        if ($attempts -lt $maxAttempts) {
            Write-Host "  Waiting... (attempt $attempts/$maxAttempts)" -ForegroundColor Yellow
            Start-Sleep -Seconds 5
        }
    }
}

if (-not $success) {
    Write-Host "`n  ERROR Explorer metrics not available after $maxAttempts attempts" -ForegroundColor Red
    Write-Host "  Check Explorer logs for errors" -ForegroundColor White
    exit 1
}

Write-Host "`n" -NoNewline
Write-Host "========================================" -ForegroundColor Green
Write-Host " Explorer Running Successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Metrics endpoint: http://localhost:5001/metrics" -ForegroundColor White
Write-Host "Logs directory: AIBettingExplorer\logs\" -ForegroundColor White
Write-Host ""
Write-Host "Next: Run setup-complete-monitoring.ps1 to import dashboard`n" -ForegroundColor Cyan
