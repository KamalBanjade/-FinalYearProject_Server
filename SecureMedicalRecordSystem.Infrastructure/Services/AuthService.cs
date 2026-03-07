using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SecureMedicalRecordSystem.Core.DTOs.Auth;
using SecureMedicalRecordSystem.Core.DTOs.Admin;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Serilog;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IAuditLogService _auditLogService;
    private readonly IEmailService _emailService;
    private readonly IKeyManagementService _keyManagementService;
    private readonly IQRTokenService _qrTokenService;
    private readonly ITotpService _totpService;
    private readonly ITrustedDeviceService _trustedDeviceService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ApplicationDbContext context,
        IConfiguration configuration,
        IAuditLogService auditLogService,
        IEmailService emailService,
        IKeyManagementService keyManagementService,
        IQRTokenService qrTokenService,
        ITotpService totpService,
        ITrustedDeviceService trustedDeviceService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _context = context;
        _configuration = configuration;
        _auditLogService = auditLogService;
        _emailService = emailService;
        _keyManagementService = keyManagementService;
        _qrTokenService = qrTokenService;
        _totpService = totpService;
        _trustedDeviceService = trustedDeviceService;
    }

    public async Task<(bool Success, string Message, RegistrationResponseDTO? Data)> RegisterPatientAsync(RegisterRequestDTO request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Check if user exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return (false, "User with this email already exists.", null);
            }

            // 2. Create ApplicationUser
            var user = new ApplicationUser
            {
                Email = request.Email,
                UserName = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                Role = "Patient",
                Address = request.Address,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                RequiresPasswordChange = false
            };

            // 3. Create in Identity
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, $"Registration failed: {errors}", null);
            }

            // 4. Add to Patient Role
            await _userManager.AddToRoleAsync(user, "Patient");

            // 5. Create Patient Entity
            var patient = new Patient
            {
                UserId = user.Id,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender
            };
            _context.Patients.Add(patient);

            // 5.1 Mandatory 2FA Setup
            await _userManager.ResetAuthenticatorKeyAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);

            var totpSecret = await _userManager.GetAuthenticatorKeyAsync(user);
            user.TOTPSecret = totpSecret;
            user.TwoFactorEnabled = true;
            user.TOTPSetupCompleted = false;
            user.MedicalQRGeneratedAt = DateTime.UtcNow;
            
            await _userManager.UpdateAsync(user);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // 6. Generate TOTP QR Data
            var issuer = "MedicalRecordSystem";
            var accountName = $"{issuer}:{user.Email}";
            var totpQRData = $"otpauth://totp/{Uri.EscapeDataString(accountName)}" +
                             $"?secret={totpSecret}" +
                             $"&issuer={Uri.EscapeDataString(issuer)}" +
                             $"&algorithm=SHA1" +
                             $"&digits=6" +
                             $"&period=30";

            // 7. Generate Medical Access Token
            var (accessToken, expiresAt) = await _qrTokenService.GenerateNormalAccessTokenAsync(patient.Id, 30);
            var frontendUrl = _configuration["ApplicationUrls:FrontendUrl"] ?? "http://localhost:3000";
            var accessUrl = $"{frontendUrl}/access/{accessToken}";

            // 8. Generate Confirmation Token
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var template = _configuration["EmailTemplates:EmailConfirmationLinkTemplate"] ?? "";
            
            var confirmationLink = template
                .Replace("[TOKEN]", System.Net.WebUtility.UrlEncode(token))
                .Replace("[USERID]", user.Id.ToString());
            
            Log.Information("Email Template: {Template}", template);
            Log.Information("Verification Link generated for {Email}: {Link}", user.Email, confirmationLink);
            
            await _emailService.SendEmailConfirmationAsync(user.Email!, confirmationLink);

            // 9. Log Action
            await _auditLogService.LogAsync(user.Id, "Patient Registration", "Registered as Patient with mandatory 2FA", "N/A", "System");

            return (true, "Account created - complete security setup", new RegistrationResponseDTO
            {
                UserId = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                RequiresSetup = true,
                
                // QR Code 1 Data
                TOTPSetupQRData = totpQRData,
                TOTPSecretManual = FormatTOTPSecret(totpSecret),
                
                // QR Code 2 Data
                MedicalAccessToken = accessToken,
                MedicalAccessURL = accessUrl,
                MedicalAccessExpiresAt = expiresAt
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"An error occurred during registration: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, InviteDoctorResponseDTO? Data)> InviteDoctorAsync(InviteDoctorRequestDTO request, Guid adminUserId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Check permissions (already handled by Controller policy but double check)
            var adminUser = await _userManager.FindByIdAsync(adminUserId.ToString());
            if (adminUser == null || adminUser.Role != "Admin")
            {
                return (false, "Only administrators can invite doctors.", null);
            }

            // 2. Check if user exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return (false, "User with this email already exists.", null);
            }

            // 3. Check NMC License
            var existingDoctor = await _context.Doctors.AnyAsync(d => d.NMCLicense == request.NMCLicense);
            if (existingDoctor)
            {
                return (false, "A doctor with this NMC License already exists.", null);
            }

            // 4. Generate Temporary Password
            var tempPassword = GenerateSecurePassword();

            // 5. Create ApplicationUser
            var user = new ApplicationUser
            {
                Email = request.Email,
                UserName = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                Role = "Doctor",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                RequiresPasswordChange = true, // Force password change
                EmailConfirmed = true // Admin-created accounts are pre-verified
            };

            // 6. Create in Identity
            var result = await _userManager.CreateAsync(user, tempPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, $"Failed to create doctor account: {errors}", null);
            }

            // 7. Add to Doctor Role
            await _userManager.AddToRoleAsync(user, "Doctor");

            // 8. Generate RSA Key Pair for digital signatures
            var keyPair = await _keyManagementService.GenerateRsaKeyPairAsync();

            // 8.1 Validate keys before saving
            var isValidKey = await _keyManagementService.ValidateKeyPairAsync(keyPair.PublicKey, keyPair.EncryptedPrivateKey);
            if (!isValidKey)
            {
                Log.Error("Generated key pair validation failed for doctor {Email}", request.Email);
                throw new InvalidOperationException("Key pair validation failed");
            }
            // 8b. Find or Create Department by Name
            var departmentEntity = await _context.Departments.FirstOrDefaultAsync(d => d.Name == request.Department);
            if (departmentEntity == null)
            {
                // Fallback: create the department if it doesn't exist to prevent failure
                departmentEntity = new Department { Name = request.Department, Description = "Auto-generated" };
                _context.Departments.Add(departmentEntity);
                await _context.SaveChangesAsync();
            }

            // 9. Create Doctor Entity
            var doctor = new Doctor
            {
                UserId = user.Id,
                NMCLicense = request.NMCLicense,
                DepartmentId = departmentEntity.Id,
                Specialization = request.Specialization,
                QualificationDetails = request.QualificationDetails,
                PublicKey = keyPair.PublicKey,
                PrivateKeyEncrypted = keyPair.EncryptedPrivateKey
            };
            _context.Doctors.Add(doctor);

            Log.Information("RSA-2048 key pair generated and validated for doctor {Email}", request.Email);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // 9. Generate Reset Token for Doctor to set password immediately
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            
            // 10. Generate Invitation Link (Password Reset Link)
            var template = _configuration["EmailTemplates:PasswordResetLinkTemplate"] ?? "";
            var resetLink = template
                .Replace("[TOKEN]", System.Net.WebUtility.UrlEncode(resetToken))
                .Replace("[USERID]", user.Id.ToString());

            // 11. Send Invitation Email
            var emailSent = await _emailService.SendDoctorInvitationEmailAsync(
                user.Email!, 
                $"{user.FirstName} {user.LastName}", 
                tempPassword, 
                resetLink);

            // 11. Log Action
            await _auditLogService.LogAsync(adminUserId, "Doctor Invited", $"Invited doctor {request.Email}", "N/A", "System");

            return (true, "Doctor invited successfully.", new InviteDoctorResponseDTO
            {
                DoctorId = user.Id,
                Email = user.Email!,
                TemporaryPassword = tempPassword,
                InvitationSent = emailSent,
                Message = emailSent 
                    ? "Invitation email sent to the doctor." 
                    : "Doctor account created, but email failed to send. Please share credentials manually."
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"An error occurred while inviting doctor: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, LoginResponseDTO? Data)> LoginAsync(LoginRequestDTO request)
    {
        // 1. Find user - include PatientProfile for popover data
        var user = await _userManager.Users
            .Include(u => u.PatientProfile)
            .FirstOrDefaultAsync(u => u.Email == request.Email);
        
        if (user == null)
        {
            return (false, "Invalid email or password.", null);
        }

        if (!user.IsActive)
        {
            return (false, "Account is disabled. Please contact support.", null);
        }

        // Password check with lockout handling.
        // Note: CheckPasswordSignInAsync already checks lockout status AND resets the
        // access-failed counter on success — no need for separate IsLockedOutAsync
        // or ResetAccessFailedCountAsync calls.
        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return (false, "Account locked due to multiple failed attempts. Please try again later.", null);
        }

        if (result.IsNotAllowed)
        {
            return (false, "Please verify your email address before logging in.", null);
        }

        if (!result.Succeeded)
        {
            return (false, "Invalid email or password.", null);
        }

        // Handle 2FA
        if (user.TwoFactorEnabled)
        {
            if (!user.TOTPSetupCompleted)
            {
                var totpSecret = await _userManager.GetAuthenticatorKeyAsync(user);
                if (string.IsNullOrEmpty(totpSecret))
                {
                    await _userManager.ResetAuthenticatorKeyAsync(user);
                    await _userManager.UpdateSecurityStampAsync(user);
                    totpSecret = await _userManager.GetAuthenticatorKeyAsync(user);
                    user.TOTPSecret = totpSecret;
                    await _userManager.UpdateAsync(user);
                }

                var issuer = "MedicalRecordSystem";
                var accountName = $"{issuer}:{user.Email}";
                var totpQRData = $"otpauth://totp/{Uri.EscapeDataString(accountName)}?secret={totpSecret}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period=30";

                string? accessToken = null;
                string? accessUrl = null;
                DateTime? expiresAt = null;

                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (patient != null)
                {
                    var tokenResult = await _qrTokenService.GenerateNormalAccessTokenAsync(patient.Id, 30);
                    accessToken = tokenResult.Token;
                    expiresAt = tokenResult.ExpiresAt;
                    var frontendUrl = _configuration["ApplicationUrls:FrontendUrl"] ?? "http://localhost:3000";
                    accessUrl = $"{frontendUrl}/access/{accessToken}";
                }

                // Generate a limited-time JWT token so the frontend can hit the /complete-setup endpoint
                var setupToken = GenerateJwtToken(user, 1); // 1 day expiry to finish setup

                return (true, "Security setup required.", new LoginResponseDTO
                {
                    Token = setupToken,
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
                    TOTPSecretManual = FormatTOTPSecret(totpSecret),
                    MedicalAccessToken = accessToken,
                    MedicalAccessURL = accessUrl,
                    MedicalAccessExpiresAt = expiresAt,
                    ExpiresAt = DateTime.UtcNow.AddDays(1)
                });
            }

            // Check if device is trusted
            bool isTrusted = false;
            if (!string.IsNullOrEmpty(request.DeviceToken))
            {
                isTrusted = await _trustedDeviceService.IsDeviceTrustedAsync(request.DeviceToken, user.Id);
            }

            if (isTrusted)
            {
                // TRUSTED DEVICE - Skip TOTP
                await _auditLogService.LogAsync(user.Id, "Login via trusted device (2FA skipped)", "Device recognized, TOTP not required", "N/A", "Browser");
                
                var expirationDaysTrusted = 30;
                var tokenTrusted = GenerateJwtToken(user, expirationDaysTrusted);
                
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                return (true, "Login successful", new LoginResponseDTO
                {
                    Token = tokenTrusted,
                    UserId = user.Id,
                    Email = user.Email!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role,
                    RequiresTwoFactor = false,
                    TwoFactorEnabled = true,
                    TrustedDevice = true, // NEW field
                    RequiresPasswordChange = user.RequiresPasswordChange,
                    TOTPSetupCompleted = user.TOTPSetupCompleted,
                    DateOfBirth = user.PatientProfile?.DateOfBirth,
                    BloodType = user.PatientProfile?.BloodType,
                    ExpiresAt = DateTime.UtcNow.AddDays(expirationDaysTrusted)
                });
            }

            // DEVICE NOT TRUSTED - Require TOTP
            if (string.IsNullOrEmpty(request.TwoFactorCode))
            {
                return (true, "Two-factor authentication required.", new LoginResponseDTO 
                { 
                    RequiresTwoFactor = true,
                    TwoFactorEnabled = true,
                    CanRememberDevice = true // NEW field - show checkbox
                });
            }

            // Try Authenticator Token first — using OtpNet directly to avoid
            // ASP.NET Identity's SecurityStamp modifier which crashes on null stamps
            bool valid2fa;
            var loginAuthKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (!string.IsNullOrEmpty(loginAuthKey))
            {
                try
                {
                    var keyBytes = OtpNet.Base32Encoding.ToBytes(loginAuthKey);
                    var totp = new OtpNet.Totp(keyBytes);
                    valid2fa = totp.VerifyTotp(request.TwoFactorCode!.Trim(), out _, new OtpNet.VerificationWindow(previous: 1, future: 1));
                }
                catch
                {
                    valid2fa = false;
                }
            }
            else
            {
                valid2fa = false;
            }
            
            if (!valid2fa)
            {
                // Try Backup Recovery Code
                var validRecovery = await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, request.TwoFactorCode!);
                if (!validRecovery.Succeeded)
                {
                    return (false, "Invalid two-factor code.", null);
                }

                // If backup code used, log it and notify remaining
                var remaining = await _userManager.CountRecoveryCodesAsync(user);
                await _auditLogService.LogAsync(user.Id, "Login (Backup Code)", $"Used recovery code. {remaining} codes left.", "N/A", "Browser");
            }
        }

        // Generate JWT
        var expirationDays = request.RememberDevice ? 30 : 1;
        var token = GenerateJwtToken(user, expirationDays);

        // Update last-login timestamp. Sequential awaits are required because UserManager.UpdateAsync
        // and AuditLogService.LogAsync both share the same scoped DbContext instance
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        await _auditLogService.LogAsync(user.Id, "User Login", "Successful login", "N/A", "Browser");

        return (true, "Login successful", new LoginResponseDTO
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            RequiresTwoFactor = false,
            TwoFactorEnabled = user.TwoFactorEnabled,
            DeviceTrusted = request.RememberDevice, // NEW field
            TOTPSetupCompleted = user.TOTPSetupCompleted,
            RequiresPasswordChange = user.RequiresPasswordChange,
            DateOfBirth = user.PatientProfile?.DateOfBirth,
            BloodType = user.PatientProfile?.BloodType,
            ExpiresAt = DateTime.UtcNow.AddDays(expirationDays)
        });
    }

    public async Task<(bool Success, string Message)> ResendVerificationEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            // For security reasons, don't reveal if account exists, but for the success screen
            // we probably want to be helpful while still being secure.
            return (true, "If your email is registered, a new verification link has been sent.");
        }

        if (user.EmailConfirmed)
        {
            return (false, "Email is already verified.");
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var template = _configuration["EmailTemplates:EmailConfirmationLinkTemplate"] ?? "";
        
        var confirmationLink = template
            .Replace("[TOKEN]", System.Net.WebUtility.UrlEncode(token))
            .Replace("[USERID]", user.Id.ToString());
        
        await _emailService.SendEmailConfirmationAsync(user.Email!, confirmationLink);
        await _auditLogService.LogAsync(user.Id, "Resend Verification", "Verification email resent", "N/A", "System");

        return (true, "Verification email sent successfully.");
    }

    public async Task<(bool Success, string Message)> ConfirmEmailAsync(Guid userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return (false, "User not found.");

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (result.Succeeded)
        {
            await _auditLogService.LogAsync(user.Id, "Email Confirmation", "Email confirmed successfully", "N/A", "System");
            return (true, "Email confirmed successfully.");
        }

        return (false, "Email confirmation failed.");
    }

    public async Task<(bool Success, string Message)> ForgotPasswordAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null || !user.IsActive)
        {
            // Don't reveal account existence
            return (true, "If your email exists in our system, you will receive a password reset link shortly.");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        
        var template = _configuration["EmailTemplates:PasswordResetLinkTemplate"] ?? "";
        var resetLink = template
            .Replace("[TOKEN]", System.Net.WebUtility.UrlEncode(token))
            .Replace("[USERID]", user.Id.ToString());

        await _emailService.SendPasswordResetEmailAsync(email, resetLink);
        await _auditLogService.LogAsync(user.Id, "Forgot Password", "Password reset requested", "N/A", "System");

        return (true, "If your email exists in our system, you will receive a password reset link shortly.");
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordRequestDTO request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null) return (false, "Invalid request.");

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (result.Succeeded)
        {
            if (user.RequiresPasswordChange)
            {
                user.RequiresPasswordChange = false;
                await _userManager.UpdateAsync(user);
            }
            
            // NEW: Revoke all trusted devices (security measure)
            await _trustedDeviceService.RevokeAllUserDevicesAsync(
                user.Id, 
                "Password reset - all devices untrusted for security");

            await _auditLogService.LogAsync(user.Id, "Password Reset", "Password reset completed - all trusted devices revoked", "User will need to re-verify on all devices", "System");
            
            return (true, "Password reset successful. All trusted devices have been logged out for security.");
        }

        return (false, "Password reset failed.");
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(Guid userId, ChangePasswordRequestDTO request)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return (false, "User not found.");

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (result.Succeeded)
        {
            if (user.RequiresPasswordChange)
            {
                user.RequiresPasswordChange = false;
                await _userManager.UpdateAsync(user);
            }

            await _auditLogService.LogAsync(user.Id, "Change Password", "Password changed successfully", "N/A", "System");
            return (true, "Password changed successfully.");
        }

        return (false, "Failed to change password.");
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(Guid userId)
    {
        return await _userManager.Users
            .Include(u => u.PatientProfile)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task<(bool Success, string Message, LoginResponseDTO? Data)> GetSetupDataAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return (false, "User not found.", null);
        }

        if (user.TOTPSetupCompleted)
        {
            return (false, "Setup already completed.", null);
        }

        var totpSecret = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(totpSecret))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);
            totpSecret = await _userManager.GetAuthenticatorKeyAsync(user);
            user.TOTPSecret = totpSecret;
            await _userManager.UpdateAsync(user);
        }

        var issuer = "MedicalRecordSystem";
        var accountName = $"{issuer}:{user.Email}";
        var totpQRData = $"otpauth://totp/{Uri.EscapeDataString(accountName)}?secret={totpSecret}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period=30";

        string? accessToken = null;
        string? accessUrl = null;
        DateTime? expiresAt = null;

        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (patient != null)
        {
            var tokenResult = await _qrTokenService.GenerateNormalAccessTokenAsync(patient.Id, 30);
            accessToken = tokenResult.Token;
            expiresAt = tokenResult.ExpiresAt;
            var frontendUrl = _configuration["ApplicationUrls:FrontendUrl"] ?? "http://localhost:3000";
            accessUrl = $"{frontendUrl}/access/{accessToken}";
        }

        return (true, "Setup data generated successfully.", new LoginResponseDTO
        {
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
            TOTPSecretManual = FormatTOTPSecret(totpSecret),
            MedicalAccessToken = accessToken,
            MedicalAccessURL = accessUrl,
            MedicalAccessExpiresAt = expiresAt,
            ExpiresAt = DateTime.UtcNow
        });
    }

    public async Task<(bool Success, string Message, TwoFactorSetupDTO? Data)> SetupTwoFactorAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return (false, "User not found.", null);

        if (user.TwoFactorEnabled)
        {
            return (false, "Two-factor authentication is already enabled.", null);
        }

        var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        if (string.IsNullOrEmpty(unformattedKey))
        {
            return (false, "Failed to generate authenticator key.", null);
        }

        // Format for display: ABCD EFGH IJKL...
        var formattedKey = string.Join(" ", Enumerable.Range(0, unformattedKey.Length / 4)
            .Select(i => unformattedKey.Substring(i * 4, 4)));

        // otpauth://totp/MedicalRecordSystem:email?secret=key&issuer=MedicalRecordSystem
        var qrCodeUri = $"otpauth://totp/MedicalRecordSystem:{user.Email}?secret={unformattedKey}&issuer=MedicalRecordSystem";

        await _auditLogService.LogAsync(user.Id, "2FA Setup", "Initiated 2FA setup", "N/A", "System");

        return (true, "Authenticator key generated.", new TwoFactorSetupDTO
        {
            UserId = user.Id,
            ManualEntryKey = formattedKey,
            QRCodeUri = qrCodeUri
        });
    }

    public async Task<(bool Success, string Message, List<string>? BackupCodes)> EnableTwoFactorAsync(Guid userId, string code)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return (false, "User not found.", null);

        var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);
        if (!isValid)
        {
            return (false, "Invalid verification code.", null);
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        
        // Generate recovery codes
        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        var codesList = recoveryCodes?.ToList() ?? new List<string>();

        await _auditLogService.LogAsync(user.Id, "2FA Enable", "Two-factor authentication enabled", "N/A", "System");

        return (true, "Two-factor authentication enabled successfully. Please save your recovery codes.", codesList);
    }

    public async Task<(bool Success, string Message)> DisableTwoFactorAsync(Guid userId, string password)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return (false, "User not found.");

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!isPasswordValid)
        {
            return (false, "Invalid password.");
        }

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);

        await _auditLogService.LogAsync(user.Id, "2FA Disable", "Two-factor authentication disabled", "N/A", "System");

        return (true, "Two-factor authentication disabled successfully.");
    }

    public async Task<(bool Success, List<string>? Codes)> GenerateBackupCodesAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || !user.TwoFactorEnabled)
        {
            return (false, null);
        }

        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        await _auditLogService.LogAsync(user.Id, "2FA Backup Codes", "New recovery codes generated", "N/A", "System");

        return (true, recoveryCodes?.ToList());
    }

    public async Task<(bool Success, string Message, object? Data)> GetAllDoctorsAsync(
        int page = 1, 
        int pageSize = 10, 
        string? searchTerm = null, 
        string? department = null, 
        bool? isActive = null)
    {
        var query = _context.Doctors
            .AsNoTracking()
            .Include(d => d.User)
            .Include(d => d.Department)
            .AsQueryable();

        // Filters
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(d => 
                d.User.FirstName.Contains(searchTerm) ||
                d.User.LastName.Contains(searchTerm) ||
                d.NMCLicense.Contains(searchTerm) ||
                d.Department.Name.Contains(searchTerm));
        }

        if (!string.IsNullOrEmpty(department))
        {
            query = query.Where(d => d.Department.Name == department);
        }

        if (isActive.HasValue)
        {
            query = query.Where(d => d.User.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync();

        var doctors = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new
            {
                Id = d.Id,
                d.UserId,
                d.User.Email,
                FullName = $"Dr. {d.User.FirstName} {d.User.LastName}",
                d.User.FirstName,
                d.User.LastName,
                d.User.IsActive,
                d.NMCLicense,
                Department = d.Department.Name,
                d.Specialization,
                d.QualificationDetails,
                d.CreatedAt,
                HasKeys = !string.IsNullOrEmpty(d.PublicKey),
                CertificationCount = d.Certifications.Count(c => c.IsValid)
            })
            .ToListAsync();

        return (true, "Doctors retrieved successfully.", new
        {
            doctors,
            pagination = new
            {
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            }
        });
    }

    public async Task<(bool Success, string Message, object? Data)> GetDoctorDetailsAsync(Guid doctorId)
    {
        // AsNoTracking: read-only, no need for change tracking.
        // TotalCertifications is computed server-side in the Select projection —
        // this prevents EF from issuing a separate COUNT query per result row.
        var doctor = await _context.Doctors
            .AsNoTracking()
            .Include(d => d.User)
            .Include(d => d.Department)
            .Include(d => d.Certifications.OrderByDescending(c => c.SignedAt).Take(10))
                .ThenInclude(c => c.MedicalRecord)
            .FirstOrDefaultAsync(d => d.Id == doctorId);

        if (doctor == null) return (false, "Doctor not found.", null);

        // Compute cert count using the already-loaded doctor ID — generates one efficient subquery.
        var totalCertifications = await _context.RecordCertifications
            .AsNoTracking()
            .CountAsync(c => c.DoctorId == doctor.Id && c.IsValid);

        return (true, "Doctor details retrieved successfully.", new
        {
            Id = doctor.Id,
            doctor.UserId,
            doctor.User.Email,
            doctor.User.FirstName,
            doctor.User.LastName,
            doctor.User.PhoneNumber,
            doctor.NMCLicense,
            Department = doctor.Department.Name,
            doctor.Specialization,
            doctor.QualificationDetails,
            doctor.User.IsActive,
            doctor.User.CreatedAt,
            doctor.User.LastLoginAt,
            HasKeys = !string.IsNullOrEmpty(doctor.PublicKey),
            TotalCertifications = totalCertifications,
            RecentCertifications = doctor.Certifications.Select(c => new
            {
                c.RecordId,
                c.MedicalRecord.OriginalFileName,
                c.SignedAt,
                c.RecordHash
            })
        });
    }

    public async Task<(bool Success, string Message)> UpdateDoctorAsync(Guid doctorUserId, UpdateDoctorRequestDTO request)
    {
        var doctor = await _context.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == doctorUserId);

        if (doctor == null) return (false, "Doctor not found.");

        doctor.User.FirstName = request.FirstName;
        doctor.User.LastName = request.LastName;
        doctor.User.PhoneNumber = request.PhoneNumber;
        doctor.User.IsActive = request.IsActive;

        doctor.NMCLicense = request.NMCLicense;
        
        // Find or create department
        var departmentEntity = await _context.Departments.FirstOrDefaultAsync(d => d.Name == request.Department);
        if (departmentEntity == null)
        {
            departmentEntity = new Department { Name = request.Department, Description = "Auto-generated" };
            _context.Departments.Add(departmentEntity);
            await _context.SaveChangesAsync();
        }
        doctor.DepartmentId = departmentEntity.Id;
        
        doctor.Specialization = request.Specialization;
        doctor.QualificationDetails = request.QualificationDetails;

        await _context.SaveChangesAsync();
        return (true, "Doctor profile updated successfully.");
    }

    public async Task<(bool Success, string Message)> DeleteDoctorAsync(Guid doctorUserId)
    {
        var user = await _userManager.FindByIdAsync(doctorUserId.ToString());
        if (user == null) return (false, "User not found.");

        // For audit trail, we might want to soft delete, but here we just deactivate or remove
        // Let's do soft delete if the entity supports it, otherwise deactivate user
        user.IsActive = false;
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded) return (false, "Failed to deactivate doctor.");

        return (true, "Doctor deactivated successfully.");
    }

    public async Task<(bool Success, string Message, object? Data)> RotateDoctorKeysAsync(Guid doctorUserId)
    {
        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == doctorUserId);
        if (doctor == null) return (false, "Doctor not found.", null);

        var keyPair = await _keyManagementService.RotateKeyPairAsync(doctorUserId);
        
        doctor.PublicKey = keyPair.PublicKey;
        doctor.PrivateKeyEncrypted = keyPair.EncryptedPrivateKey;

        await _context.SaveChangesAsync();
        
        return (true, "RSA Key pair rotated successfully.", new { doctor.PublicKey });
    }

    private string GenerateJwtToken(ApplicationUser user, int expirationDays)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? "VERY_REALLY_LONG_SECRET_KEY_FOR_JWT_DEVELOPMENT_ONLY";
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
        var expires = DateTime.UtcNow.AddDays(expirationDays);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateSecurePassword()
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*()_+";
        const string all = upper + lower + digits + special;

        var random = new Random();
        var password = new StringBuilder();

        // Ensure at least one of each
        password.Append(upper[random.Next(upper.Length)]);
        password.Append(lower[random.Next(lower.Length)]);
        password.Append(digits[random.Next(digits.Length)]);
        password.Append(digits[random.Next(digits.Length)]);
        password.Append(special[random.Next(special.Length)]);

        for (int i = 0; i < 8; i++)
        {
            password.Append(all[random.Next(all.Length)]);
        }

        // Shuffle
        return new string(password.ToString().OrderBy(c => random.Next()).ToArray());
    }

    public async Task<(bool Success, string Message, object? Data)> GetAllUsersAsync(
        int page = 1,
        int pageSize = 10,
        string? searchTerm = null,
        string? role = null,
        bool? isActive = null)
    {
        var query = _userManager.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(u =>
                u.FirstName.Contains(searchTerm) ||
                u.LastName.Contains(searchTerm) ||
                u.Email!.Contains(searchTerm));
        }

        if (!string.IsNullOrEmpty(role))
        {
            query = query.Where(u => u.Role == role);
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserOverviewDTO
            {
                Id = u.Id,
                Email = u.Email!,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Role = u.Role,
                IsActive = u.IsActive,
                EmailConfirmed = u.EmailConfirmed,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                PhoneNumber = u.PhoneNumber
            })
            .ToListAsync();

        return (true, "Users retrieved successfully.", new
        {
            users,
            pagination = new
            {
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            }
        });
    }

    public async Task<(bool Success, string Message)> UpdateUserStatusAsync(Guid userId, bool isActive)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return (false, "User not found.");

        user.IsActive = isActive;
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded) return (false, "Failed to update user status.");

        return (true, $"User {(isActive ? "activated" : "deactivated")} successfully.");
    }

    public async Task<(bool Success, string Message)> UpdateUserAsync(Guid userId, UpdateUserRequestDTO request)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return (false, "User not found.");

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;
        user.IsActive = request.IsActive;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded) return (false, "Failed to update user profile.");

        return (true, "User profile updated successfully.");
    }

    public async Task<(bool Success, string Message)> CompleteSetupAsync(Guid userId, CompleteSetupDTO dto)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return (false, "User not found.");
        }

        if (user.TOTPSetupCompleted)
        {
            return (false, "Setup already completed.");
        }

        // 1. Get the raw authenticator key managed by Identity
        var authenticatorKey = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(authenticatorKey))
        {
            return (false, "TOTP setup has not been initiated. Please go back and reload the setup page.");
        }

        // 2. Validate using OtpNet directly (avoids ASP.NET Identity's SecurityStamp dependency which crashes on null stamps)
        bool isValidCode;
        try
        {
            var keyBytes = OtpNet.Base32Encoding.ToBytes(authenticatorKey);
            var totp = new OtpNet.Totp(keyBytes);
            isValidCode = totp.VerifyTotp(dto.TOTPCode.Trim(), out _, new OtpNet.VerificationWindow(previous: 1, future: 1));
        }
        catch
        {
            return (false, "Invalid verification code format.");
        }

        if (!isValidCode)
        {
            return (false, "Invalid verification code. Please check your authenticator app and try again.");
        }

        // 2. Check confirmations
        if (!dto.TOTPScanned || !dto.MedicalQRSaved)
        {
            return (false, "Please confirm you've completed both steps.");
        }

        // 3. Mark setup as complete
        user.TOTPSetupCompleted = true;
        user.TOTPSetupCompletedAt = DateTime.UtcNow;
        
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return (false, "Failed to update user setup status.");
        }

        // 4. Log Action
        await _auditLogService.LogAsync(user.Id, "Security Setup Completed", "TOTP and medical QR setup finished", "N/A", "Browser");

        return (true, "Setup completed successfully.");
    }

    private string FormatTOTPSecret(string secret)
    {
        // Format: JBSWY3DPEHPK3PXP → JBSW Y3DP EHPK 3PXP
        var formatted = "";
        for (int i = 0; i < secret.Length; i++)
        {
            if (i > 0 && i % 4 == 0)
                formatted += " ";
            formatted += secret[i];
        }
        return formatted;
    }
}
