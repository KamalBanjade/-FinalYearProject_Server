using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureMedicalRecordSystem.Core.DTOs;
using SecureMedicalRecordSystem.Core.DTOs.QR;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.Enums;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Data;
using System.Security.Claims;

namespace SecureMedicalRecordSystem.API.Controllers;

[ApiController]
[Route("api/qr")]
public class QRCodeController : ControllerBase
{
    private readonly IQRTokenService _qrTokenService;
    private readonly IQRCodeGenerationService _qrCodeGenerationService;
    private readonly IAuditLogService _auditLogService;
    private readonly ApplicationDbContext _context;

    public QRCodeController(
        IQRTokenService qrTokenService,
        IQRCodeGenerationService qrCodeGenerationService,
        IAuditLogService auditLogService,
        ApplicationDbContext context)
    {
        _qrTokenService = qrTokenService;
        _qrCodeGenerationService = qrCodeGenerationService;
        _auditLogService = auditLogService;
        _context = context;
    }

    [HttpPost("generate-normal")]
    [Authorize(Policy = "PatientPolicy")]
    public async Task<IActionResult> GenerateNormalAccessQR([FromBody] GenerateQRRequestDTO request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var patient = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);
        if (patient == null) return NotFound(ApiResponse.FailureResult("Patient profile not found."));

        if (!patient.User.TOTPSetupCompleted)
        {
            return StatusCode(403, ApiResponse.FailureResult("Complete security setup is required before generating QR codes."));
        }

        var (token, expiresAt) = await _qrTokenService.GenerateNormalAccessTokenAsync(
            patient.Id, 
            request.ExpiryDays ?? 30);

        var accessUrl = _qrCodeGenerationService.BuildAccessUrl(token, QRTokenType.Normal);

        object qrData;
        if (request.Format.ToLower() == "svg")
        {
            qrData = new { qrCodeSvg = _qrCodeGenerationService.GenerateQRCodeSvg(accessUrl) };
        }
        else
        {
            var qrCodeBytes = _qrCodeGenerationService.GenerateQRCodeImage(accessUrl);
            qrData = new { qrCodeBase64 = Convert.ToBase64String(qrCodeBytes) };
        }

        await _auditLogService.LogAsync(
            userId,
            "Normal access QR code generated",
            "QR code expires " + expiresAt.ToString("g"),
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers["User-Agent"].ToString() ?? "unknown");

        return Ok(ApiResponse.SuccessResult(new
        {
            token,
            expiresAt,
            accessUrl,
            qrData
        }, "Normal access QR code generated successfully."));
    }

    [HttpPost("generate-emergency")]
    [Authorize(Policy = "PatientPolicy")]
    public async Task<IActionResult> GenerateEmergencyAccessQR([FromBody] GenerateQRRequestDTO request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var patient = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);
        if (patient == null) return NotFound(ApiResponse.FailureResult("Patient profile not found."));

        if (!patient.User.TOTPSetupCompleted)
        {
            return StatusCode(403, ApiResponse.FailureResult("Complete security setup is required before generating emergency QR codes."));
        }

        var (token, expiresAt) = await _qrTokenService.GenerateEmergencyAccessTokenAsync(
            patient.Id, 
            request.ExpiryDays ?? 365);

        var accessUrl = _qrCodeGenerationService.BuildAccessUrl(token, QRTokenType.Emergency);

        object qrData;
        if (request.Format.ToLower() == "svg")
        {
            qrData = new { qrCodeSvg = _qrCodeGenerationService.GenerateQRCodeSvg(accessUrl) };
        }
        else
        {
            var qrCodeBytes = _qrCodeGenerationService.GenerateQRCodeImage(accessUrl);
            qrData = new { qrCodeBase64 = Convert.ToBase64String(qrCodeBytes) };
        }

        await _auditLogService.LogAsync(
            userId,
            "Emergency access QR code generated",
            "QR code expires " + expiresAt.ToString("g"),
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers["User-Agent"].ToString() ?? "unknown");

        return Ok(ApiResponse.SuccessResult(new
        {
            token,
            expiresAt,
            accessUrl,
            qrData
        }, "Emergency access QR code generated successfully."));
    }

    [HttpGet("my-codes")]
    [Authorize(Policy = "PatientPolicy")]
    public async Task<IActionResult> GetMyQRCodes()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
        if (patient == null) return NotFound(ApiResponse.FailureResult("Patient profile not found."));

        var tokens = await _qrTokenService.GetPatientTokensAsync(patient.Id);

        var dto = tokens.Select(t => new QRCodeListItemDTO
        {
            Token = t.Token, // We return full token for management, frontend can truncate
            TokenType = t.TokenType.ToString(),
            CreatedAt = t.CreatedAt,
            ExpiresAt = t.ExpiresAt,
            IsExpired = t.ExpiresAt < DateTime.UtcNow,
            AccessCount = t.AccessCount,
            LastAccessedAt = t.LastAccessedAt
        }).ToList();

        return Ok(ApiResponse.SuccessResult(dto, "Your active QR codes retrieved successfully."));
    }

    [HttpPost("revoke/{token}")]
    [Authorize(Policy = "PatientPolicy")]
    public async Task<IActionResult> RevokeQRCode(string token)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
        if (patient == null) return NotFound(ApiResponse.FailureResult("Patient profile not found."));

        // Verify ownership
        var qrToken = await _context.QRTokens.FirstOrDefaultAsync(t => t.Token == token && t.PatientId == patient.Id);
        if (qrToken == null)
        {
            return NotFound(ApiResponse.FailureResult("QR code not found or unauthorized."));
        }

        var success = await _qrTokenService.RevokeTokenAsync(token);

        if (success)
        {
            await _auditLogService.LogAsync(
                userId,
                "QR code revoked by patient",
                $"Token: {token.Substring(0, Math.Min(8, token.Length))}...",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                Request.Headers["User-Agent"].ToString() ?? "unknown");

            return Ok(ApiResponse.SuccessResult((object?)null, "QR code revoked successfully."));
        }

        return BadRequest(ApiResponse.FailureResult("Failed to revoke QR code."));
    }
}
