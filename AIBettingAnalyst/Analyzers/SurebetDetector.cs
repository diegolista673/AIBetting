using AIBettingCore.Models;
using AIBettingAnalyst.Models;
using Serilog;

namespace AIBettingAnalyst.Analyzers;

/// <summary>
/// Detects surebet opportunities (arbitrage) by analyzing back/lay spreads.
/// A surebet exists when you can back high and lay low on the same selection.
/// </summary>
public class SurebetDetector
{
    private readonly decimal _minProfitPercent;
    private readonly decimal _minStake;
    private readonly decimal _maxStake;

    public SurebetDetector(
        decimal minProfitPercent = 0.5m, 
        decimal minStake = 10m, 
        decimal maxStake = 1000m)
    {
        _minProfitPercent = minProfitPercent;
        _minStake = minStake;
        _maxStake = maxStake;
    }

    /// <summary>
    /// Scan market for surebet opportunities.
    /// Surebet formula: (1/BackOdds + 1/LayOdds) < 1
    /// </summary>
    public List<SurebetOpportunity> DetectOpportunities(MarketSnapshot snapshot)
    {
        var opportunities = new List<SurebetOpportunity>();

        foreach (var runner in snapshot.Runners)
        {
            // Get best back and lay prices
            var bestBack = runner.AvailableToBack.FirstOrDefault();
            var bestLay = runner.AvailableToLay.FirstOrDefault();

            if (bestBack == null || bestLay == null)
            {
                continue;
            }

            // Surebet exists if lay odds < back odds (can buy low, sell high)
            if (bestLay.Price < bestBack.Price)
            {
                var opportunity = CalculateSurebet(
                    snapshot.MarketId.Value,
                    runner.SelectionId.Value,
                    runner.RunnerName,
                    bestBack,
                    bestLay
                );

                if (opportunity != null && opportunity.ProfitPercentage >= (double)_minProfitPercent)
                {
                    opportunities.Add(opportunity);
                    
                    Log.Information("💰 SUREBET FOUND! {Market} - {Selection}: Back {BackOdds} / Lay {LayOdds} = {Profit:F2}% profit",
                        snapshot.EventName,
                        runner.RunnerName,
                        bestBack.Price,
                        bestLay.Price,
                        opportunity.ProfitPercentage);
                }
            }
        }

        return opportunities;
    }

    /// <summary>
    /// Calculate surebet stakes and profit.
    /// </summary>
    private SurebetOpportunity? CalculateSurebet(
        string marketId,
        string selectionId,
        string selectionName,
        PriceSize back,
        PriceSize lay)
    {
        // Basic surebet calculation
        // Back stake: S1
        // Lay stake: S2 = S1 * BackOdds / LayOdds
        // Profit: S1 * (BackOdds - 1) - S2 * (LayOdds - 1)

        // Start with minimum stake
        var stakeBack = _minStake;
        var stakeLay = stakeBack * back.Price / lay.Price;

        // Check if we have enough liquidity
        if (stakeBack > back.Size || stakeLay > lay.Size)
        {
            // Reduce stakes to fit available liquidity
            var maxStakeByBackSize = Math.Min(back.Size, _maxStake);
            var maxStakeByLaySize = Math.Min(lay.Size * lay.Price / back.Price, _maxStake);
            
            stakeBack = Math.Min(maxStakeByBackSize, maxStakeByLaySize);
            stakeLay = stakeBack * back.Price / lay.Price;

            if (stakeBack < _minStake)
            {
                // Not enough liquidity
                return null;
            }
        }

        // Calculate profit
        var backProfit = stakeBack * (back.Price - 1);
        var layLiability = stakeLay * (lay.Price - 1);
        var netProfit = backProfit - layLiability;
        var profitPercentage = (double)(netProfit / stakeBack) * 100;

        return new SurebetOpportunity
        {
            MarketId = marketId,
            BackSelectionId = selectionId,
            BackSelectionName = selectionName,
            BackOdds = back.Price,
            BackSize = back.Size,
            LaySelectionId = selectionId, // Same selection for surebet
            LaySelectionName = selectionName,
            LayOdds = lay.Price,
            LaySize = lay.Size,
            ProfitPercentage = profitPercentage,
            StakeBack = Math.Round(stakeBack, 2),
            StakeLay = Math.Round(stakeLay, 2),
            ExpectedProfit = Math.Round(netProfit, 2)
        };
    }

    /// <summary>
    /// Calculate arbitrage percentage (lower is better).
    /// Formula: (1/BackOdds + 1/LayOdds) * 100
    /// < 100% = profitable arbitrage
    /// </summary>
    public double CalculateArbitragePercentage(decimal backOdds, decimal layOdds)
    {
        return (double)((1m / backOdds + 1m / layOdds) * 100m);
    }
}
