using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureMedicalRecordSystem.Core.DTOs.Auth;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text.Json;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class TrustedDeviceService : ITrustedDeviceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TrustedDeviceService> _logger;
    private readonly IEmailService _emailService;

    public TrustedDeviceService(
        ApplicationDbContext context, 
        ILogger<TrustedDeviceService> logger,
        IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
    }

    public Task<string?> GetDeviceTokenFromRequest(string? cookieDeviceToken)
    {
        if (string.IsNullOrWhiteSpace(cookieDeviceToken) || cookieDeviceToken.Length < 32)
            return Task.FromResult<string?>(null);

        return Task.FromResult<string?>(cookieDeviceToken);
    }

    public async Task<bool> IsDeviceTrustedAsync(string deviceToken, Guid userId)
    {
        var device = await _context.TrustedDevices
            .FirstOrDefaultAsync(d => 
                d.DeviceToken == deviceToken &&
                d.UserId == userId &&
                d.IsActive);

        if (device == null)
            return false;

        if (device.ExpiresAt < DateTime.UtcNow)
        {
            // Expired - mark as inactive
            device.IsActive = false;
            device.RevokedAt = DateTime.UtcNow;
            device.RevokedReason = "Expired after 30 days";
            await _context.SaveChangesAsync();
            
            return false;
        }

        device.LastUsedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<string> CreateTrustedDeviceAsync(Guid userId, string userAgent, string acceptLanguage, string ipAddress, int expiryDays = 30)
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(32); // 256 bits
        var deviceToken = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
        
        var fingerprint = new
        {
            UserAgent = userAgent,
            AcceptLanguage = acceptLanguage,
            IPAddress = ipAddress
        };
        
        var fingerprintJson = JsonSerializer.Serialize(fingerprint);

        var deviceName = ParseDeviceName(userAgent);

        var trustedDevice = new TrustedDevice
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceToken = deviceToken,
            DeviceName = deviceName,
            DeviceFingerprint = fingerprintJson,
            IPAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
            LastUsedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _context.TrustedDevices.AddAsync(trustedDevice);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Trusted device created for user {UserId}: {DeviceName}",
            userId, deviceName);

        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            var emailBody = $@"A new device was added to your trusted devices:
    
Device: {deviceName}
Location: {ipAddress}
Time: {DateTime.UtcNow:F}
    
This device can now login without 2FA codes for 30 days.
    
If this wasn't you, immediately:
1. Login to your account
2. Go to Security Settings
3. Revoke all trusted devices
4. Change your password";

            await _emailService.SendSecurityAlertEmailAsync(user.Email, "New Trusted Device Added", emailBody);
        }

        return deviceToken;
    }

    private string ParseDeviceName(string userAgent)
    {
        var browser = "Unknown Browser";
        var os = "Unknown OS";
        
        if (userAgent.Contains("Chrome") && !userAgent.Contains("Edg"))
            browser = "Chrome";
        else if (userAgent.Contains("Firefox"))
            browser = "Firefox";
        else if (userAgent.Contains("Safari") && !userAgent.Contains("Chrome"))
            browser = "Safari";
        else if (userAgent.Contains("Edg"))
            browser = "Edge";
        
        if (userAgent.Contains("Windows"))
            os = "Windows";
        else if (userAgent.Contains("Mac"))
            os = "Mac";
        else if (userAgent.Contains("iPhone"))
            os = "iPhone";
        else if (userAgent.Contains("Android"))
            os = "Android";
        else if (userAgent.Contains("Linux"))
            os = "Linux";
        
        return $"{browser} on {os}";
    }

    public async Task<List<TrustedDeviceDTO>> GetUserTrustedDevicesAsync(Guid userId)
    {
        var devices = await _context.TrustedDevices
            .Where(d => d.UserId == userId && d.IsActive)
            .OrderByDescending(d => d.LastUsedAt ?? d.CreatedAt)
            .ToListAsync();

        var dtos = devices.Select(d => new TrustedDeviceDTO
        {
            Id = d.Id,
            DeviceName = d.DeviceName,
            IPAddress = d.IPAddress,
            CreatedAt = d.CreatedAt,
            ExpiresAt = d.ExpiresAt,
            LastUsedAt = d.LastUsedAt,
            DaysUntilExpiry = (int)(d.ExpiresAt - DateTime.UtcNow).TotalDays,
            IsCurrentDevice = false // Handled in controller/frontend
        }).ToList();

        return dtos;
    }

    public async Task<bool> RevokeDeviceAsync(Guid deviceId, Guid userId, string reason = "User revoked")
    {
        var device = await _context.TrustedDevices
            .FirstOrDefaultAsync(d => d.Id == deviceId && d.UserId == userId);

        if (device == null)
            return false;

        device.IsActive = false;
        device.RevokedAt = DateTime.UtcNow;
        device.RevokedReason = reason;

        await _context.SaveChangesAsync();

        _logger.LogWarning(
            "Trusted device revoked for user {UserId}: {DeviceName}. Reason: {Reason}",
            userId, device.DeviceName, reason);

        return true;
    }

    public async Task RevokeAllUserDevicesAsync(Guid userId, string reason = "User revoked all")
    {
        var devices = await _context.TrustedDevices
            .Where(d => d.UserId == userId && d.IsActive)
            .ToListAsync();

        foreach (var device in devices)
        {
            device.IsActive = false;
            device.RevokedAt = DateTime.UtcNow;
            device.RevokedReason = reason;
        }

        await _context.SaveChangesAsync();

        _logger.LogWarning(
            "All trusted devices revoked for user {UserId}. Count: {Count}. Reason: {Reason}",
            userId, devices.Count, reason);
    }

    public async Task CleanupExpiredDevicesAsync()
    {
        var expiredDevices = await _context.TrustedDevices
            .Where(d => d.IsActive && d.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        foreach (var device in expiredDevices)
        {
            device.IsActive = false;
            device.RevokedAt = DateTime.UtcNow;
            device.RevokedReason = "Expired after 30 days";
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Cleaned up {Count} expired trusted devices",
            expiredDevices.Count);
    }
}
