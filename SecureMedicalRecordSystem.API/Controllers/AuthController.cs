using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureMedicalRecordSystem.Core.DTOs;
using SecureMedicalRecordSystem.Core.DTOs.Auth;
using SecureMedicalRecordSystem.Core.Interfaces;
using System.Security.Claims;

namespace SecureMedicalRecordSystem.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITrustedDeviceService _trustedDeviceService;
    private readonly IWebHostEnvironment _env;

    public AuthController(IAuthService authService, ITrustedDeviceService trustedDeviceService, IWebHostEnvironment env)
    {
        _authService = authService;
        _trustedDeviceService = trustedDeviceService;
        _env = env;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDTO request)
    {
        var result = await _authService.RegisterPatientAsync(request);
        
        if (!result.Success)
        {
            if (result.Message.Contains("exists", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(ApiResponse.FailureResult(result.Message));
            }
            return BadRequest(ApiResponse.FailureResult(result.Message));
        }

        return CreatedAtAction(nameof(GetUser), new { userId = result.Data?.UserId }, ApiResponse.SuccessResult(result.Data, result.Message));
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("auth_token", new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(),
            SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None
        });
        return Ok(ApiResponse.SuccessResult((object?)null, "Logged out successfully."));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
    {
        var result = await _authService.LoginAsync(request);

        if (!result.Success)
        {
            if (result.Message.Contains("locked", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(403, ApiResponse.FailureResult(result.Message));
            }
            return Unauthorized(ApiResponse.FailureResult(result.Message));
        }

        // Handle Trusted Device Token generation
        if (request.RememberDevice && result.Data != null && !result.Data.RequiresTwoFactor)
        {
            var userAgent = Request.Headers["User-Agent"].ToString();
            var acceptLanguage = Request.Headers["Accept-Language"].ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var deviceToken = await _trustedDeviceService.CreateTrustedDeviceAsync(result.Data.UserId, userAgent, acceptLanguage, ipAddress, 30);
            result.Data.DeviceToken = deviceToken;

            // Set cookie (30-day expiry) as per Task 7 instructions
            Response.Cookies.Append("device_token", deviceToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = !_env.IsDevelopment(),
                SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(30),
                Path = "/"
            });
        }

        if (result.Data?.Token != null)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = !_env.IsDevelopment(),
                SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
                Expires = result.Data.ExpiresAt
            };
            Response.Cookies.Append("auth_token", result.Data.Token, cookieOptions);
            // result.Data.Token = null; // We now send token in body for cross-domain tunnel support
        }

        return Ok(ApiResponse.SuccessResult(result.Data, result.Message));
    }

    [HttpGet("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail([FromQuery] Guid userId, [FromQuery] string token)
    {
        var result = await _authService.ConfirmEmailAsync(userId, token);
        
        if (!result.Success)
        {
            return BadRequest(ApiResponse.FailureResult(result.Message));
        }

        return Ok(ApiResponse.SuccessResult((object?)null, result.Message));
    }

    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendVerification([FromQuery] string email)
    {
        var result = await _authService.ResendVerificationEmailAsync(email);
        
        if (!result.Success)
        {
            return BadRequest(ApiResponse.FailureResult(result.Message));
        }

        return Ok(ApiResponse.SuccessResult((object?)null, result.Message));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDTO request)
    {
        var result = await _authService.ForgotPasswordAsync(request.Email);
        return Ok(ApiResponse.SuccessResult((object?)null, result.Message));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDTO request)
    {
        var result = await _authService.ResetPasswordAsync(request);

        if (!result.Success)
        {
            return BadRequest(ApiResponse.FailureResult(result.Message));
        }

        return Ok(ApiResponse.SuccessResult((object?)null, result.Message));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDTO request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.FailureResult("Invalid user session."));
        }

        var result = await _authService.ChangePasswordAsync(userId, request);

        if (!result.Success)
        {
            return BadRequest(ApiResponse.FailureResult(result.Message));
        }

        return Ok(ApiResponse.SuccessResult((object?)null, result.Message));
    }

    [HttpGet("user")]
    [Authorize]
    public async Task<IActionResult> GetUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.FailureResult("Invalid user session."));
        }

        var user = await _authService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound(ApiResponse.FailureResult("User not found."));
        }

        var userData = new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role,
            user.CreatedAt,
            user.LastLoginAt,
            user.IsActive,
            user.TwoFactorEnabled,
            user.TOTPSetupCompleted,
            DateOfBirth = user.PatientProfile?.DateOfBirth,
            BloodType = user.PatientProfile?.BloodType
        };

        return Ok(ApiResponse.SuccessResult(userData, "User profile retrieved successfully."));
    }

    [HttpGet("setup-data")]
    [Authorize]
    public async Task<IActionResult> GetSetupData()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.FailureResult("Invalid user session."));
        }

        var result = await _authService.GetSetupDataAsync(userId);
        if (!result.Success)
        {
            return BadRequest(ApiResponse.FailureResult(result.Message));
        }

        return Ok(ApiResponse.SuccessResult(result.Data, result.Message));
    }

    [HttpGet("two-factor/setup")]
    [Authorize]
    public async Task<IActionResult> SetupTwoFactor()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.FailureResult("Invalid user session."));
        }

        var result = await _authService.SetupTwoFactorAsync(userId);
        if (!result.Success)
        {
            return BadRequest(ApiResponse.FailureResult(result.Message));
        }

        return Ok(ApiResponse.SuccessResult(result.Data, result.Message));
    }

    [HttpPost("two-factor/enable")]
    [Authorize]
    public async Task<IActionResult> EnableTwoFactor([FromBody] VerifyTwoFactorDTO request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.FailureResult("Invalid user session."));
        }

        var result = await _authService.EnableTwoFactorAsync(userId, request.Code);
        if (!result.Success)
        {
            return BadRequest(ApiResponse.FailureResult(result.Message));
        }

        return Ok(ApiResponse.SuccessResult(new { BackupCodes = result.BackupCodes }, result.Message));
    }

    [HttpPost("two-factor/disable")]
    [Authorize]
    public async Task<IActionResult> DisableTwoFactor([FromBody] DisableTwoFactorDTO request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.FailureResult("Invalid user session."));
        }

        var result = await _authService.DisableTwoFactorAsync(userId, request.Password);
        if (!result.Success)
        {
            return result.Message.Contains("password", StringComparison.OrdinalIgnoreCase) 
                ? Unauthorized(ApiResponse.FailureResult(result.Message)) 
                : BadRequest(ApiResponse.FailureResult(result.Message));
        }

        return Ok(ApiResponse.SuccessResult((object?)null, result.Message));
    }

    [HttpPost("two-factor/backup-codes")]
    [Authorize]
    public async Task<IActionResult> GenerateBackupCodes()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.FailureResult("Invalid user session."));
        }

        var result = await _authService.GenerateBackupCodesAsync(userId);
        if (!result.Success)
        {
            return BadRequest(ApiResponse.FailureResult("Unable to generate backup codes. Is 2FA enabled?"));
        }

        return Ok(ApiResponse.SuccessResult(new { BackupCodes = result.Codes }, "New backup codes generated. Old codes are now invalid."));
    }

    [HttpPost("complete-setup")]
    [Authorize]
    public async Task<IActionResult> CompleteSetup([FromBody] CompleteSetupDTO request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.FailureResult("Invalid user session."));
        }

        var result = await _authService.CompleteSetupAsync(userId, request);
        if (!result.Success)
        {
            return BadRequest(ApiResponse.FailureResult(result.Message));
        }

        return Ok(ApiResponse.SuccessResult(new { redirectTo = "/dashboard" }, result.Message));
    }

    [HttpGet("trusted-devices")]
    [Authorize]
    public async Task<IActionResult> GetDevices()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.FailureResult("Invalid user session."));
        }

        var devices = await _trustedDeviceService.GetUserTrustedDevicesAsync(userId);
        var currentToken = Request.Cookies["device_token"];
        
        // Ensure IsCurrentDevice is set correctly based on the cookie
        var deviceData = devices.Select(d => new
        {
            d.DeviceName,
            DeviceToken = d.Id.ToString(), // Sending ID as token for frontend compatibility
            d.CreatedAt,
            d.LastUsedAt,
            d.ExpiresAt,
            d.IPAddress,
            IsCurrent = currentToken != null && currentToken.Contains(d.Id.ToString()) // Just a fallback, better to adjust DTO
        });

        return Ok(ApiResponse.SuccessResult(deviceData, "Trusted devices retrieved successfully."));
    }

    [HttpDelete("trusted-devices/{id}")]
    [Authorize]
    public async Task<IActionResult> RevokeDevice(Guid id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.FailureResult("Invalid user session."));
        }

        var result = await _trustedDeviceService.RevokeDeviceAsync(id, userId);
        if (!result)
        {
            return BadRequest(ApiResponse.FailureResult("Failed to revoke device or device not found."));
        }

        return Ok(ApiResponse.SuccessResult((object?)null, "Device revoked successfully."));
    }

    [HttpPost("trusted-devices/revoke-all")]
    [Authorize]
    public async Task<IActionResult> RevokeAllDevices()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.FailureResult("Invalid user session."));
        }

        await _trustedDeviceService.RevokeAllUserDevicesAsync(userId, "User revoked all devices via settings");
        
        return Ok(ApiResponse.SuccessResult((object?)null, "All devices revoked successfully."));
    }
}
