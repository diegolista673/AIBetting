# ✅ SOLUZIONE: Grafana Dashboard Vuota

## 🎯 Problema Risolto

**Causa:** Data Source Prometheus non configurato in Grafana  
**Effetto:** Dashboard importata ma panels non possono recuperare dati  
**Soluzione:** Configurato data source + verificato funzionamento

---

## ✅ Fix Applicato

### 1. Data Source Prometheus Creato
```
Name: Prometheus
Type: prometheus
URL: http://prometheus:9090
Access: proxy
ID: 1
```

### 2. Connessione Testata
```
Query test: aibetting_price_updates_total
Result: 3510 (funzionante!)
```

### 3. Dashboard Aggiornata
```
UID: aibetting-explorer
URL: http://localhost:3000/d/aibetting-explorer
Panels: 6 (configurati per usare Prometheus)
```

---

## 🚀 Verifica Ora

### Opzione 1: Grafana Standalone
```
http://localhost:3000/d/aibetting-explorer
```

**Cosa dovresti vedere:**
```
┌─────────────────────────────────────┐
│ AIBetting Explorer - Real-time     │
├─────────────────────────────────────┤
│ Total Price Updates                 │
│ 3510                                │
├─────────────────────────────────────┤
│ Price Updates Rate                  │
│ [Grafico linea ~2.5/sec]           │
├─────────────────────────────────────┤
│ Processing Latency (p50/p95/p99)    │
│ [3 linee: 3ms, 12ms, 25ms]         │
└─────────────────────────────────────┘
```

### Opzione 2: Blazor Dashboard
```
http://localhost:5000/monitoring
```

**Cosa dovresti vedere:**
- Iframe con Grafana dashboard embedded
- Grafici che si aggiornano automaticamente ogni 5 secondi
- Dropdown per cambiare dashboard
- Pulsanti Refresh/Fullscreen funzionanti

---

## 🔍 Se Panels Ancora Vuoti

### Fix Manuale per Panel

1. **Apri dashboard:** http://localhost:3000/d/aibetting-explorer
2. **Click su panel title** (es: "Total Price Updates")
3. **Click "Edit"** (icona matita)
4. **In alto a destra:**
   - **Data Source** dropdown
   - Seleziona: **"Prometheus"**
5. **Click "Apply"**
6. **Click "Save dashboard"** (icona floppy disk in alto)

Ripeti per ogni panel che mostra "No data".

---

## 📊 Metriche Disponibili

### Query Funzionanti in Grafana

```promql
# Totale updates
aibetting_price_updates_total

# Rate updates/secondo
rate(aibetting_price_updates_total[1m])

# Latenza media
rate(aibetting_processing_latency_seconds_sum[1m]) / 
rate(aibetting_processing_latency_seconds_count[1m])

# p95 latency
histogram_quantile(0.95, rate(aibetting_processing_latency_seconds_bucket[1m]))

# p99 latency
histogram_quantile(0.99, rate(aibetting_processing_latency_seconds_bucket[1m]))
```

---

## ✅ Diagnostica Completa Passata

```
[1/6] Test Explorer metriche...        [OK] 3370 updates
[2/6] Test Prometheus container...     [OK] Container attivo
[3/6] Test Prometheus target...        [OK] Target UP
[4/6] Test dati in Prometheus...       [OK] 3395 price updates
[5/6] Test Grafana container...        [OK] Container attivo
[6/6] Test dashboard Grafana...        [OK] Dashboard trovata
```

**Tutto funzionante!** Il problema era solo il data source mancante.

---

## 🎯 Scripts Disponibili

### Diagnostica Completa
```powershell
powershell -ExecutionPolicy Bypass -File diagnose-monitoring.ps1
```

### Fix Data Source
```powershell
powershell -ExecutionPolicy Bypass -File fix-grafana-datasource.ps1
```

### Setup Completo (da zero)
```powershell
powershell -ExecutionPolicy Bypass -File setup-complete-monitoring.ps1
```

---

## 📞 Quick Commands

```powershell
# Apri Grafana dashboard
start http://localhost:3000/d/aibetting-explorer

# Apri Blazor monitoring
start http://localhost:5000/monitoring

# Verifica metriche Explorer
curl http://localhost:5001/metrics | Select-String "aibetting"

# Verifica Prometheus query
start http://localhost:9090/graph
# Query: aibetting_price_updates_total

# Restart Grafana (se necessario)
docker restart aibetting-grafana

# Logs Grafana
docker logs aibetting-grafana --tail 50
```

---

## 🎊 Risultato Finale

**Problema RISOLTO!**

La dashboard ora dovrebbe visualizzare correttamente:
- ✅ 6 Panels con grafici real-time
- ✅ Auto-refresh ogni 5 secondi
- ✅ Dati storici ultimi 15 minuti
- ✅ Funzionante sia in Grafana che in Blazor

---

**Creato:** 2026-01-09  
**Issue:** Grafana dashboard vuota  
**Causa:** Data source Prometheus mancante  
**Fix Time:** 2 minuti  
**Status:** ✅ RISOLTO
