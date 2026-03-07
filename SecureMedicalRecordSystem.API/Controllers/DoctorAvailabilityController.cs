using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureMedicalRecordSystem.Core.DTOs;
using SecureMedicalRecordSystem.Core.DTOs.Appointments;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.Interfaces;
using System.Security.Claims;

namespace SecureMedicalRecordSystem.API.Controllers;

[ApiController]
[Route("api/doctor/availability")]
[Authorize(Policy = "DoctorPolicy")]
public class DoctorAvailabilityController : ControllerBase
{
    private readonly IDoctorAvailabilityService _availabilityService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DoctorAvailabilityController(
        IDoctorAvailabilityService availabilityService,
        UserManager<ApplicationUser> userManager)
    {
        _availabilityService = availabilityService;
        _userManager = userManager;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("schedule")]
    public async Task<ActionResult<ApiResponse<List<DoctorAvailabilityDTO>>>> GetSchedule([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var user = await _userManager.Users
            .Include(u => u.DoctorProfile)
            .FirstOrDefaultAsync(u => u.Id == GetUserId());

        if (user?.DoctorProfile == null)
            return BadRequest(ApiResponse<List<DoctorAvailabilityDTO>>.FailureResult("Doctor profile not found."));

        var result = await _availabilityService.GetDoctorScheduleAsync(user.DoctorProfile.Id, startDate, endDate);
        return Ok(ApiResponse<List<DoctorAvailabilityDTO>>.SuccessResult(result, "Schedule retrieved successfully."));
    }

    [HttpPost("working-hours")]
    public async Task<ActionResult<ApiResponse<object>>> SetWorkingHours(SetWorkingHoursDTO request)
    {
        var user = await _userManager.Users
            .Include(u => u.DoctorProfile)
            .FirstOrDefaultAsync(u => u.Id == GetUserId());

        if (user?.DoctorProfile == null)
            return BadRequest(ApiResponse<object>.FailureResult("Doctor profile not found."));

        if (!TimeSpan.TryParse(request.StartTime, out var start) || !TimeSpan.TryParse(request.EndTime, out var end))
            return BadRequest(ApiResponse<object>.FailureResult("Invalid time format. Use HH:mm."));

        var result = await _availabilityService.SetWorkingHoursAsync(user.DoctorProfile.Id, request.DayOfWeek, start, end);
        
        if (!result.Success)
            return BadRequest(ApiResponse<object>.FailureResult(result.Message));

        return Ok(ApiResponse<object>.SuccessResult(null, result.Message));
    }

    [HttpPost("block")]
    public async Task<ActionResult<ApiResponse<object>>> BlockTime(BlockTimeDTO request)
    {
        var user = await _userManager.Users
            .Include(u => u.DoctorProfile)
            .FirstOrDefaultAsync(u => u.Id == GetUserId());

        if (user?.DoctorProfile == null)
            return BadRequest(ApiResponse<object>.FailureResult("Doctor profile not found."));

        var result = await _availabilityService.BlockTimeAsync(user.DoctorProfile.Id, request.StartDateTime, request.EndDateTime, request.Reason);
        
        if (!result.Success)
            return BadRequest(ApiResponse<object>.FailureResult(result.Message));

        return Ok(ApiResponse<object>.SuccessResult(null, result.Message));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> UnblockTime(Guid id)
    {
        var result = await _availabilityService.UnblockTimeAsync(id, GetUserId());
        
        if (!result.Success)
            return BadRequest(ApiResponse<object>.FailureResult(result.Message));

        return Ok(ApiResponse<object>.SuccessResult(null, result.Message));
    }

    [HttpGet("slots/{doctorId}")]
    [AllowAnonymous] // Allow patients to see slots
    public async Task<ActionResult<ApiResponse<List<TimeSlotDTO>>>> GetSlots(Guid doctorId, [FromQuery] DateTime date, [FromQuery] int duration = 30)
    {
        var result = await _availabilityService.GetAvailableSlotsWithRulesAsync(doctorId, date, duration);
        return Ok(ApiResponse<List<TimeSlotDTO>>.SuccessResult(result, "Available slots retrieved successfully."));
    }
}
