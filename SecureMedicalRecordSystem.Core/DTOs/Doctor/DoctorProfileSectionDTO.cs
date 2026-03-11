namespace SecureMedicalRecordSystem.Core.DTOs.Doctor;

/// <summary>
/// Generic timeline section item used for Education and Experience.
/// </summary>
public class DoctorProfileSectionDTO
{
    public string Title { get; set; } = string.Empty;             // e.g. "MBBS", "Senior Cardiologist"
    public string? Institution { get; set; }                       // e.g. "AIIMS Delhi", "City Hospital"
    public string? StartYear { get; set; }                         // e.g. "2012"
    public string? EndYear { get; set; }                           // e.g. "2016" or "Present"
    public string? Description { get; set; }
}

/// <summary>
/// Certification / Fellowship entry.
/// </summary>
public class DoctorCertificationItemDTO
{
    public string Name { get; set; } = string.Empty;
    public string? IssuingBody { get; set; }
    public string? Year { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Custom key-value attribute defined freely by the doctor.
/// </summary>
public class DoctorCustomAttributeDTO
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
