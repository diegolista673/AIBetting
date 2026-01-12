using AIBettingCore.Models;
using AIBettingAnalyst.Models;
using Serilog;

namespace AIBettingAnalyst.Analyzers;

/// <summary>
/// Analyzes Weight of Money (WoM) - the distribution of volume across selections.
/// High back volume on a selection indicates market confidence.
/// </summary>
public class WeightOfMoneyAnalyzer
{
    /// <summary>
    /// Calculate WoM for all runners in a market snapshot.
    /// </summary>
    public List<WeightOfMoneyResult> Analyze(MarketSnapshot snapshot)
    {
        var results = new List<WeightOfMoneyResult>();
        
        // Calculate total market volumes
        decimal totalMarketBackVolume = 0;
        decimal totalMarketLayVolume = 0;
        
        foreach (var runner in snapshot.Runners)
        {
            var backVolume = runner.AvailableToBack.Sum(p => p.Size);
            var layVolume = runner.AvailableToLay.Sum(p => p.Size);
            
            totalMarketBackVolume += backVolume;
            totalMarketLayVolume += layVolume;
        }

        // Calculate percentage for each runner
        foreach (var runner in snapshot.Runners)
        {
            var backVolume = runner.AvailableToBack.Sum(p => p.Size);
            var layVolume = runner.AvailableToLay.Sum(p => p.Size);
            
            var backPercentage = totalMarketBackVolume > 0 
                ? (double)(backVolume / totalMarketBackVolume) * 100 
                : 0;
                
            var layPercentage = totalMarketLayVolume > 0 
                ? (double)(layVolume / totalMarketLayVolume) * 100 
                : 0;

            results.Add(new WeightOfMoneyResult
            {
                SelectionId = runner.SelectionId.Value,
                SelectionName = runner.RunnerName,
                TotalBackVolume = backVolume,
                TotalLayVolume = layVolume,
                BackPercentage = Math.Round(backPercentage, 2),
                LayPercentage = Math.Round(layPercentage, 2)
            });
        }

        return results.OrderByDescending(r => r.TotalBackVolume).ToList();
    }

    /// <summary>
    /// Detect "steam moves" - sudden volume spikes indicating insider information.
    /// </summary>
    public bool DetectSteamMove(WeightOfMoneyResult current, WeightOfMoneyResult? previous, double volumeThreshold = 30.0)
    {
        if (previous == null)
        {
            return false;
        }

        // Steam move: volume increased by more than threshold percentage
        if (previous.TotalBackVolume > 0)
        {
            var volumeIncrease = ((double)(current.TotalBackVolume - previous.TotalBackVolume) / (double)previous.TotalBackVolume) * 100;
            
            if (volumeIncrease > volumeThreshold)
            {
                Log.Information("🌊 Steam move detected on {Selection}: +{Increase:F1}% volume", 
                    current.SelectionName, volumeIncrease);
                return true;
            }
        }

        return false;
    }
}
