# ⚡ SOLUZIONE RAPIDA - Dashboard Vuota in Blazor

## 🎯 Problema
Grafana si apre nell'iframe Blazor ma **non visualizza dati/grafici**.

---

## ✅ Soluzione in 3 Comandi (3 minuti)

### **Comando 1: Riavvia Explorer**
```powershell
cd AIBettingExplorer
dotnet run
```

**Aspetta 10 secondi** e verifica metriche:
```powershell
curl http://localhost:5001/metrics | Select-String "aibetting_price_updates"
```

**✅ Output atteso:**
```
aibetting_price_updates_total 25
```

**❌ Se non vedi questo:** Explorer non sta generando metriche. Controlla errori nella console.

---

### **Comando 2: Importa Dashboard in Grafana**

#### **Opzione A: Script Automatico** (Consigliato)
```powershell
powershell -ExecutionPolicy Bypass -File setup-complete-monitoring.ps1
```

#### **Opzione B: Manuale** (2 minuti)
1. Apri: http://localhost:3000 (admin/admin)
2. Click: **Dashboards** → **Import**
3. Click: **Upload JSON file**
4. Seleziona: `grafana-dashboard-explorer.json`
5. **UID:** `aibetting-explorer` (non cambiare!)
6. **Data Source:** Seleziona "Prometheus"
7. Click: **Import**

---

### **Comando 3: Apri Blazor Dashboard**
```powershell
start http://localhost:5000/monitoring
```

Dovresti vedere i grafici che si aggiornano ogni 5 secondi!

---

## 🔍 Verifica Rapida (30 secondi)

### 1. Explorer Funziona?
```powershell
curl http://localhost:5001/metrics | Select-String "aibetting_price_updates_total"
```
✅ Deve restituire: `aibetting_price_updates_total <NUMERO>`

### 2. Prometheus Riceve Dati?
Apri: http://localhost:9090/targets  
Cerca: `aibetting-explorer`  
✅ Deve essere: **UP** (verde)

### 3. Dashboard Importata?
Apri: http://localhost:3000/d/aibetting-explorer  
✅ Deve mostrare: Dashboard con 6 panels e grafici

### 4. Blazor Mostra Grafici?
Apri: http://localhost:5000/monitoring  
✅ Deve mostrare: Grafana dashboard embedded con dati

---

## 🚨 Se NON Funziona Ancora

### Problema: Metriche `aibetting_*` non visibili

**Causa:** Explorer non sta generando snapshots

**Fix:**
```powershell
# Controlla logs Explorer
cd AIBettingExplorer
dotnet run
# Cerca in output: "Mock stream started - generating snapshots every 2000ms"
# Cerca: "Metrics update: 50 snapshots processed"
```

Se non vedi questi log, c'è un problema nel codice. Verifica:
- `ExplorerService.cs` ha le metriche Prometheus?
- `Program.cs` crea `KestrelMetricServer`?

---

### Problema: Prometheus Target DOWN

**Causa:** IP errato in `prometheus.yml`

**Fix:**
```powershell
# Ottieni IP host Windows
(Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -like "192.168.*" }).IPAddress

# Aggiorna prometheus.yml
# Sostituisci in sezione aibetting-explorer:
# targets: ['<TUO_IP>:5001']

# Riavvia Prometheus
docker restart aibetting-prometheus
```

---

### Problema: Dashboard 404 in Blazor

**Causa:** UID errato in `appsettings.json`

**Verifica UID in Grafana:**
1. Apri dashboard: http://localhost:3000/d/aibetting-explorer
2. Guarda URL: `/d/[UID]/...`
3. Copia UID

**Fix appsettings.json:**
```json
{
  "Monitoring": {
    "Dashboards": {
      "explorer": {
        "uid": "aibetting-explorer"  // ← Deve corrispondere!
      }
    }
  }
}
```

Riavvia Blazor:
```powershell
cd AIBettingBlazorDashboard
dotnet run
```

---

## 📊 Output Funzionante

Quando tutto è configurato, dovresti vedere:

```
Blazor Dashboard (http://localhost:5000/monitoring)
┌─────────────────────────────────────┐
│ Total Price Updates: 285            │
│ Rate: 2.5/sec                       │
│ p95 Latency: 12ms ✅                │
│ [Grafici che si aggiornano...]      │
└─────────────────────────────────────┘
```

---

## 🎯 Checklist Veloce

- [ ] Explorer in esecuzione e genera metriche
- [ ] Prometheus target `aibetting-explorer` = UP
- [ ] Dashboard `aibetting-explorer` importata in Grafana
- [ ] UID in appsettings.json = `aibetting-explorer`
- [ ] Grafana embedding abilitato (`GF_SECURITY_ALLOW_EMBEDDING=true`)
- [ ] Blazor dashboard mostra grafici

---

## 📞 Comandi Utili

```powershell
# Restart Explorer
cd AIBettingExplorer; dotnet run

# Restart Blazor
cd AIBettingBlazorDashboard; dotnet run

# Restart Grafana
docker restart aibetting-grafana

# Restart Prometheus
docker restart aibetting-prometheus

# Check Explorer metrics
curl http://localhost:5001/metrics | Select-String "aibetting"

# Check Prometheus targets
start http://localhost:9090/targets

# Check Grafana dashboard
start http://localhost:3000/d/aibetting-explorer

# Check Blazor monitoring
start http://localhost:5000/monitoring
```

---

**Tempo Totale:** 3-5 minuti  
**Difficoltà:** ⭐⭐ (Media)  
**Status:** ✅ Testato e funzionante
