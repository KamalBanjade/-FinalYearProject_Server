namespace SecureMedicalRecordSystem.Core.DTOs.Doctor;

/// <summary>
/// Request DTO for doctor self-updating their extended profile.
/// </summary>
public class UpdateDoctorExtendedProfileDTO
{
    // Basic editable fields
    public string? Specialization { get; set; }
    public string? HospitalAffiliation { get; set; }
    public string? ContactNumber { get; set; }

    // Extended identity
    public string? Biography { get; set; }
    public int? YearsOfExperience { get; set; }

    // Consultation info
    public string? ConsultationFee { get; set; }
    public string? ConsultationHours { get; set; }
    public string? ConsultationLocation { get; set; }
    public bool? AcceptsNewPatients { get; set; }

    // Structured sections (serialized by caller, deserialized by controller)
    public List<DoctorProfileSectionDTO>? Education { get; set; }
    public List<DoctorProfileSectionDTO>? Experience { get; set; }
    public List<DoctorCertificationItemDTO>? ProfessionalCertifications { get; set; }
    public List<DoctorProfileSectionDTO>? Awards { get; set; }

    // Simple list fields
    public List<string>? Procedures { get; set; }
    public List<string>? Languages { get; set; }

    // Free-form key-value pairs
    public List<DoctorCustomAttributeDTO>? CustomAttributes { get; set; }
}
