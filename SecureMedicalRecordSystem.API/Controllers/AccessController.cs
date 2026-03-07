using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureMedicalRecordSystem.Core.DTOs;
using SecureMedicalRecordSystem.Core.DTOs.QR;
using SecureMedicalRecordSystem.Core.Enums;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Data;

namespace SecureMedicalRecordSystem.API.Controllers;

[ApiController]
[Route("api/access")]
[AllowAnonymous]
public class AccessController : ControllerBase
{
    private readonly IAccessSessionService _accessSessionService;
    private readonly IMedicalRecordsService _medicalRecordsService;
    private readonly IQRTokenService _qrTokenService;
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public AccessController(
        IAccessSessionService accessSessionService,
        IMedicalRecordsService medicalRecordsService,
        IQRTokenService qrTokenService,
        ApplicationDbContext context,
        IAuditLogService auditLogService)
    {
        _accessSessionService = accessSessionService;
        _medicalRecordsService = medicalRecordsService;
        _qrTokenService = qrTokenService;
        _context = context;
        _auditLogService = auditLogService;
    }

    [HttpGet("{token}")]
    public async Task<IActionResult> GetAccessInfo(string token)
    {
        var (isValid, qrToken) = await _qrTokenService.ValidateTokenAsync(token);
        if (!isValid || qrToken == null)
            return NotFound(ApiResponse.FailureResult("Invalid or expired QR code"));

        var patient = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == qrToken.PatientId);

        if (patient == null || patient.User == null)
            return NotFound(ApiResponse.FailureResult("Patient info not found"));

        var isEmergency = qrToken.TokenType == QRTokenType.Emergency;
        var requiresTotp = !isEmergency && patient.User.TwoFactorEnabled;

        return Ok(ApiResponse.SuccessResult(new
        {
            patientName = $"{patient.User.FirstName} {patient.User.LastName}",
            requiresTotp = requiresTotp,
            isEmergency = isEmergency,
            message = isEmergency
                ? "Emergency access: No authentication required for critical info."
                : (requiresTotp ? "Please enter your TOTP code to access records" : "Click continue to access records")
        }, "QR token validated."));
    }

    [HttpGet("emergency/{token}")]
    public async Task<IActionResult> GetEmergencyAccess(string token)
    {
        // 1. Validate emergency token
        var (isValid, qrToken) = await _qrTokenService.ValidateTokenAsync(token);
        if (!isValid || qrToken == null)
            return NotFound(ApiResponse.FailureResult("Invalid or expired emergency QR code"));

        // 2. Verify token type
        if (qrToken.TokenType != QRTokenType.Emergency)
            return BadRequest(ApiResponse.FailureResult("This is not an emergency access token"));

        // 3. Get patient emergency data ONLY
        var patient = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == qrToken.PatientId);

        if (patient == null)
            return NotFound(ApiResponse.FailureResult("Patient info not found"));

        // 4. Build emergency response (CRITICAL INFO ONLY)
        var emergencyData = new EmergencyAccessDTO
        {
            PatientName = $"{patient.User.FirstName} {patient.User.LastName}",
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender,
            BloodType = patient.BloodType ?? "Unknown",
            Allergies = patient.Allergies ?? "None listed",
            CurrentMedications = patient.CurrentMedications ?? "None listed",
            ChronicConditions = patient.ChronicConditions ?? "None listed",
            EmergencyContact = new EmergencyContactDTO
            {
                Name = patient.EmergencyContactName,
                Phone = patient.EmergencyContactPhone,
                Relationship = patient.EmergencyContactRelationship
            },
            NotesToResponders = patient.EmergencyNotesToResponders,
            LastUpdated = patient.EmergencyDataLastUpdated
        };

        // 5. Log emergency access
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = Request.Headers["User-Agent"].ToString() ?? "Unknown";

        await _auditLogService.LogAsync(
            patient.UserId,
            "EMERGENCY ACCESS - QR code scanned",
            $"Emergency info accessed. IP: {ipAddress}, User-Agent: {userAgent}",
            ipAddress,
            userAgent,
            "QRToken",
            qrToken.Id.ToString(),
            AuditSeverity.Warning);

        // 6. Return emergency data
        return Ok(ApiResponse.SuccessResult(new
        {
            data = emergencyData,
            accessType = "EMERGENCY",
            timestamp = DateTime.UtcNow
        }, "Emergency medical information retrieved successfully."));
    }

    [HttpPost("{token}/verify")]
    public async Task<IActionResult> VerifyAndCreateSession(string token, [FromBody] VerifyAccessDTO dto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers["User-Agent"].ToString() ?? "unknown";

        var (success, message, session) = await _accessSessionService.CreateSessionAsync(
            token,
            dto.TotpCode,
            ipAddress,
            userAgent);

        if (!success || session == null)
        {
            return BadRequest(ApiResponse.FailureResult(message));
        }

        return Ok(ApiResponse.SuccessResult(new
        {
            sessionToken = session.SessionToken,
            expiresAt = session.ExpiresAt,
            remainingMinutes = session.RemainingMinutes
        }, message));
    }

    [HttpGet("session/records")]
    public async Task<IActionResult> GetSessionRecords([FromHeader(Name = "X-Session-Token")] string? sessionToken)
    {
        if (string.IsNullOrEmpty(sessionToken))
            return Unauthorized(ApiResponse.FailureResult("Session token missing."));

        var (isValid, session) = await _accessSessionService.ValidateSessionAsync(sessionToken);
        if (!isValid || session == null)
            return Unauthorized(ApiResponse.FailureResult("Invalid or expired session"));

        if (session.TokenType == QRTokenType.Emergency.ToString())
        {
            var patient = await _context.Patients
                .Include(p => p.User)
                .Include(p => p.PrimaryDoctor)
                    .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(p => p.Id == session.PatientId);

            if (patient == null) return NotFound(ApiResponse.FailureResult("Patient data not found."));

            return Ok(ApiResponse.SuccessResult(new
            {
                patientName = session.PatientName,
                sessionExpiresAt = session.ExpiresAt,
                remainingMinutes = session.RemainingMinutes,
                isEmergencyAccess = true,
                criticalInfo = new
                {
                    patient.BloodType,
                    patient.Allergies,
                    patient.ChronicConditions,
                    patient.CurrentMedications,
                    patient.EmergencyContactName,
                    patient.EmergencyContactPhone,
                    primaryDoctor = patient.PrimaryDoctor != null 
                        ? $"Dr. {patient.PrimaryDoctor.User.FirstName} {patient.PrimaryDoctor.User.LastName}"
                        : "Not assigned"
                },
                records = new List<object>() // No full records for emergency
            }, "Emergency critical information retrieved successfully."));
        }

        var result = await _medicalRecordsService.GetPatientRecordsAsync(session.PatientId, session.PatientId);
        
        return Ok(ApiResponse.SuccessResult(new
        {
            patientName = session.PatientName,
            sessionExpiresAt = session.ExpiresAt,
            remainingMinutes = session.RemainingMinutes,
            records = result.Data
        }, "Records retrieved successfully."));
    }

    [HttpGet("session/records/{id}/download")]
    public async Task<IActionResult> DownloadSessionRecord(Guid id, [FromHeader(Name = "X-Session-Token")] string? sessionToken)
    {
        if (string.IsNullOrEmpty(sessionToken))
            return Unauthorized(ApiResponse.FailureResult("Session token missing."));

        var (isValid, session) = await _accessSessionService.ValidateSessionAsync(sessionToken);
        if (!isValid || session == null)
            return Unauthorized(ApiResponse.FailureResult("Invalid or expired session"));

        if (session.TokenType == QRTokenType.Emergency.ToString())
        {
            return Forbid("File downloads are not permitted during Emergency access sessions.");
        }

        var result = await _medicalRecordsService.StreamDownloadRecordAsync(id, session.PatientId);
        if (!result.Success)
            return BadRequest(ApiResponse.FailureResult(result.Message));

        return File(result.FileStream!, result.ContentType!, result.FileName);
    }

    [HttpPost("session/logout")]
    public async Task<IActionResult> LogoutSession([FromHeader(Name = "X-Session-Token")] string? sessionToken)
    {
        if (string.IsNullOrEmpty(sessionToken))
            return BadRequest(ApiResponse.FailureResult("Session token missing."));

        await _accessSessionService.RevokeSessionAsync(sessionToken);
        return Ok(ApiResponse.SuccessResult((object?)null, "Session ended"));
    }
}
