using AIBettingCore.Models;
using AIBettingAnalyst.Models;
using Serilog;

namespace AIBettingAnalyst.Analyzers;

/// <summary>
/// Calculates Weighted Average Price (WAP) from order book depth.
/// WAP provides a more accurate price representation than last matched price.
/// </summary>
public class WAPCalculator
{
    private readonly int _maxLevels;

    public WAPCalculator(int maxLevels = 3)
    {
        _maxLevels = maxLevels;
    }

    /// <summary>
    /// Calculate WAP for both back and lay sides of a runner.
    /// </summary>
    public WAPResult Calculate(RunnerSnapshot runner)
    {
        var backWAP = CalculateWAP(runner.AvailableToBack.ToList(), _maxLevels);
        var layWAP = CalculateWAP(runner.AvailableToLay.ToList(), _maxLevels);
        
        var totalBackSize = runner.AvailableToBack.Take(_maxLevels).Sum(p => p.Size);
        var totalLaySize = runner.AvailableToLay.Take(_maxLevels).Sum(p => p.Size);
        
        return new WAPResult
        {
            BackWAP = backWAP,
            LayWAP = layWAP,
            TotalBackSize = totalBackSize,
            TotalLaySize = totalLaySize,
            BackLevels = Math.Min(runner.AvailableToBack.Count, _maxLevels),
            LayLevels = Math.Min(runner.AvailableToLay.Count, _maxLevels)
        };
    }

    /// <summary>
    /// Calculate weighted average price from price-size ladder.
    /// Formula: WAP = Σ(Price_i × Size_i) / Σ(Size_i)
    /// </summary>
    private decimal CalculateWAP(List<PriceSize> priceLadder, int maxLevels)
    {
        if (priceLadder.Count == 0)
        {
            return 0;
        }

        decimal totalWeightedPrice = 0;
        decimal totalSize = 0;

        var levelsToUse = Math.Min(priceLadder.Count, maxLevels);
        
        for (int i = 0; i < levelsToUse; i++)
        {
            var priceSize = priceLadder[i];
            totalWeightedPrice += priceSize.Price * priceSize.Size;
            totalSize += priceSize.Size;
        }

        if (totalSize == 0)
        {
            return 0;
        }

        return Math.Round(totalWeightedPrice / totalSize, 2);
    }
}
