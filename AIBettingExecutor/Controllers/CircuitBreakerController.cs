using Microsoft.AspNetCore.Mvc;
using AIBettingCore.Interfaces;

namespace AIBettingExecutor.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CircuitBreakerController : ControllerBase
{
    private readonly IRiskManager _riskManager;
    private readonly ILogger<CircuitBreakerController> _logger;

    public CircuitBreakerController(IRiskManager riskManager, ILogger<CircuitBreakerController> logger)
    {
        _riskManager = riskManager;
        _logger = logger;
    }

    /// <summary>
    /// Get current circuit breaker status
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(CircuitBreakerStatusResponse), 200)]
    public async Task<IActionResult> GetStatus()
    {
        try
        {
            var isTriggered = await _riskManager.IsCircuitBreakerTriggeredAsync();
            
            return Ok(new CircuitBreakerStatusResponse
            {
                IsTriggered = isTriggered,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting circuit breaker status");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Reset the circuit breaker to allow trading to resume
    /// </summary>
    [HttpPost("reset")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Reset()
    {
        try
        {
            var wasTriggered = await _riskManager.IsCircuitBreakerTriggeredAsync();
            
            if (!wasTriggered)
            {
                return BadRequest(new { error = "Circuit breaker is not triggered" });
            }

            await _riskManager.ResetCircuitBreakerAsync();
            
            _logger.LogWarning("Circuit breaker manually reset by API request");
            
            return Ok(new { message = "Circuit breaker reset successfully", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting circuit breaker");
            return StatusCode(500, new { error = "Failed to reset circuit breaker" });
        }
    }

    /// <summary>
    /// Get circuit breaker configuration
    /// </summary>
    [HttpGet("config")]
    [ProducesResponseType(typeof(CircuitBreakerConfig), 200)]
    public IActionResult GetConfig()
    {
        // This would come from configuration - simplified for now
        return Ok(new CircuitBreakerConfig
        {
            Enabled = true,
            FailureThreshold = 5,
            WindowMinutes = 15
        });
    }
}

public class CircuitBreakerStatusResponse
{
    public bool IsTriggered { get; set; }
    public DateTime Timestamp { get; set; }
}

public class CircuitBreakerConfig
{
    public bool Enabled { get; set; }
    public int FailureThreshold { get; set; }
    public int WindowMinutes { get; set; }
}
