using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.Enums;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Data;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class QRTokenService : IQRTokenService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<QRTokenService> _logger;

    public QRTokenService(ApplicationDbContext context, ILogger<QRTokenService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(string Token, DateTime ExpiresAt)> GenerateNormalAccessTokenAsync(Guid patientId, int expiryDays = 30)
    {
        return await GenerateTokenInternalAsync(patientId, QRTokenType.Normal, expiryDays);
    }

    public async Task<(string Token, DateTime ExpiresAt)> GenerateEmergencyAccessTokenAsync(Guid patientId, int expiryDays = 365)
    {
        return await GenerateTokenInternalAsync(patientId, QRTokenType.Emergency, expiryDays);
    }

    public async Task<(bool IsValid, QRToken? TokenData)> ValidateTokenAsync(string token)
    {
        var qrToken = await _context.QRTokens
            .Include(t => t.Patient)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(t => t.Token == token);

        if (qrToken == null)
        {
            return (false, null);
        }

        if (!qrToken.IsActive)
        {
            return (false, null);
        }

        if (qrToken.ExpiresAt < DateTime.UtcNow)
        {
            // Token expired - deactivate automatically
            qrToken.IsActive = false;
            await _context.SaveChangesAsync();
            return (false, null);
        }

        // Update tracking metadata
        qrToken.AccessCount++;
        qrToken.LastAccessedAt = DateTime.UtcNow;
        
        // Log to ScanHistory for trend tracking unified with AccessCount
        var scanHistory = new ScanHistory
        {
            PatientId = qrToken.PatientId,
            ScannedAt = DateTime.UtcNow,
            TokenType = qrToken.TokenType,
            AccessGranted = true, // If we reached here, token is valid
            TOTPVerified = qrToken.TokenType == QRTokenType.Emergency
        };
        _context.ScanHistories.Add(scanHistory);
        
        await _context.SaveChangesAsync();

        return (true, qrToken);
    }

    public async Task<bool> RevokeTokenAsync(string token)
    {
        var qrToken = await _context.QRTokens.FirstOrDefaultAsync(t => t.Token == token);
        if (qrToken == null)
        {
            return false;
        }

        qrToken.IsActive = false;
        await _context.SaveChangesAsync();
        
        _logger.LogWarning("QR token {TokenPrefix}... revoked for patient {PatientId}", 
            token[..Math.Min(8, token.Length)], qrToken.PatientId);

        return true;
    }

    public async Task<List<QRToken>> GetPatientTokensAsync(Guid patientId)
    {
        return await _context.QRTokens
            .AsNoTracking()
            .Where(t => t.PatientId == patientId && t.IsActive)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    private async Task<(string Token, DateTime ExpiresAt)> GenerateTokenInternalAsync(Guid patientId, QRTokenType type, int expiryDays)
    {
        // 1. Generate cryptographically secure token (256 bits)
        var tokenBytes = new byte[32];
        RandomNumberGenerator.Fill(tokenBytes);
        
        // 2. Convert to URL-safe Base64
        string token = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        var expiresAt = DateTime.UtcNow.AddDays(expiryDays);

        // 3. Create and save entity
        var qrToken = new QRToken
        {
            PatientId = patientId,
            Token = token,
            TokenType = type,
            ExpiresAt = expiresAt,
            IsActive = true,
            AccessCount = 0
        };

        await _context.QRTokens.AddAsync(qrToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation("{Type} access token generated for patient {PatientId}, expires {ExpiresAt}", 
            type, patientId, expiresAt);

        return (token, expiresAt);
    }
}
