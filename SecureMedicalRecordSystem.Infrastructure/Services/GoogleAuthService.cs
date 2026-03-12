using SecureMedicalRecordSystem.Core.DTOs.Auth;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly IQRTokenService _qrTokenService;
    private readonly ILocalUrlProvider _urlProvider;
    
    private const string GoogleAuthUrl = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string GoogleTokenUrl = "https://oauth2.googleapis.com/token";
    private const string GoogleUserInfoUrl = "https://www.googleapis.com/oauth2/v2/userinfo";
    private const string GoogleIssuer = "https://accounts.google.com";
    
    public GoogleAuthService(
        HttpClient httpClient,
        IConfiguration configuration,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IAuditLogService auditLogService,
        IQRTokenService qrTokenService,
        ILocalUrlProvider urlProvider)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _userManager = userManager;
        _context = context;
        _auditLogService = auditLogService;
        _qrTokenService = qrTokenService;
        _urlProvider = urlProvider;
    }

    public string GetGoogleLoginUrl(out string state)
    {
        var clientId = _configuration["Authentication:Google:ClientId"];
        if (string.IsNullOrEmpty(clientId))
            throw new InvalidOperationException("Google ClientId not configured");

        state = GenerateSecureState();
        
        var redirectUri = _configuration["Authentication:Google:RedirectUri"];
        var scope = "openid email profile";
        
        var queryParams = new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "redirect_uri", redirectUri },
            { "response_type", "code" },
            { "scope", scope },
            { "state", state },
            { "access_type", "offline" },
            { "prompt", "consent" }
        };
        
        var queryString = string.Join("&", queryParams.Select(kvp => 
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
        
        return $"{GoogleAuthUrl}?{queryString}";
    }

    public async Task<(bool Success, string Message, LoginResponseDTO? Data)> HandleGoogleCallbackAsync(
        string authorizationCode,
        string state,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenResponse = await ExchangeCodeForTokensAsync(authorizationCode, cancellationToken);
            var idTokenPayload = ValidateAndDecodeIdToken(tokenResponse.Id_Token);
            var userInfo = await GetGoogleUserInfoAsync(tokenResponse.Access_Token, cancellationToken);
            
            ValidateUserInfo(userInfo);
            
            var (user, isNewUser) = await FindOrCreateUserAsync(userInfo, idTokenPayload.Sub, cancellationToken);
            
            var data = await GenerateLoginResponseAsync(user, isNewUser);
            
            await _auditLogService.LogAsync(user.Id, "Google Login", "User logged in via Google OAuth", "N/A", "System");
            
            return (true, "Login successful", data);
        }
        catch (Exception ex)
        {
            return (false, $"Google login failed: {ex.Message}", null);
        }
    }

    private async Task<GoogleTokenResponse> ExchangeCodeForTokensAsync(
        string authorizationCode,
        CancellationToken cancellationToken)
    {
        var clientId = _configuration["Authentication:Google:ClientId"];
        var clientSecret = _configuration["Authentication:Google:ClientSecret"];
        
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            throw new InvalidOperationException("Google OAuth credentials not configured");

        var requestData = new Dictionary<string, string>
        {
            { "code", authorizationCode },
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "redirect_uri", _configuration["Authentication:Google:RedirectUri"] },
            { "grant_type", "authorization_code" }
        };

        var response = await _httpClient.PostAsync(GoogleTokenUrl, new FormUrlEncodedContent(requestData), cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to exchange authorization code: {error}");
        }

        return await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken)
               ?? throw new InvalidOperationException("Invalid token response from Google");
    }

    private GoogleIdTokenPayload ValidateAndDecodeIdToken(string idToken)
    {
        var clientId = _configuration["Authentication:Google:ClientId"];
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(idToken);
        
        var payload = new GoogleIdTokenPayload
        {
            Iss = jwtToken.Claims.FirstOrDefault(c => c.Type == "iss")?.Value ?? "",
            Sub = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "",
            Aud = jwtToken.Claims.FirstOrDefault(c => c.Type == "aud")?.Value ?? "",
            Email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? "",
            Email_Verified = bool.Parse(jwtToken.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value ?? "false"),
            Iat = long.Parse(jwtToken.Claims.FirstOrDefault(c => c.Type == "iat")?.Value ?? "0"),
            Exp = long.Parse(jwtToken.Claims.FirstOrDefault(c => c.Type == "exp")?.Value ?? "0")
        };
        
        if (payload.Iss != GoogleIssuer && payload.Iss != "accounts.google.com")
            throw new SecurityException($"Invalid token issuer: {payload.Iss}");
            
        if (payload.Aud != clientId)
            throw new SecurityException($"Invalid token audience: {payload.Aud}");
            
        if (DateTimeOffset.FromUnixTimeSeconds(payload.Exp) < DateTimeOffset.UtcNow)
            throw new SecurityException("Token has expired");
            
        return payload;
    }

    private async Task<GoogleUserInfo> GetGoogleUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _httpClient.GetAsync(GoogleUserInfoUrl, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Failed to fetch user info from Google");

        return await response.Content.ReadFromJsonAsync<GoogleUserInfo>(cancellationToken)
               ?? throw new InvalidOperationException("Invalid user info response from Google");
    }

    private void ValidateUserInfo(GoogleUserInfo userInfo)
    {
        if (string.IsNullOrEmpty(userInfo.Email))
            throw new InvalidOperationException("Email not provided by Google");
        if (!userInfo.Email_Verified)
            throw new InvalidOperationException("Email not verified by Google");
    }

    private async Task<(ApplicationUser user, bool isNewUser)> FindOrCreateUserAsync(
        GoogleUserInfo userInfo, string googleId, CancellationToken cancellationToken)
    {
        var existingUser = await _userManager.Users.Include(u => u.PatientProfile).FirstOrDefaultAsync(u => u.Email == userInfo.Email, cancellationToken);

        if (existingUser != null)
        {
            if (string.IsNullOrEmpty(existingUser.GoogleId))
            {
                existingUser.GoogleId = googleId;
                existingUser.Provider = "Google";
                if (string.IsNullOrEmpty(existingUser.ProfilePictureUrl) && !string.IsNullOrEmpty(userInfo.Picture))
                {
                    existingUser.ProfilePictureUrl = userInfo.Picture;
                }
                await _userManager.UpdateAsync(existingUser);
            }
            return (existingUser, false);
        }

        var user = new ApplicationUser
        {
            UserName = userInfo.Email,
            Email = userInfo.Email,
            EmailConfirmed = true,
            GoogleId = googleId,
            Provider = "Google",
            FirstName = userInfo.Given_Name ?? "Unknown",
            LastName = userInfo.Family_Name ?? "Unknown",
            ProfilePictureUrl = userInfo.Picture,
            Role = "Patient",
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            RequiresPasswordChange = false,
            TwoFactorEnabled = true,
            TOTPSetupCompleted = false,
            MedicalQRGeneratedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        // Initialize TOTP Secret for new user
        await _userManager.ResetAuthenticatorKeyAsync(user);
        await _userManager.UpdateSecurityStampAsync(user);
        var totpSecret = await _userManager.GetAuthenticatorKeyAsync(user);
        user.TOTPSecret = totpSecret;
        await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        await _userManager.AddToRoleAsync(user, "Patient");

        var patient = new Patient { UserId = user.Id };
        _context.Patients.Add(patient);
        await _context.SaveChangesAsync(cancellationToken);
        
        user.PatientProfile = patient;

        return (user, true);
    }

    private async Task<LoginResponseDTO> GenerateLoginResponseAsync(ApplicationUser user, bool isNewUser)
    {
        var expirationDays = 30; // Default Google Auth token length (Remember Me)
        
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("SecretKey not found");
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey));

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.GivenName, user.FirstName),
            new Claim(ClaimTypes.Surname, user.LastName),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddDays(expirationDays);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        if (user.TwoFactorEnabled && !user.TOTPSetupCompleted)
        {
            var totpSecretArr = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(totpSecretArr))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                await _userManager.UpdateSecurityStampAsync(user);
                totpSecretArr = await _userManager.GetAuthenticatorKeyAsync(user);
                user.TOTPSecret = totpSecretArr;
                await _userManager.UpdateAsync(user);
            }

            var issuer = "MedicalRecordSystem";
            var accountName = $"{issuer}:{user.Email}";
            var totpQRData = $"otpauth://totp/{Uri.EscapeDataString(accountName)}?secret={totpSecretArr}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period=30";

            string? accessToken = null;
            string? accessUrl = null;
            DateTime? expiresAtSetup = null;

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (patient != null)
            {
                var tokenResult = await _qrTokenService.GenerateNormalAccessTokenAsync(patient.Id, 30);
                accessToken = tokenResult.Token;
                expiresAtSetup = tokenResult.ExpiresAt;
                var frontendUrl = _urlProvider.FrontendBaseUrl;
                accessUrl = $"{frontendUrl}/access/{accessToken}";
            }

            return new LoginResponseDTO
            {
                Token = jwt,
                UserId = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                RequiresTwoFactor = false,
                TwoFactorEnabled = true,
                TOTPSetupCompleted = false,
                RequiresSetup = true,
                TOTPSetupQRData = totpQRData,
                TOTPSecretManual = totpSecretArr, // Note: FormatTOTPSecret is private in AuthService, so we use raw or we'd need to move it
                MedicalAccessToken = accessToken,
                MedicalAccessURL = accessUrl,
                MedicalAccessExpiresAt = expiresAtSetup,
                ProfilePictureUrl = user.ProfilePictureUrl,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            };
        }

        return new LoginResponseDTO
        {
            Token = jwt,
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            RequiresTwoFactor = false,
            TwoFactorEnabled = user.TwoFactorEnabled,
            TOTPSetupCompleted = user.TOTPSetupCompleted,
            RequiresPasswordChange = user.RequiresPasswordChange,
            DateOfBirth = user.PatientProfile?.DateOfBirth,
            BloodType = user.PatientProfile?.BloodType,
            ProfilePictureUrl = user.ProfilePictureUrl,
            ExpiresAt = expiresAt,
            DeviceTrusted = true // Google login is high-trust
        };
    }

    private static string GenerateSecureState()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}
