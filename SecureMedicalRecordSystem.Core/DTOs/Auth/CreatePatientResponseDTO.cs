using System;

namespace SecureMedicalRecordSystem.Core.DTOs.Auth;

public class CreatePatientResponseDTO
{
    public Guid PatientId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string TemporaryPassword { get; set; } = string.Empty;
}
