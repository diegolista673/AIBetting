-- AIBetting Database Schema
-- PostgreSQL 14+
-- Database: aibetting_db

-- Tabella trades (tracciamento ordini e P&L)
CREATE TABLE IF NOT EXISTS trades (
    id UUID PRIMARY KEY,
    timestamp TIMESTAMPTZ NOT NULL,
    market_id TEXT NOT NULL,
    selection_id TEXT NOT NULL,
    stake NUMERIC NOT NULL,
    odds NUMERIC NOT NULL,
    type TEXT NOT NULL CHECK (type IN ('BACK', 'LAY')),
    status TEXT NOT NULL CHECK (status IN ('PENDING', 'MATCHED', 'UNMATCHED', 'CANCELLED')),
    profit_loss NUMERIC NULL,
    commission NUMERIC NOT NULL,
    net_profit NUMERIC NULL,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Indici per performance
CREATE INDEX IF NOT EXISTS idx_trades_timestamp ON trades(timestamp);
CREATE INDEX IF NOT EXISTS idx_trades_market_id ON trades(market_id);
CREATE INDEX IF NOT EXISTS idx_trades_status ON trades(status);
CREATE INDEX IF NOT EXISTS idx_trades_created_at ON trades(created_at);

-- Tabella daily_summaries (aggregati giornalieri)
CREATE TABLE IF NOT EXISTS daily_summaries (
    id SERIAL PRIMARY KEY,
    date DATE NOT NULL UNIQUE,
    total_trades INTEGER NOT NULL DEFAULT 0,
    total_stake NUMERIC NOT NULL DEFAULT 0,
    total_profit_loss NUMERIC NOT NULL DEFAULT 0,
    total_commission NUMERIC NOT NULL DEFAULT 0,
    net_profit NUMERIC NOT NULL DEFAULT 0,
    roi_percent NUMERIC NULL,
    win_rate NUMERIC NULL,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Indice per query per data
CREATE INDEX IF NOT EXISTS idx_daily_summaries_date ON daily_summaries(date DESC);

-- View per statistiche rapide
CREATE OR REPLACE VIEW v_trading_stats AS
SELECT 
    COUNT(*) as total_orders,
    COUNT(*) FILTER (WHERE status = 'MATCHED') as matched_orders,
    COUNT(*) FILTER (WHERE status = 'UNMATCHED') as unmatched_orders,
    COUNT(*) FILTER (WHERE status = 'CANCELLED') as cancelled_orders,
    ROUND(COUNT(*) FILTER (WHERE status = 'MATCHED')::numeric / NULLIF(COUNT(*), 0) * 100, 2) as match_rate_percent,
    SUM(stake) as total_staked,
    SUM(net_profit) FILTER (WHERE net_profit IS NOT NULL) as total_net_profit,
    ROUND(AVG(net_profit) FILTER (WHERE net_profit IS NOT NULL), 2) as avg_net_profit_per_trade,
    MAX(net_profit) as best_trade,
    MIN(net_profit) as worst_trade
FROM trades
WHERE created_at >= CURRENT_DATE - INTERVAL '30 days';

-- View per P&L giornaliero
CREATE OR REPLACE VIEW v_daily_pnl AS
SELECT 
    DATE(created_at) as trade_date,
    COUNT(*) as num_trades,
    SUM(stake) as total_stake,
    SUM(profit_loss) as gross_pnl,
    SUM(commission) as total_commission,
    SUM(net_profit) as net_pnl,
    ROUND(SUM(net_profit) / NULLIF(SUM(stake), 0) * 100, 2) as roi_percent
FROM trades
WHERE status = 'MATCHED' AND net_profit IS NOT NULL
GROUP BY DATE(created_at)
ORDER BY trade_date DESC;

-- Function per aggiornare daily_summaries automaticamente
CREATE OR REPLACE FUNCTION update_daily_summary()
RETURNS TRIGGER AS $$
BEGIN
    INSERT INTO daily_summaries (
        date, 
        total_trades, 
        total_stake, 
        total_profit_loss, 
        total_commission, 
        net_profit,
        roi_percent,
        win_rate,
        updated_at
    )
    SELECT 
        DATE(NEW.created_at),
        COUNT(*),
        SUM(stake),
        SUM(profit_loss),
        SUM(commission),
        SUM(net_profit),
        ROUND(SUM(net_profit) / NULLIF(SUM(stake), 0) * 100, 2),
        ROUND(COUNT(*) FILTER (WHERE net_profit > 0)::numeric / NULLIF(COUNT(*), 0) * 100, 2),
        NOW()
    FROM trades
    WHERE DATE(created_at) = DATE(NEW.created_at)
        AND status = 'MATCHED'
        AND net_profit IS NOT NULL
    ON CONFLICT (date) DO UPDATE SET
        total_trades = EXCLUDED.total_trades,
        total_stake = EXCLUDED.total_stake,
        total_profit_loss = EXCLUDED.total_profit_loss,
        total_commission = EXCLUDED.total_commission,
        net_profit = EXCLUDED.net_profit,
        roi_percent = EXCLUDED.roi_percent,
        win_rate = EXCLUDED.win_rate,
        updated_at = NOW();
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Trigger per aggiornamento automatico summary
DROP TRIGGER IF EXISTS trg_update_daily_summary ON trades;
CREATE TRIGGER trg_update_daily_summary
    AFTER INSERT OR UPDATE ON trades
    FOR EACH ROW
    WHEN (NEW.status = 'MATCHED' AND NEW.net_profit IS NOT NULL)
    EXECUTE FUNCTION update_daily_summary();

-- Grant permissions
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO aibetting_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO aibetting_user;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA public TO aibetting_user;

-- Commenti per documentazione
COMMENT ON TABLE trades IS 'Storico completo di tutti gli ordini piazzati sul sistema';
COMMENT ON TABLE daily_summaries IS 'Aggregati giornalieri per analisi ROI e performance';
COMMENT ON VIEW v_trading_stats IS 'Statistiche trading degli ultimi 30 giorni';
COMMENT ON VIEW v_daily_pnl IS 'P&L giornaliero con ROI';
