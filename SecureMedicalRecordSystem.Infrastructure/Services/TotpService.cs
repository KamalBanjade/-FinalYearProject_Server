using OtpNet;
using SecureMedicalRecordSystem.Core.Interfaces;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class TotpService : ITotpService
{
    private readonly IQRCodeGenerationService _qrCodeGenerationService;

    public TotpService(IQRCodeGenerationService qrCodeGenerationService)
    {
        _qrCodeGenerationService = qrCodeGenerationService;
    }

    public string GenerateSecret()
    {
        var secret = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(secret);
    }

    public string GenerateQrCodeUri(string secret, string email, string issuer = "SecureMedicalSystem")
    {
        var otpUri = new OtpUri(OtpType.Totp, secret, email, issuer);
        return otpUri.ToString();
    }

    public bool ValidateTotp(string secret, string code)
    {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(code))
            return false;

        try
        {
            var secretBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(secretBytes);
            // Allow 30 seconds clock drift (default window is 0, so we check CURRENT and optionally previous)
            // VerificationWindow.Recent(1) checks current + 1 before + 1 after
            return totp.VerifyTotp(code, out _, new VerificationWindow(1, 1));
        }
        catch
        {
            return false;
        }
    }

    public byte[] GenerateQrCodeImage(string qrCodeUri)
    {
        return _qrCodeGenerationService.GenerateQRCodeImage(qrCodeUri);
    }
}
