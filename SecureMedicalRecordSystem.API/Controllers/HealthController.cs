using Microsoft.AspNetCore.Mvc;
using SecureMedicalRecordSystem.Core.DTOs;

namespace SecureMedicalRecordSystem.API.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok(ApiResponse<object>.SuccessResult(new { status = "healthy", timestamp = DateTime.UtcNow }));
    }

    [HttpGet("ready")]
    public IActionResult GetReady()
    {
        // Add database/SMTP checks here in production
        return Ok(ApiResponse<object>.SuccessResult(new { status = "ready" }));
    }

    [HttpGet("live")]
    public IActionResult GetLive()
    {
        return Ok(ApiResponse<object>.SuccessResult(new { status = "live" }));
    }
}
