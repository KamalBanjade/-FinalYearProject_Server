using SecureMedicalRecordSystem.Core.Entities;

namespace SecureMedicalRecordSystem.Core.Interfaces;

/// <summary>
/// JWT + Refresh Token service contract.
/// </summary>
public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user);
    string GenerateRefreshToken();
    Task<RefreshToken> CreateRefreshTokenAsync(Guid userId, string ipAddress, string userAgent);
    Task<ApplicationUser?> ValidateRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(string token, string replacedByToken);
}
