@echo off
REM ============================================
REM AIBetting Monitoring Stack - Complete Reset
REM ============================================

echo.
echo ╔════════════════════════════════════════════════════╗
echo ║  AIBetting Monitoring Stack - Complete Reset     ║
echo ╚════════════════════════════════════════════════════╝
echo.

cd /d "%~dp0"

echo 📂 Working directory: %CD%
echo.

REM ============================================
REM STEP 1: Stop All Containers
REM ============================================
echo 🛑 STEP 1: Stopping all AIBetting containers...
echo ----------------------------------------

docker-compose down -v --remove-orphans >nul 2>&1
docker stop aibetting-grafana aibetting-prometheus aibetting-alertmanager aibetting-redis-exporter aibetting-node-exporter aibetting_redis aibetting_postgres aibetting_redis_exporter aibetting_postgres_exporter aibetting_redis_insight aibetting_pgadmin >nul 2>&1

echo ✅ Containers stopped
timeout /t 2 >nul

REM ============================================
REM STEP 2: Remove All Containers
REM ============================================
echo.
echo 🗑️  STEP 2: Removing all AIBetting containers...
echo ----------------------------------------

docker rm -f aibetting-grafana aibetting-prometheus aibetting-alertmanager aibetting-redis-exporter aibetting-node-exporter aibetting_redis aibetting_postgres aibetting_redis_exporter aibetting_postgres_exporter aibetting_redis_insight aibetting_pgadmin >nul 2>&1

echo ✅ Containers removed

REM ============================================
REM STEP 3: Clean Networks
REM ============================================
echo.
echo 🌐 STEP 3: Cleaning networks...
echo ----------------------------------------

docker network rm aibetting-monitoring aibetting-monitoring-v2 aibetting_default >nul 2>&1

echo ✅ Networks cleaned

REM ============================================
REM STEP 4: System Cleanup
REM ============================================
echo.
echo 🧹 STEP 4: System cleanup...
echo ----------------------------------------

docker system prune -f >nul 2>&1

echo ✅ Cleanup complete
timeout /t 2 >nul

REM ============================================
REM STEP 5: Verify Cleanup
REM ============================================
echo.
echo 🔍 STEP 5: Verifying cleanup...
echo ----------------------------------------

docker ps -a | findstr /i "aibetting" >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo ⚠️  WARNING: Some containers still exist
    docker ps -a | findstr /i "aibetting"
    echo.
    echo Try running this script as Administrator
    pause
    exit /b 1
) else (
    echo ✅ All containers successfully removed
)

REM ============================================
REM STEP 6: Start Fresh Stack
REM ============================================
echo.
echo 🚀 STEP 6: Starting fresh monitoring stack...
echo ----------------------------------------

if not exist "docker-compose.yml" (
    echo ❌ ERROR: docker-compose.yml not found!
    echo    Make sure you're in the correct directory.
    pause
    exit /b 1
)

echo Starting services...
docker-compose up -d

echo Waiting for services to start...
timeout /t 8 >nul

REM ============================================
REM STEP 7: Status Check
REM ============================================
echo.
echo 📊 STEP 7: Container status...
echo ----------------------------------------

docker-compose ps

REM ============================================
REM STEP 8: Health Checks
REM ============================================
echo.
echo 🏥 STEP 8: Health checks...
echo ----------------------------------------

curl -s http://localhost:9090/-/healthy >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo   ✅ Prometheus is healthy
) else (
    echo   ❌ Prometheus is not responding
)

curl -s http://localhost:3000/api/health >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo   ✅ Grafana is healthy
) else (
    echo   ❌ Grafana is not responding
)

curl -s http://localhost:9093/-/healthy >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo   ✅ Alertmanager is healthy
) else (
    echo   ❌ Alertmanager is not responding
)

REM ============================================
REM Final Summary
REM ============================================
echo.
echo ╔════════════════════════════════════════════════════╗
echo ║            🎉 Setup Complete!                    ║
echo ╚════════════════════════════════════════════════════╝
echo.

echo 🌐 Access URLs:
echo   📊 Grafana:       http://localhost:3000
echo      Login:         admin / admin
echo.
echo   📈 Prometheus:    http://localhost:9090
echo      Targets:       http://localhost:9090/targets
echo.
echo   🔔 Alertmanager:  http://localhost:9093
echo.

echo 📝 Next Steps:
echo   1. Open Grafana (http://localhost:3000)
echo   2. Add Prometheus data source
echo   3. Import dashboard from: executor-dashboard.json
echo   4. Start AIBettingExecutor: dotnet run
echo.

echo ✨ Monitoring stack is ready!
echo.

pause
