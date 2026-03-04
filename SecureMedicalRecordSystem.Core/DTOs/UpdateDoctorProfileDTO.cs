namespace SecureMedicalRecordSystem.Core.DTOs;

public class UpdateDoctorProfileDTO
{
    public string? Department { get; set; }
    public string? Specialization { get; set; }
    public string? HospitalAffiliation { get; set; }
    public string? ContactNumber { get; set; }
}
