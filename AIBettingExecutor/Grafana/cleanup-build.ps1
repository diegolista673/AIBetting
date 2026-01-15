# ============================================
# AIBetting Build Cleanup & Fix
# ============================================

Write-Host "AIBetting Build Cleanup" -ForegroundColor Cyan
Write-Host "=======================" -ForegroundColor Cyan
Write-Host ""

$projects = @("AIBettingExplorer", "AIBettingAnalyst", "AIBettingExecutor")

# Step 1: Stop all processes
Write-Host "1. Stopping all AIBetting processes..." -ForegroundColor Yellow
foreach ($project in $projects) {
    $process = Get-Process -Name $project -ErrorAction SilentlyContinue
    if ($process) {
        Write-Host "   Stopping $project (PID $($process.Id))..." -NoNewline
        Stop-Process -Id $process.Id -Force
        Start-Sleep -Milliseconds 500
        Write-Host " DONE" -ForegroundColor Green
    } else {
        Write-Host "   $project - Not running" -ForegroundColor Gray
    }
}

Start-Sleep -Seconds 2

# Step 2: Clean bin/obj folders
Write-Host ""
Write-Host "2. Cleaning bin and obj folders..." -ForegroundColor Yellow
foreach ($project in $projects) {
    $projectPath = Join-Path $PSScriptRoot "..\$project"
    
    if (Test-Path $projectPath) {
        Write-Host "   Cleaning $project..." -NoNewline
        
        $binPath = Join-Path $projectPath "bin"
        $objPath = Join-Path $projectPath "obj"
        
        if (Test-Path $binPath) {
            Remove-Item $binPath -Recurse -Force -ErrorAction SilentlyContinue
        }
        if (Test-Path $objPath) {
            Remove-Item $objPath -Recurse -Force -ErrorAction SilentlyContinue
        }
        
        Write-Host " DONE" -ForegroundColor Green
    }
}

# Step 3: Verify no locked files
Write-Host ""
Write-Host "3. Verifying no locked processes..." -ForegroundColor Yellow
foreach ($project in $projects) {
    $process = Get-Process -Name $project -ErrorAction SilentlyContinue
    if ($process) {
        Write-Host "   WARNING - $project still running (PID $($process.Id))" -ForegroundColor Red
        Write-Host "   Force killing..." -NoNewline
        Stop-Process -Id $process.Id -Force
        Write-Host " DONE" -ForegroundColor Green
    } else {
        Write-Host "   $project - Clean" -ForegroundColor Green
    }
}

# Step 4: Check ports availability
Write-Host ""
Write-Host "4. Checking ports availability..." -ForegroundColor Yellow
$ports = @(5001, 5002, 5003)
foreach ($port in $ports) {
    $connection = Test-NetConnection -ComputerName localhost -Port $port -WarningAction SilentlyContinue -InformationLevel Quiet
    if ($connection) {
        Write-Host "   Port $port - IN USE (may cause issues)" -ForegroundColor Yellow
    } else {
        Write-Host "   Port $port - FREE" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "=======================" -ForegroundColor Green
Write-Host "Cleanup Complete!" -ForegroundColor Green
Write-Host "=======================" -ForegroundColor Green
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Run 'dotnet build' in each project directory" -ForegroundColor White
Write-Host "  2. Or use '.\manage-apps.ps1 -Action start'" -ForegroundColor White
Write-Host ""
Write-Host "If build still fails:" -ForegroundColor Yellow
Write-Host "  - Close Visual Studio" -ForegroundColor White
Write-Host "  - Run this script again" -ForegroundColor White
Write-Host "  - Restart Visual Studio" -ForegroundColor White
Write-Host ""
