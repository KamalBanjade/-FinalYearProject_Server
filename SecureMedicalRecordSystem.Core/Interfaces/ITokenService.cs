using SecureMedicalRecordSystem.Core.Entities;

namespace SecureMedicalRecordSystem.Core.Interfaces;

/// <summary>
/// JWT + Refresh Token service contract.
/// </summary>
public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Task<RefreshToken> CreateRefreshTokenAsync(Guid userId, string ipAddress, string userAgent);
    Task<User?> ValidateRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(string token, string replacedByToken);
}
