@echo off
echo.
echo ========================================
echo   Diagnostica Monitoring Stack
echo ========================================
echo.

echo [1/6] Verifico Explorer...
curl -s http://localhost:5001/metrics | findstr /C:"aibetting_price_updates_total" >nul
if %ERRORLEVEL% == 0 (
    echo [OK] Explorer metriche attive
    curl -s http://localhost:5001/metrics | findstr /C:"aibetting_price_updates_total"
) else (
    echo [ERROR] Metriche aibetting_* NON TROVATE
    echo [FIX] Riavvia Explorer: cd AIBettingExplorer ^&^& dotnet run
    goto :error
)

echo.
echo [2/6] Verifico Prometheus container...
docker ps | findstr /C:"prometheus" >nul
if %ERRORLEVEL% == 0 (
    echo [OK] Prometheus container attivo
) else (
    echo [ERROR] Prometheus non in esecuzione
    echo [FIX] docker-compose --profile monitoring up -d prometheus
    goto :error
)

echo.
echo [3/6] Verifico Prometheus target...
curl -s http://localhost:9090/api/v1/targets | findstr /C:"aibetting-explorer" >nul
if %ERRORLEVEL% == 0 (
    echo [OK] Target aibetting-explorer trovato
    curl -s "http://localhost:9090/api/v1/targets" | findstr /C:"health.*up"
    if %ERRORLEVEL% == 0 (
        echo [OK] Target status: UP
    ) else (
        echo [WARNING] Target potrebbe essere DOWN
    )
) else (
    echo [ERROR] Target aibetting-explorer non trovato
    echo [FIX] Verifica prometheus.yml
    goto :error
)

echo.
echo [4/6] Verifico dati in Prometheus...
curl -s "http://localhost:9090/api/v1/query?query=aibetting_price_updates_total" | findstr /C:"result" >nul
if %ERRORLEVEL% == 0 (
    echo [OK] Prometheus ha dati aibetting_price_updates_total
) else (
    echo [ERROR] Nessun dato in Prometheus
    echo [FIX] Aspetta 30 secondi che Prometheus faccia scraping
    goto :error
)

echo.
echo [5/6] Verifico Grafana container...
docker ps | findstr /C:"grafana" >nul
if %ERRORLEVEL% == 0 (
    echo [OK] Grafana container attivo
) else (
    echo [ERROR] Grafana non in esecuzione
    echo [FIX] docker-compose --profile monitoring up -d grafana
    goto :error
)

echo.
echo [6/6] Verifico dashboard in Grafana...
curl -s -u admin:admin http://localhost:3000/api/search?type=dash-db | findstr /C:"aibetting-explorer" >nul
if %ERRORLEVEL% == 0 (
    echo [OK] Dashboard aibetting-explorer trovata
) else (
    echo [ERROR] Dashboard NON importata in Grafana
    echo [FIX] Importa grafana-dashboard-explorer.json
    echo        1. Apri http://localhost:3000
    echo        2. Dashboards -^> Import
    echo        3. Upload grafana-dashboard-explorer.json
    goto :error
)

echo.
echo ========================================
echo   Tutti i test passati!
echo ========================================
echo.
echo Verifica finale:
echo 1. Apri: http://localhost:3000/d/aibetting-explorer
echo 2. Dovresti vedere 6 panels con grafici
echo 3. Se vedi "No data", aspetta 10-20 secondi
echo.
echo Se dashboard ancora vuota:
echo - Apri panel -^> Edit
echo - Verifica Data Source = Prometheus
echo - Verifica Query = aibetting_price_updates_total
echo.
pause
goto :eof

:error
echo.
echo ========================================
echo   Test FALLITO - Fix richiesto
echo ========================================
echo.
pause
exit /b 1
