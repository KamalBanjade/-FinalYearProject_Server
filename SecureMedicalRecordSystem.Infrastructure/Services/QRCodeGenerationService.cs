using Microsoft.Extensions.Logging;
using QRCoder;
using SecureMedicalRecordSystem.Core.Enums;
using SecureMedicalRecordSystem.Core.Interfaces;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class QRCodeGenerationService : IQRCodeGenerationService
{
    private readonly ILocalUrlProvider _urlProvider;
    private readonly ILogger<QRCodeGenerationService> _logger;

    public QRCodeGenerationService(ILocalUrlProvider urlProvider, ILogger<QRCodeGenerationService> logger)
    {
        _urlProvider = urlProvider;
        _logger = logger;
    }

    public byte[] GenerateQRCodeImage(string data, int pixelsPerModule = 20)
    {
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(pixelsPerModule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PNG QR code for data: {Data}", data);
            throw;
        }
    }

    public string GenerateQRCodeSvg(string data, int pixelsPerModule = 20)
    {
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new SvgQRCode(qrCodeData);
            return qrCode.GetGraphic(pixelsPerModule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating SVG QR code for data: {Data}", data);
            throw;
        }
    }

    public string BuildAccessUrl(string token, QRTokenType tokenType)
    {
        string baseUrl = _urlProvider.FrontendIpBaseUrl;

        return tokenType switch
        {
            QRTokenType.Normal => $"{baseUrl}/access/{token}",
            QRTokenType.Emergency => $"{baseUrl}/emergency/{token}",
            _ => throw new ArgumentException("Invalid token type", nameof(tokenType))
        };
    }
}
