# ✅ Guida Rapida - Applicare Dashboard in Grafana

## 🎯 **File Pronto da Usare**

**File:** `grafana-dashboards/strategy-signals-grafana-ready.json`

Questo è il JSON corretto **senza il wrapper** `{"dashboard": ...}` - pronto da incollare direttamente nel JSON Model di Grafana.

---

## 📋 **Metodo 1: JSON Model (Raccomandato)** ⭐

### **Passi:**

1. **Apri Grafana** → `http://localhost:3000`

2. **Crea nuova dashboard:**
   - Click **"+"** (sidebar sinistra)
   - **"Create Dashboard"**
   - **"Add visualization"** (qualsiasi tipo)
   - Click **"Save dashboard"** (icona disco top-right)
   - Nome: `Strategy Signals Monitor`

3. **Apri JSON Model:**
   - Click **⚙️ Dashboard Settings** (icona ingranaggio top-right)
   - Sidebar sinistra → **"JSON Model"**

4. **Sostituisci JSON:**
   - **Ctrl+A** (seleziona tutto il JSON esistente)
   - **Cancella**
   - Apri file: `grafana-dashboards/strategy-signals-grafana-ready.json`
   - **Ctrl+A** → **Ctrl+C** (copia tutto)
   - Torna in Grafana JSON Model
   - **Ctrl+V** (incolla)

5. **Salva:**
   - Click **"Save changes"** (bottone blu in alto a destra nel JSON Model)
   - Torna alla dashboard (click "Back" o freccia)
   - Click **"Save dashboard"** (icona disco)

6. **Configura Data Source:**
   - Se vedi "No data source":
     - Edit un panel qualsiasi
     - Dropdown "Data source" → Seleziona **"Prometheus"**
     - Apply
   - Oppure:
     - Vai in ogni panel e seleziona Prometheus dal dropdown

---

## 📋 **Metodo 2: Import Dashboard**

### **Passi:**

1. **Import:**
   - Click **"+"** → **"Import"**
   - **"Upload JSON file"**
   - Seleziona: `grafana-dashboards/strategy-signals-monitor.json` (quello originale con wrapper)

2. **Configura:**
   - **Name:** Strategy Signals Monitor
   - **Folder:** General (o crea "AIBetting")
   - **Prometheus:** Seleziona dal dropdown

3. **Import:**
   - Click **"Import"**

**Nota:** Questo metodo usa il file originale con il wrapper `{"dashboard": ...}` che è corretto per l'import via UI.

---

## 🔍 **Verifica Funzionamento**

### **Checklist:**

1. **Dashboard visibile:**
   ```
   http://localhost:3000/d/aibetting-strategy-signals/strategy-signals-monitor
   ```

2. **Panel senza errori:**
   - [ ] Nessun "No data source" visible
   - [ ] Panel non mostrano errori rossi
   - [ ] Auto-refresh funziona (5s in alto a destra)

3. **Dati presenti:**
   - [ ] Panel "Total Signals Today" mostra un numero (anche 0 va bene)
   - [ ] Panel "Signal Rate" mostra grafici (anche vuoti se nessun dato)
   - [ ] Nessun "No data" se Analyst è attivo e genera metriche

---

## 🔧 **Configura Data Source su Tutti i Panel**

Se dopo import alcuni panel mostrano "No data source":

### **Metodo Veloce:**

1. **Dashboard Settings** (⚙️)
2. **Variables** (sidebar)
3. **Add variable**

**Config:**
```
Name: datasource
Type: Datasource
Query: prometheus
Hide: Variable (nasconde dropdown in dashboard)
```

4. **Save variable**

5. **JSON Model:**
   - Cerca tutte le occorrenze di `"datasource": { "type": "prometheus" }`
   - Sostituisci con: `"datasource": { "type": "prometheus", "uid": "${datasource}" }`

6. **Save Dashboard**

Oppure **più semplice**:

1. **Edit un panel** (click titolo → Edit)
2. **Data source dropdown** → Seleziona Prometheus
3. **Apply**
4. Ripeti per tutti i 9 panel (o usa JSON Model per cambio globale)

---

## 🐛 **Troubleshooting**

### **Problema: "Panel data error: datasource not found"**

**Fix:**
1. Configuration → Data Sources
2. Verifica Prometheus esiste
3. Se manca, aggiungi:
   - Type: Prometheus
   - URL: `http://localhost:9090`
   - Access: Server (default)
   - Save & Test

### **Problema: "No data"**

**Fix:**
```powershell
# 1. Check Analyst attivo
Get-Process | Where { $_.ProcessName -like "*Analyst*" }

# 2. Check metriche esistono
curl http://localhost:5002/metrics | Select-String "aibetting_analyst"

# 3. Check Prometheus scraping
# Apri http://localhost:9090/targets
# Verifica "aibetting-analyst" è UP
```

### **Problema: Import fallisce con errore JSON**

**Fix:**
- Usa `strategy-signals-grafana-ready.json` invece di quello con wrapper
- Verifica JSON valido: https://jsonlint.com/
- Copia-incolla manualmente (evita encoding issues)

---

## 📊 **Panel da Verificare**

Dopo import/paste, verifica questi panel chiave:

| # | Panel | Query | Verifica |
|---|-------|-------|----------|
| 1 | Total Signals | `sum(aibetting_analyst_signals_by_type_total)` | Numero visibile |
| 4 | Distribution | `sum by (strategy) (increase[1h])` | Pie chart con slice |
| 6 | Signal Rate | `rate(...) * 3600` | Grafici linee |
| 9 | Table | 4 query merge | Tabella con colonne |

---

## ✅ **Success!**

Dashboard pronta quando:
- ✅ 9 panel visibili
- ✅ Nessun errore rosso
- ✅ Auto-refresh attivo (5s)
- ✅ Time range selector funziona
- ✅ Dati popolano (se Analyst genera metriche)

---

## 🎯 **Differenze Tra i File**

| File | Uso | Wrapper | Quando |
|------|-----|---------|--------|
| `strategy-signals-monitor.json` | Import UI | `{"dashboard": ...}` | Import via "+" → Import |
| `strategy-signals-grafana-ready.json` | JSON Model | `{"title": ...}` | Settings → JSON Model |

**TIP:** Se import via UI non funziona, usa JSON Model con il file `-grafana-ready.json`!

---

## 🚀 **Next Steps**

Dopo import riuscito:

1. **Salva dashboard** (icona disco)
2. **Star dashboard** (⭐ in alto) per aggiungerla ai preferiti
3. **Configura alert** (opzionale)
4. **Condividi URL** con team
5. **Test con dati reali** avviando Analyst

---

**Creato:** 2026-01-12  
**File:** strategy-signals-grafana-ready.json  
**Status:** ✅ Ready to Use  
**Metodo:** JSON Model Paste
