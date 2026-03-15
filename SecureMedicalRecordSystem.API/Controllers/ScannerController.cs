using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SecureMedicalRecordSystem.API.Hubs;
using SecureMedicalRecordSystem.Core.DTOs;
using SecureMedicalRecordSystem.Core.DTOs.Scanner;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.Enums;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Data;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SecureMedicalRecordSystem.API.Controllers;

[ApiController]
[Route("api/scanner")]
public class ScannerController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<ScannerHub, IScannerHubClient> _hubContext;
    private readonly ITotpService _totpService;
    private readonly IAuditLogService _auditLogService;

    public ScannerController(
        ApplicationDbContext context,
        IHubContext<ScannerHub, IScannerHubClient> hubContext,
        ITotpService totpService,
        IAuditLogService auditLogService)
    {
        _context = context;
        _hubContext = hubContext;
        _totpService = totpService;
        _auditLogService = auditLogService;
    }

    [AllowAnonymous]
    [HttpPost("pair")]
    public async Task<IActionResult> PairMobileToDesktop([FromBody] PairRequestDTO request)
    {
        var desktop = await _context.DesktopSessions
            .Include(d => d.Doctor)
            .FirstOrDefaultAsync(d => d.SessionId == request.DesktopSessionId && d.IsActive);
        
        if (desktop == null)
            return BadRequest(ApiResponse.FailureResult("Invalid or expired desktop session"));

        var pairing = new MobileScannerPairing
        {
            MobileDeviceId = request.MobileDeviceId,
            DesktopSessionId = desktop.Id,
            DoctorId = desktop.DoctorId,
            DeviceName = request.DeviceName,
            PairedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsActive = true
        };
        
        await _context.MobileScannerPairings.AddAsync(pairing);
        
        // Notify desktop via WebSocket (all connections for this user)
        await _hubContext.Clients.User(desktop.Doctor.UserId.ToString())
            .MobilePaired(new {
                deviceName = request.DeviceName,
                pairedAt = DateTime.UtcNow
            });
        
        await _context.SaveChangesAsync();
        
        return Ok(ApiResponse.SuccessResult(null, "Paired successfully"));
    }

    [AllowAnonymous]
    [HttpPost("notify-patient-scanned")]
    public async Task<IActionResult> NotifyPatientScanned([FromBody] ScanRequestDTO request)
    {
        var desktop = await _context.DesktopSessions
            .Include(d => d.MobileScannerPairings)
            .Include(d => d.Doctor) // Ensure we have access to UserId
            .FirstOrDefaultAsync(d => d.SessionId == request.DesktopSessionId && d.IsActive);
        
        if (desktop == null)
            return BadRequest(ApiResponse.FailureResult("Desktop session not found or inactive"));

        // Validate mobile device is paired
        var isPaired = desktop.MobileScannerPairings.Any(m => 
            m.MobileDeviceId == request.MobileDeviceId && 
            m.IsActive && 
            m.ExpiresAt > DateTime.UtcNow);

        if (!isPaired)
            return Unauthorized(ApiResponse.FailureResult("Mobile device not paired to this session"));

        // Look up QR Token
        var patientToken = await _context.QRTokens
            .Include(t => t.Patient)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(t => t.Token == request.PatientToken && t.IsActive);

        if (patientToken == null || (patientToken.ExpiresAt.HasValue && patientToken.ExpiresAt < DateTime.UtcNow))
            return BadRequest(ApiResponse.FailureResult("Invalid or expired patient QR token"));

        // Update token access stats
        patientToken.AccessCount++;
        patientToken.LastAccessedAt = DateTime.UtcNow;

        var patient = patientToken.Patient;

        // Log scan attempt
        var scanLog = new ScanHistory
        {
            PatientId = patient.Id,
            DoctorId = desktop.DoctorId,
            DesktopSessionId = desktop.Id,
            MobileDeviceId = request.MobileDeviceId,
            ScannedAt = DateTime.UtcNow,
            TOTPVerified = patientToken.TokenType == QRTokenType.Emergency,
            AccessGranted = patientToken.TokenType == QRTokenType.Emergency,
            TokenType = patientToken.TokenType
        };
        await _context.ScanHistories.AddAsync(scanLog);

        if (patientToken.TokenType == QRTokenType.Emergency)
        {
            // Immediate Audit Log for Emergency
            var ipAddr = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            await _auditLogService.LogAsync(
                desktop.Doctor.UserId,
                "EMERGENCY SCAN ACCESS",
                $"Doctor accessed critical records for patient {patient.Id} via EMERGENCY QR scanner.",
                ipAddr,
                Request.Headers["User-Agent"].ToString() ?? "unknown",
                "Patient",
                patient.Id.ToString(),
                AuditSeverity.Critical);
        }

        await _context.SaveChangesAsync();

        // Calculate Age
        int age = DateTime.UtcNow.Year - patient.DateOfBirth.Year;
        if (patient.DateOfBirth.Date > DateTime.UtcNow.AddYears(-age)) age--;

        // Notify Desktop (all connections for this user)
        await _hubContext.Clients.User(desktop.Doctor.UserId.ToString())
            .PatientScanned(new {
                scanHistoryId = scanLog.Id,
                patient = new {
                    id = patient.Id,
                    name = $"{patient.User.FirstName} {patient.User.LastName}",
                    age = age,
                    gender = patient.Gender,
                    mrNumber = patient.MedicalRecordNumber,
                    patientId = patient.Id
                },
                requiresTOTP = (patientToken.TokenType == Core.Enums.QRTokenType.Normal) && patient.User.TOTPSetupCompleted,
                isEmergency = patientToken.TokenType == Core.Enums.QRTokenType.Emergency,
                scannedAt = DateTime.UtcNow
            });

        return Ok(ApiResponse.SuccessResult(null, "Patient scan sent to desktop"));
    }

    [Authorize(Roles = "Doctor")]
    [HttpPost("verify-totp")]
    public async Task<IActionResult> VerifyTotp([FromBody] DesktopVerifyTotpDTO request)
    {
        var doctorUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(doctorUserIdStr)) return Unauthorized();
        
        var patient = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == request.PatientId);

        if (patient == null)
            return NotFound(ApiResponse.FailureResult("Patient not found"));

        if (patient.User.TOTPSetupCompleted)
        {
            if (string.IsNullOrEmpty(patient.User.TOTPSecret))
                return BadRequest(ApiResponse.FailureResult("TOTP not configured for this patient."));

            var isTotpValid = _totpService.ValidateTotp(patient.User.TOTPSecret, request.TotpCode);
            if (!isTotpValid)
            {
                // Optionally log failure
                return BadRequest(ApiResponse.FailureResult("Invalid TOTP code."));
            }
        }

        // If provided, update ScanHistory
        if (!string.IsNullOrEmpty(request.DesktopSessionId))
        {
            var session = await _context.DesktopSessions
                .FirstOrDefaultAsync(s => s.SessionId == request.DesktopSessionId);
                
            if (session != null)
            {
                var recentScan = await _context.ScanHistories
                    .Where(s => s.DesktopSessionId == session.Id && s.PatientId == patient.Id)
                    .OrderByDescending(s => s.ScannedAt)
                    .FirstOrDefaultAsync();
                    
                if (recentScan != null && recentScan.ScannedAt.AddMinutes(5) > DateTime.UtcNow)
                {
                    recentScan.TOTPVerified = true;
                    recentScan.TOTPVerifiedAt = DateTime.UtcNow;
                    recentScan.AccessGranted = true;
                    await _context.SaveChangesAsync();
                }
            }
        }

        // Audit Log
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await _auditLogService.LogAsync(
            Guid.Parse(doctorUserIdStr),
            "DESKTOP SCAN VERIFIED",
            $"Doctor verified TOTP for patient {patient.Id} via mobile scanner handoff.",
            ipAddress,
            Request.Headers["User-Agent"].ToString() ?? "unknown",
            "Patient",
            patient.Id.ToString(),
            SecureMedicalRecordSystem.Core.Enums.AuditSeverity.Info);

        return Ok(ApiResponse.SuccessResult(null, "TOTP Verified successfully"));
    }
}
