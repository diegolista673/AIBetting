# 🚀 Quick Import Guide - Strategy Signals Monitor Dashboard

## ✅ **Dashboard Creata!**

Il file JSON della dashboard Grafana completa è stato creato:
**`grafana-dashboards/strategy-signals-monitor.json`**

---

## 📊 **Contenuto Dashboard**

### **9 Panel Configurati:**

1. **Total Signals Today** (Stat) - Contatore totale segnali
2. **High-Confidence Signals** (Stat) - Segnali con confidence > 80%
3. **Average ROI** (Stat) - ROI medio tutte le strategie
4. **Strategy Distribution** (Pie Chart) - % segnali per strategia
5. **Signals by Risk** (Bar Gauge) - Distribuzione per livello rischio
6. **Signal Rate** (Time Series) - Segnali/ora nel tempo
7. **Confidence Timeline** (Time Series) - Confidence per strategia
8. **ROI by Strategy** (Bar Chart) - ROI medio per strategia
9. **Latest Signals Summary** (Table) - Riepilogo dettagliato

### **Configurazione:**
- ✅ Auto-refresh: 5 secondi
- ✅ Time range: Ultima ora
- ✅ Thresholds coloring configurati
- ✅ Tooltips multi-series
- ✅ Legend con calcoli (max, mean, last)
- ✅ Annotation per alert

---

## 🔧 **Come Importare**

### **Metodo 1: Via UI Grafana** (Più Semplice)

1. **Apri Grafana:**
   ```
   http://localhost:3000
   ```

2. **Login** (default: admin/admin)

3. **Import Dashboard:**
   - Click icona **"+"** (sidebar sinistra)
   - Seleziona **"Import"**
   - Click **"Upload JSON file"**
   - Seleziona file: `grafana-dashboards/strategy-signals-monitor.json`
   - Click **"Load"**

4. **Configura Data Source:**
   - **Prometheus:** Seleziona il tuo data source Prometheus
   - **Folder:** Scegli "AIBetting" (o crea nuovo)
   - **UID:** `aibetting-strategy-signals` (auto-generato)

5. **Import!**
   - Click **"Import"**
   - Dashboard pronta! 🎉

**URL Dashboard:** `http://localhost:3000/d/aibetting-strategy-signals/strategy-signals-monitor`

---

### **Metodo 2: Via API** (Automazione)

```powershell
# PowerShell
$headers = @{
    "Content-Type" = "application/json"
    "Authorization" = "Bearer YOUR_API_KEY"
}

$body = Get-Content "grafana-dashboards/strategy-signals-monitor.json" -Raw

Invoke-RestMethod `
    -Uri "http://localhost:3000/api/dashboards/db" `
    -Method Post `
    -Headers $headers `
    -Body $body
```

**Nota:** Sostituisci `YOUR_API_KEY` con la tua API key Grafana (Settings → API Keys).

---

### **Metodo 3: Via CLI** (Docker/K8s)

```bash
# Bash
curl -X POST \
  http://localhost:3000/api/dashboards/db \
  -H 'Content-Type: application/json' \
  -H 'Authorization: Bearer YOUR_API_KEY' \
  -d @grafana-dashboards/strategy-signals-monitor.json
```

---

## 🔍 **Verifica Pre-Import**

### **Checklist:**

- [ ] **Analyst è attivo:**
  ```powershell
  Get-Process | Where { $_.ProcessName -like "*Analyst*" }
  ```

- [ ] **Metriche disponibili:**
  ```powershell
  curl http://localhost:5002/metrics | Select-String "signals_by_type"
  ```

- [ ] **Prometheus scraping:**
  - Apri `http://localhost:9090/targets`
  - Verifica `aibetting-analyst` è **UP**

- [ ] **Grafana accessibile:**
  ```powershell
  curl http://localhost:3000/api/health
  # Output: {"commit":"...","database":"ok",...}
  ```

---

## 🎨 **Personalizzazione Post-Import**

### **1. Cambia Time Range Default**

Dashboard Settings (⚙️) → Time Options:
- **Default:** `now-6h` (ultime 6 ore)
- **Refresh:** `10s` (ogni 10 secondi)

### **2. Aggiungi Variabili**

Dashboard Settings → Variables → Add Variable:

**Variable: `strategy`**
```
Name: strategy
Type: Query
Query: label_values(aibetting_analyst_signals_by_type_total, strategy)
Multi-value: true
Include All: true
```

Poi modifica query panel:
```promql
# Prima
sum by (strategy) (increase(aibetting_analyst_signals_by_type_total[1h]))

# Dopo (con variabile)
sum by (strategy) (increase(aibetting_analyst_signals_by_type_total{strategy=~"$strategy"}[1h]))
```

### **3. Aggiungi Alert**

Panel → Edit → Alert:

**Alert: High Confidence Signal**
```yaml
Condition: 
  WHEN last() OF query(A) IS ABOVE 0.9

Evaluate every: 1m
For: 1m

Notifications:
  Send to: Slack/Email
  Message: "🚨 High confidence signal: {{strategy}}"
```

---

## 📊 **Test Dashboard**

### **Genera Dati di Test:**

Se non ci sono ancora segnali reali, puoi:

1. **Abilita tutte le strategie** in `appsettings.json`:
   ```json
   "Scalping": { "Enabled": true },
   "SteamMove": { "Enabled": true },
   "GreenUp": { "Enabled": true },
   "ValueBet": { "Enabled": true }
   ```

2. **Riavvia Analyst:**
   ```powershell
   cd AIBettingAnalyst
   dotnet run
   ```

3. **Avvia Explorer** (genera dati mock):
   ```powershell
   cd AIBettingExplorer
   dotnet run
   ```

4. **Attendi 2-3 minuti** per vedere primi segnali

5. **Refresh dashboard** e verifica panel popolati

---

## 🔧 **Troubleshooting**

### **Problema: Panel vuoti "No data"**

**Causa:** Prometheus non ha ancora dati o scraping non configurato

**Fix:**
```powershell
# 1. Verifica metriche esistono
curl http://localhost:5002/metrics | Select-String "aibetting_analyst"

# 2. Verifica Prometheus scraping
# Apri http://localhost:9090/targets
# Verifica "aibetting-analyst" è UP

# 3. Test query manualmente
# Apri http://localhost:9090/graph
# Query: aibetting_analyst_signals_by_type_total
```

### **Problema: "Unable to connect to data source"**

**Fix:**
1. Dashboard Settings → Variables
2. Verifica Data Source: **Prometheus**
3. Se manca, vai in Configuration → Data Sources → Add Prometheus
4. URL: `http://localhost:9090`
5. Save & Test

### **Problema: Import fallisce**

**Fix:**
1. Verifica versione Grafana >= 10.0
2. Controlla JSON è valido: `jsonlint strategy-signals-monitor.json`
3. Prova import manuale copiando contenuto JSON

---

## 📈 **Panel Descriptions**

### **Row 1: Overview Stats**

| Panel | Metrica | Threshold |
|-------|---------|-----------|
| Total Signals | `sum(aibetting_analyst_signals_by_type_total)` | <10 red, 10-50 yellow, >50 green |
| High-Conf | `count(confidence > 0.8)` | <5 red, 5-20 yellow, >20 green |
| Avg ROI | `avg(last_signal_roi)` | <1% red, 1-3% yellow, >3% green |

### **Row 2: Distribution**

| Panel | Tipo | Query |
|-------|------|-------|
| Strategy Distribution | Pie Chart | `sum by (strategy) (increase[1h])` |
| Signals by Risk | Bar Gauge | `sum by (risk_level) (increase[1h])` |

### **Row 3: Time Series**

- **Signal Rate:** Rate segnali/ora per strategia
- **Multi-line:** Ogni strategia+tipo è una linea
- **Smooth interpolation** per trend più leggibili

### **Row 4: Performance**

| Panel | Tipo | Focus |
|-------|------|-------|
| Confidence Timeline | Time Series | Confidence trend per strategia |
| ROI by Strategy | Bar Chart | Confronto ROI medio |

### **Row 5: Table**

**Columns:**
1. Strategy
2. Last Confidence (color-coded)
3. Last ROI (%)
4. Total Signals
5. Avg Confidence

**Sorting:** Default by Total Signals DESC

---

## 🎯 **Utilizzo Ottimale**

### **Monitoring Giornaliero:**

1. **Mattina:** 
   - Check "Total Signals Today"
   - Verifica nessuna strategia "morta" (0 segnali)

2. **Durante Trading:**
   - Monitora "Signal Rate" per spike anomali
   - Alert su "High-Confidence Signals"

3. **Sera:**
   - Analizza "ROI by Strategy"
   - Ottimizza config strategie underperforming

### **Alert Consigliati:**

```yaml
1. Low Signal Rate:
   expr: rate(signals[5m]) * 3600 < 1
   for: 10m
   
2. High Confidence:
   expr: last_signal_confidence > 0.9
   
3. High ROI:
   expr: last_signal_roi > 8
```

---

## ✅ **Checklist Post-Import**

- [ ] Dashboard importata senza errori
- [ ] Tutti i 9 panel visibili
- [ ] Dati presenti (no "No data")
- [ ] Auto-refresh funzionante (5s)
- [ ] Time range selector funziona
- [ ] Legend mostra valori corretti
- [ ] Tooltips informativi
- [ ] Colori threshold corretti
- [ ] Table sorting funziona
- [ ] Export/share dashboard funziona

---

## 🎉 **Success!**

Se tutti i panel mostrano dati, hai completato con successo:

1. ✅ Implementazione metriche Prometheus
2. ✅ Dashboard Grafana configurata
3. ✅ Monitoring real-time segnali strategie
4. ✅ Sistema pronto per produzione

**Next Steps:**
- [ ] Configura alert critici
- [ ] Aggiungi variabili per filtri
- [ ] Crea snapshot per backup
- [ ] Condividi con team

---

## 📚 **Risorse**

- **Dashboard File:** `grafana-dashboards/strategy-signals-monitor.json`
- **Guida Completa:** `GRAFANA-STRATEGY-SIGNALS-DASHBOARD.md`
- **Metriche Docs:** `STRATEGY-SIGNALS-METRICS-SUMMARY.md`
- **Analyst Docs:** `AIBETTING-ANALYST-RIEPILOGO.md`

---

**Creato:** 2026-01-12  
**Dashboard UID:** `aibetting-strategy-signals`  
**Panels:** 9  
**Status:** ✅ Ready for Import
