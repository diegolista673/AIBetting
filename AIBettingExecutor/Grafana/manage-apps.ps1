# ============================================
# AIBetting Process Manager
# ============================================

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("start", "stop", "restart", "status")]
    [string]$Action = "status"
)

$apps = @(
    @{Name="AIBettingExplorer"; Path="AIBettingExplorer"; Port=5001},
    @{Name="AIBettingAnalyst"; Path="AIBettingAnalyst"; Port=5002},
    @{Name="AIBettingExecutor"; Path="AIBettingExecutor"; Port=5003}
)

function Get-AppStatus {
    Write-Host "AIBetting Applications Status" -ForegroundColor Cyan
    Write-Host "=============================" -ForegroundColor Cyan
    Write-Host ""
    
    foreach ($app in $apps) {
        $process = Get-Process -Name $app.Name -ErrorAction SilentlyContinue
        $portInUse = Test-NetConnection -ComputerName localhost -Port $app.Port -WarningAction SilentlyContinue -InformationLevel Quiet
        
        Write-Host "$($app.Name):" -ForegroundColor Yellow
        if ($process) {
            Write-Host "  Process:  RUNNING (PID: $($process.Id))" -ForegroundColor Green
            Write-Host "  Port:     $($app.Port) $(if($portInUse){'LISTENING'}else{'NOT LISTENING'})" -ForegroundColor $(if($portInUse){'Green'}else{'Red'})
            Write-Host "  Memory:   $([math]::Round($process.WorkingSet64/1MB, 2)) MB" -ForegroundColor Gray
            Write-Host "  CPU:      $([math]::Round($process.CPU, 2))s" -ForegroundColor Gray
        } else {
            Write-Host "  Process:  NOT RUNNING" -ForegroundColor Red
            Write-Host "  Port:     $($app.Port) $(if($portInUse){'OCCUPIED (conflict!)'}else{'FREE'})" -ForegroundColor $(if($portInUse){'Yellow'}else{'Gray'})
        }
        Write-Host ""
    }
}

function Stop-Apps {
    Write-Host "Stopping AIBetting Applications..." -ForegroundColor Yellow
    
    foreach ($app in $apps) {
        $process = Get-Process -Name $app.Name -ErrorAction SilentlyContinue
        if ($process) {
            Write-Host "  Stopping $($app.Name) (PID: $($process.Id))..." -NoNewline
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            Start-Sleep -Milliseconds 500
            Write-Host " STOPPED" -ForegroundColor Green
        } else {
            Write-Host "  $($app.Name): Already stopped" -ForegroundColor Gray
        }
    }
    Write-Host ""
}

function Start-Apps {
    Write-Host "Starting AIBetting Applications..." -ForegroundColor Yellow
    Write-Host ""
    
    # Stop any running instances first
    Stop-Apps
    
    foreach ($app in $apps) {
        Write-Host "Starting $($app.Name) on port $($app.Port)..." -ForegroundColor Cyan
        
        $projectPath = Join-Path $PSScriptRoot "..\$($app.Path)"
        
        if (-not (Test-Path $projectPath)) {
            Write-Host "  ERROR: Project directory not found at $projectPath" -ForegroundColor Red
            continue
        }
        
        # Start in new PowerShell window
        $startArgs = @(
            "-NoExit",
            "-Command",
            "cd '$projectPath'; Write-Host 'Starting $($app.Name)...' -ForegroundColor Green; dotnet run"
        )
        
        Start-Process powershell -ArgumentList $startArgs -WindowStyle Normal
        Write-Host "  Started in new window (port $($app.Port))" -ForegroundColor Green
        Start-Sleep -Seconds 1
    }
    
    Write-Host ""
    Write-Host "Waiting 5 seconds for apps to initialize..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5
    
    Write-Host ""
    Get-AppStatus
}

function Restart-Apps {
    Write-Host "Restarting AIBetting Applications..." -ForegroundColor Yellow
    Write-Host ""
    Stop-Apps
    Start-Sleep -Seconds 2
    Start-Apps
}

# Main execution
Write-Host ""
switch ($Action) {
    "status" {
        Get-AppStatus
        Write-Host "Usage: .\manage-apps.ps1 [-Action start|stop|restart|status]" -ForegroundColor Gray
    }
    "start" {
        Start-Apps
    }
    "stop" {
        Stop-Apps
        Get-AppStatus
    }
    "restart" {
        Restart-Apps
    }
}

Write-Host ""
Write-Host "Prometheus Targets: http://localhost:9090/targets" -ForegroundColor Cyan
Write-Host "Grafana Dashboards: http://localhost:3000" -ForegroundColor Cyan
Write-Host ""
