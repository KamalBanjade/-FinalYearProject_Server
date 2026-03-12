namespace SecureMedicalRecordSystem.Core.DTOs.Doctor;

/// <summary>
/// Full extended profile read model returned from GET /api/doctor/profile
/// and GET /api/patient/doctors/{id}
/// </summary>
public class DoctorExtendedProfileDTO
{
    // Identity
    public Guid DoctorId { get; set; }
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NMCLicense { get; set; } = string.Empty;
    public string DepartmentId { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string? HospitalAffiliation { get; set; }
    public string? ContactNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }

    // Extended identity
    public string? Biography { get; set; }
    public int? YearsOfExperience { get; set; }

    // Consultation info
    public string? ConsultationFee { get; set; }
    public string? ConsultationHours { get; set; }
    public string? ConsultationLocation { get; set; }
    public bool? AcceptsNewPatients { get; set; }

    // Deserialized structured sections
    public List<DoctorProfileSectionDTO> Education { get; set; } = new();
    public List<DoctorProfileSectionDTO> Experience { get; set; } = new();
    public List<DoctorCertificationItemDTO> ProfessionalCertifications { get; set; } = new();
    public List<DoctorProfileSectionDTO> Awards { get; set; } = new();
    public List<string> Procedures { get; set; } = new();
    public List<string> Languages { get; set; } = new();
    public List<DoctorCustomAttributeDTO> CustomAttributes { get; set; } = new();

    // Profile Completion Score (0–100)
    public int ProfileCompletionScore { get; set; }
    public List<string> MissingProfileFields { get; set; } = new();
}
