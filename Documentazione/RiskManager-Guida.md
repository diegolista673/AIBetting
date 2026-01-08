# RiskManager - Documentazione

## Panoramica

Il `RiskManager` è un componente critico del sistema AIBE-MAS che gestisce:
- **Validazione ordini** contro limiti di esposizione e bankroll
- **Tracking esposizione** per mercato e selezione
- **Circuit breaker** per protezione da fallimenti consecutivi
- **P&L tracking** giornaliero e cumulativo

---

## Architettura

### Interfacce
- `IRiskManager` - Contratto principale per gestione rischio
- `RiskValidationResult` - Risultato validazione ordine
- `RiskLimits` - Configurazione limiti di rischio

### Implementazioni
- `RedisRiskManager` - Implementazione Redis-backed
- `RedisPnLTracker` - Helper per tracking P&L

---

## ?? Circuit Breaker Pattern

### Cos'è il Circuit Breaker?

Il **Circuit Breaker** è un pattern di protezione che **interrompe automaticamente il trading** quando si verificano troppi errori consecutivi, prevenendo perdite a cascata.

**Analogia**: Come un interruttore elettrico che scatta quando la corrente è eccessiva.

### Trigger Conditions
- **?3 ordini falliti** negli ultimi **5 minuti**
- Configurabile via `CoreConstants.MaxConsecutiveFailures`
- Auto-reset dopo 5 minuti (vecchi fallimenti scadono)

### Stati
```
?? CLOSED ? Trading attivo
   ? (3+ failures in 5 min)
?? OPEN ? Trading bloccato, kill-switch attivato
   ? (admin reset + fix issue)
?? CLOSED ? Sistema ripristinato
```

### Implementazione
```csharp
// Registra fallimento
await _riskManager.RecordFailedOrderAsync(orderId, "Reason", ct);

// Verifica se attivare circuit breaker
if (await _riskManager.ShouldTriggerCircuitBreakerAsync(ct))
{
    await _cacheBus.SetTradingEnabledAsync(false, ct);
    // Pubblica kill-switch notification
}

// Recovery manuale
await _db.KeyDeleteAsync(RedisKeys.FailedOrders);
await _cacheBus.SetTradingEnabledAsync(true, ct);
```

### Redis Storage
```bash
# Sorted set: score = timestamp
ZADD failed:orders 1704556800 '{"OrderId":"001","Reason":"Error"}'
ZCOUNT failed:orders <5min_ago> +inf  # Returns count for trigger check
```

### Benefici
- ? Previene migliaia di ordini errati da bug
- ? Blocca trading durante API Betfair down
- ? Protegge da connessioni instabili
- ? Limita danni da configurazioni errate

---

## Utilizzo

### 1. Setup Dependency Injection

```csharp
// In Program.cs o Startup.cs
services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = ConfigurationOptions.Parse("localhost:6379");
    return ConnectionMultiplexer.Connect(config);
});

services.AddSingleton<IRiskManager, RedisRiskManager>();
services.AddSingleton<RedisPnLTracker>();
```

### 2. Validazione Ordine (in Executor)

```csharp
public class OrderExecutor : IOrderExecutor
{
    private readonly IRiskManager _riskManager;
    
    public async Task<OrderResult> PlaceAsync(PlaceOrderRequest request, CancellationToken ct)
    {
        // STEP 1: Validare l'ordine contro risk rules
        var validation = await _riskManager.ValidateOrderAsync(request, ct);
        
        if (!validation.IsValid)
        {
            _logger.LogWarning("Order rejected: {Reason}", validation.RejectionReason);
            
            // Registra come fallimento per circuit breaker
            await _riskManager.RecordFailedOrderAsync(
                Guid.NewGuid().ToString(), 
                validation.RejectionReason!, 
                ct);
            
            return new OrderResult 
            { 
                OrderId = "",
                Status = OrderStatus.Cancelled,
                Message = validation.RejectionReason
            };
        }
        
        // STEP 2: Piazzare l'ordine su Betfair
        var result = await PlaceBetfairOrderAsync(request, ct);
        
        // STEP 3: Aggiornare exposure se matched
        if (result.Status == OrderStatus.Matched)
        {
            await _riskManager.UpdateExposureAsync(
                request.MarketId,
                request.SelectionId,
                request.Side,
                request.Stake,
                request.Odds,
                ct);
        }
        
        return result;
    }
}
```

### 3. Tracking P&L (in Accounting)

```csharp
public class TradeLogger : ITradeLogger
{
    private readonly RedisPnLTracker _pnlTracker;
    private readonly TradingDbContext _dbContext;
    
    public async Task LogAsync(TradeRecord trade, CancellationToken ct)
    {
        // Salva in PostgreSQL
        await _dbContext.Trades.AddAsync(trade, ct);
        await _dbContext.SaveChangesAsync(ct);
        
        // Aggiorna P&L in Redis se settled
        if (trade.NetProfit.HasValue)
        {
            await _pnlTracker.UpdatePnLAsync(trade.NetProfit.Value, ct);
        }
    }
}
```

### 4. Monitoraggio Circuit Breaker (in Watchdog)

```csharp
public class WatchdogService : BackgroundService
{
    private readonly IRiskManager _riskManager;
    private readonly ICacheBus _cacheBus;
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // Controlla circuit breaker ogni 30 secondi
            var shouldTrigger = await _riskManager.ShouldTriggerCircuitBreakerAsync(ct);
            
            if (shouldTrigger)
            {
                _logger.LogCritical("Circuit breaker triggered! Disabling trading.");
                
                // Disabilita trading
                await _cacheBus.SetTradingEnabledAsync(false, ct);
                
                // Pubblica kill-switch
                await PublishKillSwitchAsync(
                    "Circuit breaker activated - too many failed orders", 
                    ct);
            }
            
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
        }
    }
}
```

---

## Configurazione Risk Limits

### Default (Conservativi)
```csharp
var defaults = RiskLimits.Default;
// Bankroll: 10,000
// MaxExposurePerMarket: 500
// MaxExposurePerSelection: 200
// MaxStakePerOrder: 100
// MaxDailyLoss: 500
// MaxRiskPerTradePercent: 2%
```

### Custom
```csharp
var customLimits = new RiskLimits
{
    Bankroll = 50000m,
    MaxExposurePerMarket = 2000m,
    MaxExposurePerSelection = 1000m,
    MaxStakePerOrder = 500m,
    MaxDailyLoss = 2000m,
    MaxRiskPerTradePercent = 0.01m // 1%
};

await _riskManager.UpdateRiskLimitsAsync(customLimits, ct);
```

### Via Dashboard/Admin Panel
```csharp
// API endpoint per admin
[HttpPost("api/risk/limits")]
public async Task<IActionResult> UpdateLimits([FromBody] RiskLimits limits)
{
    await _riskManager.UpdateRiskLimitsAsync(limits, CancellationToken.None);
    return Ok();
}
```

---

## Redis Data Structures

### Exposure Tracking
```redis
# Hash per mercato: exposure:{marketId}
HGETALL exposure:1.234567890
> "12345" "150.50"    # SelectionId -> Exposure
> "67890" "200.00"

# Hash posizioni: positions:{marketId}
HGETALL positions:1.234567890
> "12345" "{\"SelectionId\":\"12345\",\"Side\":\"Back\",\"Stake\":100,\"Odds\":2.5,...}"
```

### Risk Limits
```redis
# Hash configurazione
HGETALL risk:limits
> "Bankroll" "10000.00"
> "MaxExposurePerMarket" "500.00"
> "MaxExposurePerSelection" "200.00"
> ...
```

### Circuit Breaker
```redis
# Sorted set con timestamp come score
ZRANGE failed:orders 0 -1 WITHSCORES
> "{\"OrderId\":\"abc\",\"Reason\":\"Insufficient funds\",...}" "1704556800"
> "{\"OrderId\":\"def\",\"Reason\":\"Invalid odds\",...}" "1704556850"
```

### P&L Tracking
```redis
# String per daily P&L
GET pnl:daily:2024-01-15
> "125.50"

# String per total P&L
GET pnl:total
> "3450.75"
```

---

## Logica di Validazione

### Order Validation Flow
```
1. Stake <= MaxStakePerOrder?
2. PotentialLoss <= Bankroll * MaxRiskPerTradePercent?
3. SelectionExposure + PotentialLoss <= MaxExposurePerSelection?
4. MarketExposure + PotentialLoss <= MaxExposurePerMarket?
5. DailyPnL > -MaxDailyLoss?
6. CircuitBreaker NOT triggered?
7. TradingEnabled == true?

? ALL PASS -> Order Allowed
? ANY FAIL -> Order Rejected
```

### Potential Loss Calculation
```csharp
// BACK: rischio = stake
var backLoss = stake;

// LAY: rischio = liability = stake * (odds - 1)
var layLoss = stake * (odds - 1);

// Esempio:
// LAY €100 @ 3.0 -> Liability = 100 * (3.0 - 1) = €200
```

### Circuit Breaker Logic
```csharp
// Trigger se >= 3 ordini falliti negli ultimi 5 minuti
var failedOrdersWindow = TimeSpan.FromMinutes(5);
var maxFailures = CoreConstants.MaxConsecutiveFailures; // 3

if (recentFailures >= maxFailures)
{
    // Kill-switch attivato
    TradingEnabled = false;
}
```

---

## Best Practices

### 1. Always Validate Before Placing
```csharp
// ? BAD
await PlaceOrderAsync(request);

// ? GOOD
var validation = await _riskManager.ValidateOrderAsync(request, ct);
if (validation.IsValid)
{
    await PlaceOrderAsync(request);
}
```

### 2. Update Exposure on Match
```csharp
// Solo quando ordine è matched, non pending
if (result.Status == OrderStatus.Matched)
{
    await _riskManager.UpdateExposureAsync(...);
}
```

### 3. Record ALL Failures
```csharp
// Include validazione, API errors, timeouts
await _riskManager.RecordFailedOrderAsync(orderId, reason, ct);
```

### 4. Monitor Circuit Breaker
```csharp
// Check periodico in Watchdog service
var shouldHalt = await _riskManager.ShouldTriggerCircuitBreakerAsync(ct);
```

### 5. Update P&L After Settlement
```csharp
// Non al place, ma dopo il risultato dell'evento
if (trade.NetProfit.HasValue)
{
    await _pnlTracker.UpdatePnLAsync(trade.NetProfit.Value, ct);
}
```

---

## Testing

### Unit Test Example
```csharp
[Fact]
public async Task ValidateOrder_ExceedsStakeLimit_ReturnsInvalid()
{
    // Arrange
    var limits = new RiskLimits { MaxStakePerOrder = 100m };
    await _riskManager.UpdateRiskLimitsAsync(limits, CancellationToken.None);
    
    var request = new PlaceOrderRequest
    {
        MarketId = new MarketId("1.123"),
        SelectionId = new SelectionId("456"),
        Side = TradeSide.Back,
        Stake = 200m, // Exceeds limit!
        Odds = 2.0m
    };
    
    // Act
    var result = await _riskManager.ValidateOrderAsync(request, CancellationToken.None);
    
    // Assert
    Assert.False(result.IsValid);
    Assert.Contains("exceeds max per order", result.RejectionReason);
}
```

### Integration Test with Testcontainers
```csharp
[Fact]
public async Task UpdateExposure_PersistsToRedis()
{
    // Arrange
    using var redisContainer = new RedisContainer();
    await redisContainer.StartAsync();
    
    var redis = ConnectionMultiplexer.Connect(redisContainer.GetConnectionString());
    var riskManager = new RedisRiskManager(redis);
    
    // Act
    await riskManager.UpdateExposureAsync(
        new MarketId("1.123"),
        new SelectionId("456"),
        TradeSide.Back,
        100m,
        2.5m,
        CancellationToken.None);
    
    // Assert
    var exposure = await riskManager.GetMarketExposureAsync(
        new MarketId("1.123"), 
        CancellationToken.None);
    
    Assert.Equal(100m, exposure["456"]);
}
```

---

## Troubleshooting

### Problem: Circuit breaker triggers troppo spesso
**Soluzione**: Aumenta `MaxConsecutiveFailures` o riduci il window
```csharp
// In CoreConstants.cs
public const int MaxConsecutiveFailures = 5; // Da 3 a 5
```

### Problem: Ordini rifiutati per exposure anche su mercati nuovi
**Soluzione**: Verifica TTL delle chiavi exposure (24h default)
```bash
redis-cli TTL exposure:1.234567890
```

### Problem: Daily P&L non si resetta
**Soluzione**: Le chiavi daily hanno TTL 48h, usa chiavi separate per giorno
```csharp
var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
var pnlKey = RedisKeys.DailyPnL(today); // Chiave per data specifica
```

### Problem: Validation lenta (> 50ms)
**Soluzione**: Usa pipeline Redis per queries multiple
```csharp
var batch = _db.CreateBatch();
var exposureTask = batch.HashGetAllAsync(exposureKey);
var limitsTask = batch.HashGetAllAsync(RedisKeys.RiskLimits);
batch.Execute();
await Task.WhenAll(exposureTask, limitsTask);
```

---

## KPI e Monitoring

### Metriche da tracciare
```csharp
// In Watchdog/Dashboard
var metrics = new
{
    TotalExposure = exposures.Values.Sum(),
    ActiveMarkets = exposures.Count,
    DailyPnL = await _pnlTracker.GetTodayPnLAsync(ct),
    TotalPnL = await _pnlTracker.GetTotalPnLAsync(ct),
    FailedOrdersLast5Min = await GetRecentFailuresCountAsync(ct),
    CircuitBreakerActive = await _riskManager.ShouldTriggerCircuitBreakerAsync(ct),
    Limits = await _riskManager.GetRiskLimitsAsync(ct)
};
```

### Alert Triggers
- `DailyPnL < -MaxDailyLoss * 0.8` ? Warning
- `CircuitBreakerActive == true` ? Critical
- `TotalExposure > Bankroll * 0.5` ? Warning
- `FailedOrders > MaxConsecutiveFailures * 0.7` ? Warning

---

## Roadmap

- [ ] Support per multi-currency exposure
- [ ] Dynamic risk limits basati su ML confidence
- [ ] Exposure netting per opposite positions
- [ ] Integration con margin requirements Betfair
- [ ] Hedging automatico su soglia exposure

---

**Ultima Modifica**: 2024-01-15  
**Versione**: 1.0.0  
**Autore**: Diego Lista
