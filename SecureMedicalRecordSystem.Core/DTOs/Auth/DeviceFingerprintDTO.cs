namespace SecureMedicalRecordSystem.Core.DTOs.Auth;

public class DeviceFingerprintDTO
{
    public string Browser { get; set; } = string.Empty;
    public string OS { get; set; } = string.Empty;
    public string ScreenResolution { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
}
