using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureMedicalRecordSystem.Core.DTOs;
using SecureMedicalRecordSystem.Core.DTOs.Admin;
using SecureMedicalRecordSystem.Core.Interfaces;
using System.Security.Claims;

namespace SecureMedicalRecordSystem.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminPolicy")]
public class AdminController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAuditLogService _auditLogService;

    public AdminController(IAuthService authService, IAuditLogService auditLogService)
    {
        _authService = authService;
        _auditLogService = auditLogService;
    }

    [HttpPost("doctors/invite")]
    public async Task<IActionResult> InviteDoctor([FromBody] InviteDoctorRequestDTO request)
    {
        var adminUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminUserIdClaim) || !Guid.TryParse(adminUserIdClaim, out var adminUserId))
        {
            return Unauthorized(ApiResponse.FailureResult("Invalid admin session."));
        }

        var result = await _authService.InviteDoctorAsync(request, adminUserId);

        if (!result.Success)
        {
            if (result.Message.Contains("exists", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(ApiResponse.FailureResult(result.Message));
            }
            return BadRequest(ApiResponse.FailureResult(result.Message));
        }

        return StatusCode(201, ApiResponse.SuccessResult(result.Data, result.Message));
    }

    [HttpGet("doctors")]
    public async Task<IActionResult> GetAllDoctors(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? department = null,
        [FromQuery] bool? isActive = null)
    {
        var result = await _authService.GetAllDoctorsAsync(page, pageSize, searchTerm, department, isActive);
        return Ok(ApiResponse.SuccessResult(result.Data, result.Message));
    }

    [HttpGet("doctors/{id}")]
    public async Task<IActionResult> GetDoctorDetails(Guid id)
    {
        var result = await _authService.GetDoctorDetailsAsync(id);
        if (!result.Success) return NotFound(ApiResponse.FailureResult(result.Message));
        return Ok(ApiResponse.SuccessResult(result.Data, result.Message));
    }

    [HttpPut("doctors/{id}")]
    public async Task<IActionResult> UpdateDoctor(Guid id, [FromBody] UpdateDoctorRequestDTO request)
    {
        var result = await _authService.UpdateDoctorAsync(id, request);
        if (!result.Success) return BadRequest(ApiResponse.FailureResult(result.Message));
        return Ok(ApiResponse.SuccessResult((object?)null, result.Message));
    }

    [HttpDelete("doctors/{id}")]
    public async Task<IActionResult> DeleteDoctor(Guid id)
    {
        var result = await _authService.DeleteDoctorAsync(id);
        if (!result.Success) return BadRequest(ApiResponse.FailureResult(result.Message));
        return Ok(ApiResponse.SuccessResult((object?)null, result.Message));
    }

    [HttpPost("doctors/{id}/regenerate-keys")]
    public async Task<IActionResult> RotateKeys(Guid id)
    {
        var result = await _authService.RotateDoctorKeysAsync(id);
        if (!result.Success) return BadRequest(ApiResponse.FailureResult(result.Message));
        return Ok(ApiResponse.SuccessResult(result.Data, result.Message));
    }

    // ==========================================
    // USER MANAGEMENT
    // ==========================================

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? role = null,
        [FromQuery] bool? isActive = null)
    {
        var result = await _authService.GetAllUsersAsync(page, pageSize, searchTerm, role, isActive);
        return Ok(ApiResponse.SuccessResult(result.Data, result.Message));
    }

    [HttpPut("users/{id}/status")]
    public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] bool isActive)
    {
        var result = await _authService.UpdateUserStatusAsync(id, isActive);
        if (!result.Success) return BadRequest(ApiResponse.FailureResult(result.Message));
        return Ok(ApiResponse.SuccessResult((object?)null, result.Message));
    }

    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequestDTO request)
    {
        var result = await _authService.UpdateUserAsync(id, request);
        if (!result.Success) return BadRequest(ApiResponse.FailureResult(result.Message));
        return Ok(ApiResponse.SuccessResult((object?)null, result.Message));
    }

    // ==========================================
    // AUDIT LOG MANAGEMENT
    // ==========================================

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? action = null,
        [FromQuery] SecureMedicalRecordSystem.Core.Enums.AuditSeverity? severity = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var (logs, totalCount) = await _auditLogService.GetLogsAsync(page, pageSize, searchTerm, action, severity, fromDate, toDate);

        var response = new SecureMedicalRecordSystem.Core.DTOs.Admin.PaginatedAuditLogsDTO
        {
            Logs = logs,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse.SuccessResult(response, "Audit logs retrieved successfully."));
    }
}
