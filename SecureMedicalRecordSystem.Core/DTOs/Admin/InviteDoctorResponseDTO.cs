namespace SecureMedicalRecordSystem.Core.DTOs.Admin;

public class InviteDoctorResponseDTO
{
    public Guid DoctorId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string TemporaryPassword { get; set; } = string.Empty;
    public bool InvitationSent { get; set; }
    public string Message { get; set; } = string.Empty;
}
