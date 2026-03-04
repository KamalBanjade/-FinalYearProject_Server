using SecureMedicalRecordSystem.Core.Enums;

namespace SecureMedicalRecordSystem.Core.Interfaces;

/// <summary>
/// Service for generating QR codes and building access URLs.
/// </summary>
public interface IQRCodeGenerationService
{
    /// <summary>
    /// Generates a QR code as a PNG image byte array.
    /// </summary>
    /// <param name="data">The text or URL to encode in the QR code.</param>
    /// <param name="pixelsPerModule">Size of the QR code (default is 20).</param>
    /// <returns>A byte array representing the PNG image.</returns>
    byte[] GenerateQRCodeImage(string data, int pixelsPerModule = 20);

    /// <summary>
    /// Generates a QR code as an SVG string.
    /// </summary>
    /// <param name="data">The text or URL to encode in the QR code.</param>
    /// <param name="pixelsPerModule">Size control for the SVG modules (default is 20).</param>
    /// <returns>A string containing the SVG markup.</returns>
    string GenerateQRCodeSvg(string data, int pixelsPerModule = 20);

    /// <summary>
    /// Builds the full front-end URL for the QR code access based on token type.
    /// </summary>
    /// <param name="token">The unique access token.</param>
    /// <param name="tokenType">The type of access (Normal or Emergency).</param>
    /// <returns>The fully qualified URL string.</returns>
    string BuildAccessUrl(string token, QRTokenType tokenType);
}
