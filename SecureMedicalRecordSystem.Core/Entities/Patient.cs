using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.Entities;

public class Patient : BaseEntity
{
    public Guid UserId { get; set; }

    // Required fields
    [Required]
    public DateTime DateOfBirth { get; set; }

    [Required]
    [MaxLength(20)]
    public string Gender { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? MedicalRecordNumber { get; set; }  // e.g. "MR-00012345"

    // Optional fields
    [MaxLength(10)]
    public string? BloodType { get; set; }
    public string? Allergies { get; set; }
    public string? ChronicConditions { get; set; }
    public string? CurrentMedications { get; set; }
    
    [MaxLength(100)]
    public string? EmergencyContactName { get; set; }
    
    [MaxLength(20)]
    public string? EmergencyContactPhone { get; set; }

    [MaxLength(50)]
    public string? EmergencyContactRelationship { get; set; }
    
    public string? EmergencyNotesToResponders { get; set; }
    
    public DateTime? EmergencyDataLastUpdated { get; set; }

    public string? Address { get; set; }

    [MaxLength(100)]
    public string? Occupation { get; set; }

    // Primary Doctor (for smart suggestions) 
    public Guid? PrimaryDoctorId { get; set; }
    public Doctor? PrimaryDoctor { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
    public ICollection<PatientHealthRecord> StructuredRecords { get; set; } = new List<PatientHealthRecord>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<QRToken> QRTokens { get; set; } = new List<QRToken>();
}
