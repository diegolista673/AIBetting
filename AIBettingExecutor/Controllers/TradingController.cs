using Microsoft.AspNetCore.Mvc;
using AIBettingExecutor.SignalProcessing;

namespace AIBettingExecutor.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TradingController : ControllerBase
{
    private readonly SignalProcessor _signalProcessor;
    private readonly ILogger<TradingController> _logger;
    private static bool _isPaused = false;
    private static readonly object _lock = new object();

    public TradingController(SignalProcessor signalProcessor, ILogger<TradingController> logger)
    {
        _signalProcessor = signalProcessor;
        _logger = logger;
    }

    /// <summary>
    /// Get current trading status
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(TradingStatusResponse), 200)]
    public IActionResult GetStatus()
    {
        return Ok(new TradingStatusResponse
        {
            IsPaused = _isPaused,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Pause all trading activity
    /// </summary>
    [HttpPost("pause")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public IActionResult Pause()
    {
        lock (_lock)
        {
            if (_isPaused)
            {
                return BadRequest(new { error = "Trading is already paused" });
            }

            _isPaused = true;
            _logger.LogWarning("Trading PAUSED by API request");
            
            return Ok(new 
            { 
                message = "Trading paused successfully", 
                timestamp = DateTime.UtcNow,
                isPaused = true
            });
        }
    }

    /// <summary>
    /// Resume trading activity
    /// </summary>
    [HttpPost("resume")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public IActionResult Resume()
    {
        lock (_lock)
        {
            if (!_isPaused)
            {
                return BadRequest(new { error = "Trading is not paused" });
            }

            _isPaused = false;
            _logger.LogWarning("Trading RESUMED by API request");
            
            return Ok(new 
            { 
                message = "Trading resumed successfully", 
                timestamp = DateTime.UtcNow,
                isPaused = false
            });
        }
    }

    /// <summary>
    /// Check if trading is currently paused
    /// </summary>
    public static bool IsTradingPaused()
    {
        lock (_lock)
        {
            return _isPaused;
        }
    }
}

public class TradingStatusResponse
{
    public bool IsPaused { get; set; }
    public DateTime Timestamp { get; set; }
}
