using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureMedicalRecordSystem.Core.DTOs;
using SecureMedicalRecordSystem.Core.DTOs.Admin;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Core.Entities;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace SecureMedicalRecordSystem.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminPolicy")]
public class AdminController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAuditLogService _auditLogService;
    private readonly IImageStorageService _imageStorageService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(
        IAuthService authService, 
        IAuditLogService auditLogService,
        IImageStorageService imageStorageService,
        UserManager<ApplicationUser> userManager)
    {
        _authService = authService;
        _auditLogService = auditLogService;
        _imageStorageService = imageStorageService;
        _userManager = userManager;
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

        return Ok(ApiResponse.SuccessResult((object?)null, result.Message));
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

    [HttpGet("doctors/{id}/profile")]
    public async Task<IActionResult> GetDoctorExtendedProfile(Guid id)
    {
        var doctor = await _authService.GetDoctorEntityByIdAsync(id);
        if (doctor == null) return NotFound(ApiResponse.FailureResult("Doctor not found."));
        var profile = DoctorController.BuildExtendedProfileDTO(doctor);
        return Ok(ApiResponse.SuccessResult(profile, "Extended profile retrieved."));
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

    [HttpPost("patients")]
    public async Task<IActionResult> CreatePatient([FromBody] SecureMedicalRecordSystem.Core.DTOs.Auth.CreatePatientRequestDTO request)
    {
        var adminUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminUserIdClaim) || !Guid.TryParse(adminUserIdClaim, out var adminUserId))
        {
            return Unauthorized(ApiResponse.FailureResult("Invalid admin session."));
        }

        // Admin creates patient without a Primary Doctor initially
        var result = await _authService.CreatePatientAccountAsync(request, null, adminUserId);
        if (result.Success)
        {
            return Ok(ApiResponse.SuccessResult((object?)null, result.Message));
        }
        return BadRequest(ApiResponse.FailureResult(result.Message));
    }

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

    [HttpGet("dashboard/stats")]
    public async Task<IActionResult> GetSystemStatistics()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));
        }

        var stats = await _auditLogService.GetSystemStatisticsAsync(userId);
        return Ok(ApiResponse.SuccessResult(stats, "System statistics retrieved successfully."));
    }

    [HttpGet("security-alerts")]
    public async Task<IActionResult> GetSecurityAlerts()
    {
        var alerts = await _auditLogService.GetSecurityAlertsAsync();
        return Ok(ApiResponse.SuccessResult(alerts, $"{alerts.Count} security alert(s) retrieved."));
    }

    [HttpPost("apply-retention")]
    public async Task<IActionResult> ApplyRetentionPolicy([FromQuery] int retentionDays = 90)
    {
        if (retentionDays < 30)
            return BadRequest(ApiResponse.FailureResult("Minimum retention period is 30 days."));

        var deletedCount = await _auditLogService.ApplyRetentionPolicyAsync(retentionDays);
        return Ok(ApiResponse.SuccessResult(new { deletedCount, retentionDays },
            $"Retention policy applied. {deletedCount} old log entries removed."));
    }

    [HttpGet("export-logs")]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var (logs, _) = await _auditLogService.GetLogsAsync(1, int.MaxValue, searchTerm, action, null, fromDate, toDate);

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Id,Timestamp,User,Email,Action,Details,Severity,IPAddress,EntityType,EntityId");

        foreach (var log in logs)
        {
            csv.AppendLine(string.Join(",",
                EscapeCsv(log.Id.ToString()),
                EscapeCsv(log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")),
                EscapeCsv(log.UserName ?? "System"),
                EscapeCsv(log.UserEmail ?? ""),
                EscapeCsv(log.Action),
                EscapeCsv(log.Details),
                EscapeCsv(log.SeverityLabel),
                EscapeCsv(log.IPAddress),
                EscapeCsv(log.EntityType ?? ""),
                EscapeCsv(log.EntityId ?? "")
            ));
        }

        var fileName = $"audit_logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", fileName);
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    [HttpPost("profile/picture")]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized(ApiResponse.FailureResult("User not found"));

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(ApiResponse.FailureResult("User not found"));

        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse.FailureResult("No file uploaded"));

        // Delete old picture if exists
        if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
        {
            await _imageStorageService.DeleteImageAsync(user.ProfilePictureUrl);
        }

        using var stream = file.OpenReadStream();
        var imageUrl = await _imageStorageService.UploadImageAsync(stream, file.FileName, "profile-pictures");

        user.ProfilePictureUrl = imageUrl;
        await _userManager.UpdateAsync(user);

        return Ok(ApiResponse.SuccessResult(new { url = user.ProfilePictureUrl }, "Profile picture updated"));
    }

    [HttpDelete("profile/picture")]
    public async Task<IActionResult> DeleteProfilePicture()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized(ApiResponse.FailureResult("User not found"));

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(ApiResponse.FailureResult("User not found"));

        if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
        {
            await _imageStorageService.DeleteImageAsync(user.ProfilePictureUrl);
            user.ProfilePictureUrl = null;
            await _userManager.UpdateAsync(user);
        }

        return Ok(ApiResponse.SuccessResult((object?)null, "Profile picture deleted"));
    }
}
