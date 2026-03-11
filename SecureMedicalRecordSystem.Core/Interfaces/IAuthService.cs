using SecureMedicalRecordSystem.Core.DTOs.Auth;
using SecureMedicalRecordSystem.Core.DTOs.Admin;
using SecureMedicalRecordSystem.Core.Entities;

namespace SecureMedicalRecordSystem.Core.Interfaces;

public interface IAuthService
{
    Task<(bool Success, string Message, RegistrationResponseDTO? Data)> RegisterPatientAsync(RegisterRequestDTO request);
    Task<(bool Success, string Message, InviteDoctorResponseDTO? Data)> InviteDoctorAsync(InviteDoctorRequestDTO request, Guid adminUserId);
    Task<(bool Success, string Message, LoginResponseDTO? Data)> LoginAsync(LoginRequestDTO request);
    Task<(bool Success, string Message)> ConfirmEmailAsync(Guid userId, string token);
    Task<(bool Success, string Message)> ResendVerificationEmailAsync(string email);
    Task<(bool Success, string Message)> ForgotPasswordAsync(string email);
    Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordRequestDTO request);
    Task<(bool Success, string Message)> ChangePasswordAsync(Guid userId, ChangePasswordRequestDTO request);
    Task<ApplicationUser?> GetUserByIdAsync(Guid userId);
    Task<ApplicationUser?> GetUserByEmailAsync(string email);

    // 2FA Methods
    Task<(bool Success, string Message, TwoFactorSetupDTO? Data)> SetupTwoFactorAsync(Guid userId);
    Task<(bool Success, string Message, List<string>? BackupCodes)> EnableTwoFactorAsync(Guid userId, string code);
    Task<(bool Success, string Message)> DisableTwoFactorAsync(Guid userId, string password);
    Task<(bool Success, List<string>? Codes)> GenerateBackupCodesAsync(Guid userId);
    Task<(bool Success, string Message)> CompleteSetupAsync(Guid userId, CompleteSetupDTO dto);
    Task<(bool Success, string Message, LoginResponseDTO? Data)> GetSetupDataAsync(Guid userId);

    // Admin Doctor Management
    Task<(bool Success, string Message, object? Data)> GetAllDoctorsAsync(
        int page = 1, 
        int pageSize = 10, 
        string? searchTerm = null, 
        string? department = null, 
        bool? isActive = null);
    
    Task<(bool Success, string Message, object? Data)> GetDoctorDetailsAsync(Guid doctorId);
    Task<Doctor?> GetDoctorEntityByIdAsync(Guid doctorId);
    Task<(bool Success, string Message)> UpdateDoctorAsync(Guid doctorUserId, UpdateDoctorRequestDTO request);
    Task<(bool Success, string Message)> DeleteDoctorAsync(Guid doctorUserId);
    Task<(bool Success, string Message, object? Data)> RotateDoctorKeysAsync(Guid doctorUserId);

    // Admin User Management
    Task<(bool Success, string Message, object? Data)> GetAllUsersAsync(
        int page = 1,
        int pageSize = 10,
        string? searchTerm = null,
        string? role = null,
        bool? isActive = null);

    Task<(bool Success, string Message)> UpdateUserStatusAsync(Guid userId, bool isActive);
    Task<(bool Success, string Message)> UpdateUserAsync(Guid userId, UpdateUserRequestDTO request);
}
