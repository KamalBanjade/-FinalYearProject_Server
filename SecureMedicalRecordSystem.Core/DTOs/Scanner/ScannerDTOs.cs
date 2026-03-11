using System;
using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.DTOs.Scanner;

public class PairRequestDTO
{
    [Required]
    public string DesktopSessionId { get; set; } = string.Empty;
    
    [Required]
    public string MobileDeviceId { get; set; } = string.Empty;
    
    [Required]
    public string DeviceName { get; set; } = string.Empty;
}

public class ScanRequestDTO
{
    [Required]
    public string PatientToken { get; set; } = string.Empty;
    
    [Required]
    public string DesktopSessionId { get; set; } = string.Empty;
    
    [Required]
    public string MobileDeviceId { get; set; } = string.Empty;
}

public class DesktopVerifyTotpDTO
{
    [Required]
    public Guid PatientId { get; set; }
    
    [Required]
    public string TotpCode { get; set; } = string.Empty;
    
    public string DesktopSessionId { get; set; } = string.Empty;
}
