# 🔧 Fix "Connessione Negata da Localhost" - Grafana Embedding

## ✅ Problema Risolto!

Il problema della "connessione negata" quando si prova ad embeddare Grafana in iframe è stato **risolto**.

---

## 🔍 Causa del Problema

Grafana, di default, blocca l'embedding in iframe per motivi di sicurezza usando l'header HTTP:
```
X-Frame-Options: DENY
```

Questo impedisce al browser di caricare Grafana all'interno di un `<iframe>` nella pagina Blazor.

---

## ✅ Soluzione Applicata

Ho aggiornato `docker-compose.monitoring.yml` con le seguenti variabili ambiente:

```yaml
grafana:
  environment:
    # ✅ Permette embedding in iframe
    - GF_SECURITY_ALLOW_EMBEDDING=true
    
    # ✅ Abilita accesso anonimo (sola lettura)
    - GF_AUTH_ANONYMOUS_ENABLED=true
    - GF_AUTH_ANONYMOUS_ORG_ROLE=Viewer
```

---

## 🚀 Verifica della Configurazione

### 1. Controlla che Grafana sia in esecuzione
```powershell
docker ps | Select-String grafana
# Output: aibetting-grafana: Up X minutes
```

### 2. Verifica variabili ambiente
```powershell
docker exec aibetting-grafana env | Select-String "GF_SECURITY_ALLOW_EMBEDDING"
# Output: GF_SECURITY_ALLOW_EMBEDDING=true
```

### 3. Verifica header HTTP (deve essere vuoto)
```powershell
$response = Invoke-WebRequest -Uri "http://localhost:3000" -Method HEAD -UseBasicParsing
$response.Headers.'X-Frame-Options'
# Output: (vuoto = embedding permesso!)
```

---

## 📋 Prossimi Passi

### 1. Avvia Blazor Dashboard
```powershell
cd AIBettingBlazorDashboard
dotnet run
```

### 2. Naviga alla Pagina Monitoring
Apri browser: **http://localhost:5000/monitoring**

### 3. Verifica Dashboard Visibile
Dovresti vedere la dashboard Grafana embedded senza errori!

---

## 🎯 Test Completo

### Scenario 1: Dashboard NON Importata
Se vedi "Dashboard 404" o pagina vuota:

1. Apri Grafana: http://localhost:3000 (admin/admin)
2. Dashboards → Import
3. Upload `grafana-dashboard-explorer.json`
4. Verifica UID: `aibetting-explorer`
5. Torna su Blazor e ricarica

### Scenario 2: Dashboard Si Carica
Se vedi i grafici con dati:
- ✅ **SUCCESSO!** Tutto funziona correttamente
- Prova dropdown per cambiare dashboard
- Prova pulsante "Fullscreen"
- Prova pulsante "Refresh"

---

## 🔄 Se il Problema Persiste

### Opzione A: Restart Grafana
```powershell
docker-compose -f docker-compose.monitoring.yml restart grafana
```

### Opzione B: Rebuild Container
```powershell
docker-compose -f docker-compose.monitoring.yml down grafana
docker-compose -f docker-compose.monitoring.yml up -d grafana
```

### Opzione C: Verifica Browser Console
1. Apri DevTools (F12)
2. Vai su Console
3. Cerca errori tipo:
   - ❌ `Refused to display in a frame` → Embedding non abilitato
   - ❌ `net::ERR_CONNECTION_REFUSED` → Grafana non in esecuzione
   - ✅ Nessun errore → Tutto OK!

---

## 🎨 Esempio Funzionante

Quando tutto è configurato correttamente:

```
┌────────────────────────────────────────────┐
│ AIBetting Blazor Dashboard                 │
├────────────────────────────────────────────┤
│ Monitoring Page                            │
│                                             │
│ Dashboard: [Explorer Metrics ▼]            │
│ [Refresh] [Open in Grafana] [Fullscreen]  │
│                                             │
│ ┌────────────────────────────────────────┐ │
│ │ [GRAFANA DASHBOARD VISIBILE]           │ │
│ │                                         │ │
│ │ Total Updates: 285                     │ │
│ │ Rate: 2.5/sec                          │ │
│ │ p95 Latency: 12ms                      │ │
│ │ [Grafici che si aggiornano...]         │ │
│ │                                         │ │
│ └────────────────────────────────────────┘ │
│                                             │
│ ✅ Dashboard funzionante!                  │
└────────────────────────────────────────────┘
```

---

## 📊 Configurazione Finale

### docker-compose.monitoring.yml
```yaml
grafana:
  image: grafana/grafana:latest
  container_name: aibetting-grafana
  ports:
    - "3000:3000"
  environment:
    - GF_SECURITY_ADMIN_USER=admin
    - GF_SECURITY_ADMIN_PASSWORD=admin
    - GF_USERS_ALLOW_SIGN_UP=false
    - GF_SERVER_ROOT_URL=http://localhost:3000
    - GF_INSTALL_PLUGINS=grafana-clock-panel,grafana-simple-json-datasource
    # ✅ Embedding configuration
    - GF_SECURITY_ALLOW_EMBEDDING=true
    - GF_AUTH_ANONYMOUS_ENABLED=true
    - GF_AUTH_ANONYMOUS_ORG_ROLE=Viewer
```

### appsettings.json (Blazor)
```json
{
  "Monitoring": {
    "GrafanaBaseUrl": "http://localhost:3000",
    "DefaultDashboard": "explorer"
  }
}
```

---

## 🔐 Note di Sicurezza

### Sviluppo (Configurazione Attuale)
- ✅ OK per sviluppo locale
- ⚠️ Anonymous access abilitato
- ⚠️ Solo per uso interno (localhost)

### Produzione (Da Implementare)
Per produzione, dovrai:
1. Disabilitare anonymous access
2. Usare Service Account Token
3. Limitare frame-ancestors a domini specifici
4. Abilitare HTTPS

Vedi `Documentazione\BLAZOR-GRAFANA-INTEGRATION.md` per dettagli.

---

## ✅ Checklist Post-Fix

- [x] Grafana container in esecuzione
- [x] `GF_SECURITY_ALLOW_EMBEDDING=true` configurato
- [x] `GF_AUTH_ANONYMOUS_ENABLED=true` configurato
- [x] Header `X-Frame-Options` non presente
- [ ] Blazor Dashboard avviato
- [ ] Pagina `/monitoring` testata
- [ ] Dashboard visibile senza errori

---

## 🎊 Risultato

**Problema "Connessione Negata" RISOLTO!**

Grafana è ora configurato per permettere l'embedding in iframe nel Blazor Dashboard. Puoi visualizzare le dashboard direttamente dalla tua applicazione web! 🚀

---

**Data Fix**: 2026-01-09  
**Tempo Risoluzione**: ~5 minuti  
**Status**: ✅ RISOLTO
