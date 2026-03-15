using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureMedicalRecordSystem.Core.DTOs;
using SecureMedicalRecordSystem.Core.Interfaces;
using System.Security.Claims;

namespace SecureMedicalRecordSystem.API.Controllers;

[ApiController]
[Route("api/patient/statistics")]
[Authorize(Policy = "PatientPolicy")]
public class PatientStatisticsController : ControllerBase
{
    private readonly IPatientStatisticsService _statisticsService;

    public PatientStatisticsController(IPatientStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardStatistics()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));
        }

        var stats = await _statisticsService.GetDashboardStatisticsAsync(userId);
        return Ok(ApiResponse.SuccessResult(stats, "Patient dashboard statistics retrieved successfully."));
    }
}
