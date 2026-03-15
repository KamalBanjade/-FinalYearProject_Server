using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Data;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SecureMedicalRecordSystem.API.Controllers;

[ApiController]
[Route("api/doctor/statistics")]
[Authorize]
public class DoctorStatisticsController : ControllerBase
{
    private readonly IDoctorStatisticsService _statisticsService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DoctorStatisticsController> _logger;

    public DoctorStatisticsController(
        IDoctorStatisticsService statisticsService,
        ApplicationDbContext context,
        ILogger<DoctorStatisticsController> logger)
    {
        _statisticsService = statisticsService;
        _context = context;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardStatistics()
    {
        try
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            var stats = await _statisticsService.GetDashboardStatisticsAsync(doctorId);
            return Ok(stats);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard statistics");
            return StatusCode(500, new { message = "Error fetching statistics" });
        }
    }

    [HttpGet("today-schedule")]
    public async Task<IActionResult> GetTodaySchedule()
    {
        try 
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            var appointments = await _statisticsService.GetTodayScheduleAsync(doctorId);
            return Ok(appointments);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
    }

    [HttpGet("week-schedule")]
    public async Task<IActionResult> GetWeekSchedule()
    {
        try 
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            var schedule = await _statisticsService.GetWeekScheduleAsync(doctorId);
            return Ok(schedule);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
    }

    [HttpGet("patient-volume")]
    public async Task<IActionResult> GetPatientVolumeTrend()
    {
        try 
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            var trend = await _statisticsService.GetPatientVolumeTrendAsync(doctorId);
            return Ok(trend);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
    }

    [HttpGet("record-status")]
    public async Task<IActionResult> GetRecordStatusBreakdown()
    {
        try 
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            var breakdown = await _statisticsService.GetRecordStatusBreakdownAsync(doctorId);
            return Ok(breakdown);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
    }

    [HttpGet("recent-scans")]
    public async Task<IActionResult> GetRecentScans([FromQuery] int limit = 10)
    {
        try 
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            var scans = await _statisticsService.GetRecentScansAsync(doctorId, limit);
            return Ok(scans);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
    }

    [HttpGet("template-usage")]
    public async Task<IActionResult> GetTemplateUsage()
    {
        try 
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            var usage = await _statisticsService.GetTemplateUsageAsync(doctorId);
            return Ok(usage);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
    }

    [HttpGet("record-growth")]
    public async Task<IActionResult> GetRecordGrowthTrend()
    {
        try 
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            var trend = await _statisticsService.GetRecordGrowthTrendAsync(doctorId);
            return Ok(trend);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
    }

    private async Task<Guid> GetCurrentDoctorIdAsync()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out var userId))
            throw new UnauthorizedAccessException("User ID not found in token");
            
        var doctor = await _context.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == userId);
            
        if (doctor == null)
            throw new UnauthorizedAccessException("Logged in user is not a doctor");
            
        return doctor.Id;
    }
}
