using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureMedicalRecordSystem.Core.Interfaces;
using System.Security.Claims;

namespace SecureMedicalRecordSystem.API.Controllers;

[ApiController]
[Route("api/alerts")]
[Authorize]
public class AlertsController : ControllerBase
{
    private readonly IStabilityAlertService _alertService;

    public AlertsController(IStabilityAlertService alertService)
    {
        _alertService = alertService;
    }

    [HttpGet("unread")]
    public async Task<IActionResult> GetUnreadAlerts()
    {
        var doctorIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(doctorIdString)) return Unauthorized();
        
        var doctorId = Guid.Parse(doctorIdString);
        var alerts = await _alertService.GetUnreadAlertsForDoctorAsync(doctorId);
        return Ok(alerts);
    }

    [HttpGet("unread/count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var doctorIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(doctorIdString)) return Unauthorized();

        var doctorId = Guid.Parse(doctorIdString);
        var count = await _alertService.GetUnreadAlertCountAsync(doctorId);
        return Ok(new { count });
    }

    [HttpPatch("{alertId}/read")]
    public async Task<IActionResult> MarkAsRead(Guid alertId)
    {
        var doctorIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(doctorIdString)) return Unauthorized();

        var doctorId = Guid.Parse(doctorIdString);
        await _alertService.MarkAlertAsReadAsync(alertId, doctorId);
        return NoContent();
    }
}
