# ✅ Grafana Dashboard Analyst - Soluzione Completa

## 🎯 **File Creati**

1. **`GRAFANA-TROUBLESHOOTING.md`** - Guida completa troubleshooting (7 cause comuni + fix)
2. **`fix-grafana-analyst.ps1`** - Script automatico diagnosi e repair

---

## 🚀 **Quick Start - Risolvi Problema**

### **Metodo 1: Script Automatico (Consigliato)**

```powershell
# Esegui script auto-diagnosi
.\fix-grafana-analyst.ps1
```

Lo script:
- ✅ Verifica 7 punti critici
- ✅ Identifica problemi automaticamente
- ✅ Suggerisce fix specifici
- ✅ Opzione per fix automatici
- ✅ Apre dashboard se tutto OK

---

### **Metodo 2: Verifica Manuale Rapida**

```powershell
# 1. Check Analyst attivo
Get-Process -Name "AIBettingAnalyst"

# 2. Check metriche
curl http://localhost:5002/metrics | Select-String "aibetting_analyst_snapshots"

# 3. Check Prometheus target
start http://localhost:9090/targets
# Cerca: aibetting-analyst (deve essere UP)

# 4. Check Prometheus dati
start http://localhost:9090/graph
# Query: aibetting_analyst_snapshots_processed_total

# 5. Apri dashboard
start http://localhost:3000/d/aibetting-analyst
```

---

## 🔧 **7 Cause Comuni e Fix Rapidi**

### **1. Dashboard Non Esiste**
```powershell
# Fix: Import via UI
# 1. http://localhost:3000/dashboards
# 2. New → Import
# 3. Upload grafana-dashboard-analyst.json
```

### **2. Data Source Mancante**
```powershell
# Fix: Crea via UI
# 1. http://localhost:3000/datasources
# 2. Add data source → Prometheus
# 3. URL: http://prometheus:9090
# 4. Save & test
```

### **3. Prometheus Target Non Configurato**
```powershell
# Fix: Verifica prometheus.yml e riavvia
docker restart aibetting-prometheus
```

### **4. Analyst Non Attivo**
```powershell
# Fix: Avvia Analyst
cd AIBettingAnalyst
dotnet run
```

### **5. Metriche Non Incrementano**
```powershell
# Fix: Verifica Explorer attivo
Get-Process -Name "AIBettingExplorer"

# Se non attivo:
cd AIBettingExplorer
dotnet run
```

### **6. Time Range Errato**
```
# Fix: Nella dashboard
1. Click time picker (alto destra)
2. Seleziona "Last 15 minutes"
3. Click "Apply"
```

### **7. Query Syntax Error**
```
# Fix: Verifica query corrette in panels
aibetting_analyst_snapshots_processed_total
aibetting_analyst_surebets_found_total
```

---

## 📊 **Verifica Finale**

Dopo aver applicato i fix, verifica che tutto funzioni:

```powershell
# Test completo
.\fix-grafana-analyst.ps1

# Oppure test manuale
Write-Host "1. Analyst:" (Get-Process -Name "AIBettingAnalyst" -EA SilentlyContinue ? "✅" : "❌")
Write-Host "2. Metriche:" (Test-NetConnection localhost -Port 5002 -InformationLevel Quiet ? "✅" : "❌")
Write-Host "3. Prometheus:" ((Invoke-RestMethod "http://localhost:9090/api/v1/targets").data.activeTargets | Where { $_.labels.job -eq "aibetting-analyst" } ? "✅" : "❌")
Write-Host "4. Grafana:" ((Invoke-RestMethod "http://localhost:3000/api/search?type=dash-db" -Headers @{Authorization="Basic YWRtaW46YWRtaW4="} | Where { $_.uid -eq "aibetting-analyst" }) ? "✅" : "❌")
```

---

## 🎯 **Checklist Completa**

Verifica tutti questi punti:

- [ ] **Analyst**: Processo attivo
- [ ] **Port 5002**: Risponde
- [ ] **Metriche**: Disponibili su `/metrics`
- [ ] **Metriche**: Incrementano (non sempre 0)
- [ ] **Prometheus**: Target "aibetting-analyst" configurato
- [ ] **Prometheus**: Target status "UP"
- [ ] **Prometheus**: Ha dati (query manuale OK)
- [ ] **Grafana**: Data source "Prometheus" configurato
- [ ] **Grafana**: Data source test OK
- [ ] **Grafana**: Dashboard "aibetting-analyst" esiste
- [ ] **Dashboard**: Panels hanno query corrette
- [ ] **Dashboard**: Time range corretto (Last 15 min)
- [ ] **End-to-end**: Query via Grafana funziona

---

## 📞 **Link Rapidi**

| Risorsa | URL |
|---------|-----|
| **Dashboard Analyst** | http://localhost:3000/d/aibetting-analyst |
| Grafana Dashboards | http://localhost:3000/dashboards |
| Grafana Data Sources | http://localhost:3000/datasources |
| Prometheus Targets | http://localhost:9090/targets |
| Prometheus Query | http://localhost:9090/graph |
| Analyst Metrics | http://localhost:5002/metrics |
| Explorer Metrics | http://localhost:5001/metrics |

---

## 🆘 **Fix Emergenza - Restart Completo**

Se nulla funziona, restart completo dello stack:

```powershell
# 1. Stop tutti i processi
Get-Process | Where-Object { $_.ProcessName -like "*AIBetting*" } | Stop-Process -Force

# 2. Restart Docker
docker restart aibetting-prometheus aibetting-grafana aibetting-redis

# 3. Attendi 15 secondi
Start-Sleep -Seconds 15

# 4. Avvia Explorer
cd C:\Users\SMARTW\source\repos\AIBettingSolution\AIBettingExplorer
Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run" -WindowStyle Minimized

# 5. Attendi 10 secondi
Start-Sleep -Seconds 10

# 6. Avvia Analyst
cd C:\Users\SMARTW\source\repos\AIBettingSolution\AIBettingAnalyst
Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run"

# 7. Attendi 15 secondi
Start-Sleep -Seconds 15

# 8. Verifica
.\fix-grafana-analyst.ps1

# 9. Apri dashboard
start http://localhost:3000/d/aibetting-analyst
```

---

## 📚 **Documentazione Completa**

- **`GRAFANA-TROUBLESHOOTING.md`**: Guida dettagliata con tutte le soluzioni
- **`fix-grafana-analyst.ps1`**: Script auto-diagnosi e repair
- **`CONTROLLO-FINALE-ANALYST.md`**: Report stato sistema
- **`GRAFANA-ANALYST-FIX.md`**: Fix applicati precedentemente

---

## ✅ **Stato Atteso Finale**

Dopo aver applicato tutti i fix:

```
╔═══════════════════════════════════════════════╗
║     GRAFANA ANALYST DASHBOARD - OK ✅        ║
╠═══════════════════════════════════════════════╣
║  ✅ Analyst: RUNNING                         ║
║  ✅ Metriche: 4500+ snapshots                ║
║  ✅ Prometheus Target: UP                    ║
║  ✅ Prometheus Data: Available               ║
║  ✅ Grafana Data Source: OK                  ║
║  ✅ Dashboard: Visible                       ║
║  ✅ Panels: Showing data                     ║
╠═══════════════════════════════════════════════╣
║  🎉 DASHBOARD FUNZIONANTE!                   ║
╚═══════════════════════════════════════════════╝
```

**Dashboard URL:** http://localhost:3000/d/aibetting-analyst

---

**Creato:** 2026-01-12  
**Status:** Soluzione Completa  
**Tools:** 2 file (guida + script)
