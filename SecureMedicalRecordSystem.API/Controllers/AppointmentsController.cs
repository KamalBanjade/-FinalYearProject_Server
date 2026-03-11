using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureMedicalRecordSystem.Core.DTOs;
using SecureMedicalRecordSystem.Core.DTOs.Appointments;
using SecureMedicalRecordSystem.Core.DTOs.MedicalRecords;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.Interfaces;
using System.Security.Claims;

namespace SecureMedicalRecordSystem.API.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(
        IAppointmentService appointmentService,
        UserManager<ApplicationUser> userManager,
        ILogger<AppointmentsController> logger)
    {
        _appointmentService = appointmentService;
        _userManager = userManager;
        _logger = logger;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("request")]
    [Authorize(Policy = "PatientPolicy")]
    public async Task<ActionResult<ApiResponse<AppointmentDTO>>> RequestAppointment(CreateAppointmentDTO request)
    {
        var result = await _appointmentService.CreateAppointmentAsync(request, GetUserId());
        if (!result.Success)
            return BadRequest(ApiResponse<AppointmentDTO>.FailureResult(result.Message));

        return Ok(ApiResponse<AppointmentDTO>.SuccessResult(result.Data, result.Message));
    }

    [HttpGet("patient")]
    [Authorize(Policy = "PatientPolicy")]
    public async Task<ActionResult<ApiResponse<List<AppointmentDTO>>>> GetPatientAppointments([FromQuery] bool includeHistory = false)
    {
        var result = await _appointmentService.GetPatientAppointmentsAsync(GetUserId(), GetUserId(), includeHistory);
        if (!result.Success)
            return BadRequest(ApiResponse<List<AppointmentDTO>>.FailureResult(result.Message));

        return Ok(ApiResponse<List<AppointmentDTO>>.SuccessResult(result.Data, result.Message));
    }

    [HttpGet("doctor")]
    [Authorize(Policy = "DoctorPolicy")]
    public async Task<ActionResult<ApiResponse<List<AppointmentDTO>>>> GetDoctorAppointments([FromQuery] DateTime? date = null, [FromQuery] bool includeHistory = false)
    {
        var user = await _userManager.Users
            .Include(u => u.DoctorProfile)
            .FirstOrDefaultAsync(u => u.Id == GetUserId());

        if (user?.DoctorProfile == null)
            return BadRequest(ApiResponse<List<AppointmentDTO>>.FailureResult("Doctor profile not found."));

        var result = await _appointmentService.GetDoctorAppointmentsAsync(user.DoctorProfile.Id, GetUserId(), date, includeHistory);
        if (!result.Success)
            return BadRequest(ApiResponse<List<AppointmentDTO>>.FailureResult(result.Message));

        return Ok(ApiResponse<List<AppointmentDTO>>.SuccessResult(result.Data, result.Message));
    }

    [HttpGet("doctor/stats")]
    [Authorize(Policy = "DoctorPolicy")]
    public async Task<ActionResult<ApiResponse<DoctorAppointmentStatsDTO>>> GetDoctorStats()
    {
        var user = await _userManager.Users
            .Include(u => u.DoctorProfile)
            .FirstOrDefaultAsync(u => u.Id == GetUserId());

        if (user?.DoctorProfile == null)
            return BadRequest(ApiResponse<DoctorAppointmentStatsDTO>.FailureResult("Doctor profile not found."));

        var result = await _appointmentService.GetDoctorStatsAsync(user.DoctorProfile.Id, GetUserId());
        if (!result.Success)
            return BadRequest(ApiResponse<DoctorAppointmentStatsDTO>.FailureResult(result.Message));

        return Ok(ApiResponse<DoctorAppointmentStatsDTO>.SuccessResult(result.Data, result.Message));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AppointmentDTO>>> GetAppointment(Guid id)
    {
        var result = await _appointmentService.GetAppointmentByIdAsync(id, GetUserId());
        if (!result.Success)
            return NotFound(ApiResponse<AppointmentDTO>.FailureResult(result.Message));

        return Ok(ApiResponse<AppointmentDTO>.SuccessResult(result.Data, result.Message));
    }

    [HttpPut("{id}/cancel")]
    public async Task<ActionResult<ApiResponse<object>>> CancelAppointment(Guid id, CancelAppointmentDTO request)
    {
        var result = await _appointmentService.CancelAppointmentAsync(id, request.CancellationReason, GetUserId());
        if (!result.Success)
            return BadRequest(ApiResponse<object>.FailureResult(result.Message));

        return Ok(ApiResponse<object>.SuccessResult(null, result.Message));
    }

    [HttpPut("{id}/reschedule")]
    public async Task<ActionResult<ApiResponse<AppointmentDTO>>> RescheduleAppointment(Guid id, RescheduleAppointmentDTO request)
    {
        var result = await _appointmentService.RescheduleAppointmentAsync(id, request.NewAppointmentDate, GetUserId());
        if (!result.Success)
            return BadRequest(ApiResponse<AppointmentDTO>.FailureResult(result.Message));

        return Ok(ApiResponse<AppointmentDTO>.SuccessResult(result.Data, result.Message));
    }

    [HttpPut("{id}/complete")]
    [Authorize(Policy = "DoctorPolicy")]
    public async Task<ActionResult<ApiResponse<object>>> CompleteAppointment(Guid id, CompleteAppointmentDTO request)
    {
        var result = await _appointmentService.CompleteAppointmentAsync(id, request.ConsultationNotes, GetUserId());
        if (!result.Success)
            return BadRequest(ApiResponse<object>.FailureResult(result.Message));

        return Ok(ApiResponse<object>.SuccessResult(null, result.Message));
    }

    [HttpPut("{id}/confirm")]
    [Authorize(Policy = "DoctorPolicy")]
    public async Task<ActionResult<ApiResponse<object>>> ConfirmAppointment(Guid id)
    {
        var result = await _appointmentService.ConfirmAppointmentAsync(id, GetUserId());
        if (!result.Success)
            return BadRequest(ApiResponse<object>.FailureResult(result.Message));

        return Ok(ApiResponse<object>.SuccessResult(null, result.Message));
    }

    [HttpPost("{id}/link-record")]
    [Authorize(Policy = "DoctorPolicy")]
    public async Task<ActionResult<ApiResponse<object>>> LinkRecord(Guid id, LinkRecordDTO request)
    {
        var result = await _appointmentService.LinkRecordToAppointmentAsync(id, request.MedicalRecordId, request.Notes ?? "", GetUserId());
        if (!result.Success)
            return BadRequest(ApiResponse<object>.FailureResult(result.Message));

        return Ok(ApiResponse<object>.SuccessResult(null, result.Message));
    }

    [HttpGet("availability/{doctorId}")]
    public async Task<ActionResult<ApiResponse<List<TimeSlotDTO>>>> GetAvailability(Guid doctorId, [FromQuery] DateTime date)
    {
        var slots = await _appointmentService.GetDoctorAvailableSlotsAsync(doctorId, date);
        return Ok(ApiResponse<List<TimeSlotDTO>>.SuccessResult(slots, "Availability retrieved successfully"));
    }

    [HttpGet("smart-suggestions")]
    [Authorize(Policy = "PatientPolicy")]
    public async Task<ActionResult<ApiResponse<SmartDoctorSuggestionDTO>>> GetSmartSuggestions()
    {
        var result = await _appointmentService.GetSmartDoctorSuggestionsAsync(GetUserId());
        if (!result.Success)
            return BadRequest(ApiResponse<SmartDoctorSuggestionDTO>.FailureResult(result.Message));

        return Ok(ApiResponse<SmartDoctorSuggestionDTO>.SuccessResult(result.Data, result.Message));
    }

    [HttpGet("suggest-by-reason")]
    public async Task<ActionResult<ApiResponse<List<DoctorSuggestionItem>>>> SuggestByReason([FromQuery] string reason)
    {
        var result = await _appointmentService.SuggestDoctorsByReasonAsync(reason);
        if (!result.Success)
            return BadRequest(ApiResponse<List<DoctorSuggestionItem>>.FailureResult(result.Message));

        return Ok(ApiResponse<List<DoctorSuggestionItem>>.SuccessResult(result.Data, result.Message));
    }
}
