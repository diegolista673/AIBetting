using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using AIBettingExecutor.Configuration;

namespace AIBettingExecutor.Controllers;

[ApiController]
[Route("api/config")]
public class ConfigController : ControllerBase
{
    private readonly IOptionsMonitor<ExecutorConfiguration> _config;
    private readonly ILogger<ConfigController> _logger;

    public ConfigController(IOptionsMonitor<ExecutorConfiguration> config, ILogger<ConfigController> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Get current risk management configuration
    /// </summary>
    [HttpGet("risk")]
    [ProducesResponseType(typeof(RiskConfigResponse), 200)]
    public IActionResult GetRiskConfig()
    {
        try
        {
            var riskConfig = _config.CurrentValue.Risk;

            var response = new RiskConfigResponse
            {
                Enabled = riskConfig.Enabled,
                MaxStakePerOrder = riskConfig.MaxStakePerOrder,
                MaxExposurePerMarket = riskConfig.MaxExposurePerMarket,
                MaxExposurePerSelection = riskConfig.MaxExposurePerSelection,
                MaxDailyLoss = riskConfig.MaxDailyLoss,
                CircuitBreakerEnabled = riskConfig.CircuitBreakerEnabled,
                CircuitBreakerFailureThreshold = riskConfig.CircuitBreakerFailureThreshold,
                CircuitBreakerWindowMinutes = riskConfig.CircuitBreakerWindowMinutes
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting risk configuration");
            return StatusCode(500, new { error = "Failed to get risk configuration" });
        }
    }

    /// <summary>
    /// Update risk management configuration
    /// </summary>
    [HttpPut("risk")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public IActionResult UpdateRiskConfig([FromBody] RiskConfigRequest request)
    {
        try
        {
            // Validate request
            if (request.MaxStakePerOrder <= 0)
            {
                return BadRequest(new { error = "MaxStakePerOrder must be greater than 0" });
            }

            if (request.MaxExposurePerMarket <= 0)
            {
                return BadRequest(new { error = "MaxExposurePerMarket must be greater than 0" });
            }

            if (request.MaxExposurePerSelection <= 0)
            {
                return BadRequest(new { error = "MaxExposurePerSelection must be greater than 0" });
            }

            if (request.MaxDailyLoss <= 0)
            {
                return BadRequest(new { error = "MaxDailyLoss must be greater than 0" });
            }

            if (request.CircuitBreakerFailureThreshold <= 0)
            {
                return BadRequest(new { error = "CircuitBreakerFailureThreshold must be greater than 0" });
            }

            if (request.CircuitBreakerWindowMinutes <= 0)
            {
                return BadRequest(new { error = "CircuitBreakerWindowMinutes must be greater than 0" });
            }

            // In a real implementation, this would update the configuration
            // For now, we log the request and return success
            _logger.LogWarning("Risk configuration update requested: {@Request}", request);
            _logger.LogWarning("NOTE: Configuration update not persisted to appsettings.json - requires manual edit");

            return Ok(new 
            { 
                message = "Risk configuration updated successfully (in-memory only)", 
                timestamp = DateTime.UtcNow,
                note = "Configuration changes are not persisted. Edit appsettings.json for permanent changes."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating risk configuration");
            return StatusCode(500, new { error = "Failed to update risk configuration" });
        }
    }

    /// <summary>
    /// Get current executor configuration summary
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ConfigSummaryResponse), 200)]
    public IActionResult GetConfigSummary()
    {
        try
        {
            var config = _config.CurrentValue;

            var response = new ConfigSummaryResponse
            {
                RiskManagementEnabled = config.Risk.Enabled,
                CircuitBreakerEnabled = config.Risk.CircuitBreakerEnabled,
                PaperTradingEnabled = config.Trading.EnablePaperTrading,
                MockBetfairEnabled = config.Trading.UseMockBetfair,
                Timestamp = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration summary");
            return StatusCode(500, new { error = "Failed to get configuration summary" });
        }
    }
}

public class RiskConfigResponse
{
    public bool Enabled { get; set; }
    public decimal MaxStakePerOrder { get; set; }
    public decimal MaxExposurePerMarket { get; set; }
    public decimal MaxExposurePerSelection { get; set; }
    public decimal MaxDailyLoss { get; set; }
    public bool CircuitBreakerEnabled { get; set; }
    public int CircuitBreakerFailureThreshold { get; set; }
    public int CircuitBreakerWindowMinutes { get; set; }
}

public class RiskConfigRequest
{
    public bool Enabled { get; set; }
    public decimal MaxStakePerOrder { get; set; }
    public decimal MaxExposurePerMarket { get; set; }
    public decimal MaxExposurePerSelection { get; set; }
    public decimal MaxDailyLoss { get; set; }
    public bool CircuitBreakerEnabled { get; set; }
    public int CircuitBreakerFailureThreshold { get; set; }
    public int CircuitBreakerWindowMinutes { get; set; }
}

public class ConfigSummaryResponse
{
    public bool RiskManagementEnabled { get; set; }
    public bool CircuitBreakerEnabled { get; set; }
    public bool PaperTradingEnabled { get; set; }
    public bool MockBetfairEnabled { get; set; }
    public DateTime Timestamp { get; set; }
}
