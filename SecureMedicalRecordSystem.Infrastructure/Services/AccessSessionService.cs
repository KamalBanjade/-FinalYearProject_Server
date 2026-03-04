using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureMedicalRecordSystem.Core.DTOs.QR;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.Enums;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Data;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class AccessSessionService : IAccessSessionService
{
    private readonly ApplicationDbContext _context;
    private readonly IQRTokenService _qrTokenService;
    private readonly ITotpService _totpService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AccessSessionService> _logger;

    public AccessSessionService(
        ApplicationDbContext context,
        IQRTokenService qrTokenService,
        ITotpService totpService,
        UserManager<ApplicationUser> userManager,
        IAuditLogService auditLogService,
        ILogger<AccessSessionService> logger)
    {
        _context = context;
        _qrTokenService = qrTokenService;
        _totpService = totpService;
        _userManager = userManager;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, AccessSessionDTO? Session)> CreateSessionAsync(
        string token, 
        string? totpCode,
        string ipAddress,
        string userAgent)
    {
        // 1. Validate QR token
        var (isValid, qrToken) = await _qrTokenService.ValidateTokenAsync(token);
        if (!isValid || qrToken == null)
            return (false, "Invalid or expired token", null);

        // 2. Get patient and user
        var patient = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == qrToken.PatientId);

        if (patient == null || patient.User == null)
            return (false, "Patient data not found", null);

        // 3. Verify TOTP if enabled (EXCEPT for Emergency access)
        if (qrToken.TokenType == QRTokenType.Normal && patient.User.TwoFactorEnabled)
        {
            if (string.IsNullOrEmpty(totpCode))
                return (false, "TOTP code is required for this patient", null);

            var isValidTotp = _totpService.ValidateTotp(
                patient.User.TOTPSecret ?? string.Empty,
                totpCode);
            
            if (!isValidTotp)
                return (false, "Invalid TOTP code", null);
        }
        else if (qrToken.TokenType == QRTokenType.Normal && !string.IsNullOrEmpty(totpCode))
        {
            return (false, "TOTP not enabled for this patient", null);
        }

        // 4. Create session
        var session = new AccessSession
        {
            Id = Guid.NewGuid(),
            SessionToken = GenerateSessionToken(),
            QRTokenId = qrToken.Id,
            PatientId = patient.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30), // 30 min session
            IPAddress = ipAddress,
            UserAgent = userAgent,
            IsActive = true
        };

        // 5. Save session
        await _context.AccessSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        // 6. Log access
        await _auditLogService.LogAsync(
            patient.UserId,
            "Medical records accessed via QR code",
            $"Session: {session.SessionToken[..Math.Min(8, session.SessionToken.Length)]}...",
            ipAddress,
            userAgent,
            "AccessSession",
            session.Id.ToString(),
            AuditSeverity.Info);

        // 7. Return session DTO
        return (true, "Session created", new AccessSessionDTO
        {
            SessionToken = session.SessionToken,
            PatientId = patient.Id,
            PatientName = $"{patient.User.FirstName} {patient.User.LastName}",
            TokenType = qrToken.TokenType.ToString(),
            ExpiresAt = session.ExpiresAt,
            RemainingMinutes = (int)(session.ExpiresAt - DateTime.UtcNow).TotalMinutes
        });
    }

    public async Task<(bool Success, AccessSessionDTO? Session)> ValidateSessionAsync(string sessionId)
    {
        // 1. Find session
        var session = await _context.AccessSessions
            .Include(s => s.QRToken)
            .Include(s => s.Patient)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(s => s.SessionToken == sessionId && s.IsActive);

        // 2. Validate session
        if (session == null)
            return (false, null);

        // 3. Check expiration
        if (session.ExpiresAt < DateTime.UtcNow)
        {
            session.IsActive = false;
            await _context.SaveChangesAsync();
            return (false, null);
        }

        // 4. Return session data
        return (true, new AccessSessionDTO
        {
            SessionToken = session.SessionToken,
            PatientId = session.PatientId,
            PatientName = $"{session.Patient.User.FirstName} {session.Patient.User.LastName}",
            TokenType = session.QRToken.TokenType.ToString(),
            ExpiresAt = session.ExpiresAt,
            RemainingMinutes = (int)(session.ExpiresAt - DateTime.UtcNow).TotalMinutes
        });
    }

    public async Task<bool> RevokeSessionAsync(string sessionId)
    {
        // 1. Find session
        var session = await _context.AccessSessions
            .FirstOrDefaultAsync(s => s.SessionToken == sessionId);

        // 2. Revoke
        if (session != null)
        {
            session.IsActive = false;
            await _context.SaveChangesAsync();
        }

        // 3. Return success
        return session != null;
    }

    public async Task CleanupExpiredSessionsAsync()
    {
        var expiredSessions = await _context.AccessSessions
            .Where(s => s.ExpiresAt < DateTime.UtcNow && s.IsActive)
            .ToListAsync();

        foreach (var session in expiredSessions)
        {
            session.IsActive = false;
        }

        await _context.SaveChangesAsync();
        
        if (expiredSessions.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired access sessions", expiredSessions.Count);
        }
    }

    private static string GenerateSessionToken()
    {
        var tokenBytes = new byte[32];
        RandomNumberGenerator.Fill(tokenBytes);
        return Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
