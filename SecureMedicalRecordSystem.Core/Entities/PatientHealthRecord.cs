using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.Entities;

public class PatientHealthRecord : BaseEntity
{
    [Required]
    public Guid PatientId { get; set; }

    [Required]
    public Guid DoctorId { get; set; }

    public Guid? AppointmentId { get; set; }

    [Required]
    public DateTime RecordDate { get; set; }

    [MaxLength(50)]
    public string? RecordType { get; set; }

    // Base Health Data (Always Captured)
    public int? BloodPressureSystolic { get; set; }
    public int? BloodPressureDiastolic { get; set; }
    public int? HeartRate { get; set; }
    public decimal? Temperature { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Height { get; set; }
    public decimal? BMI { get; set; }
    public decimal? SpO2 { get; set; }

    // Free Text
    [MaxLength(1000)]
    public string? ChiefComplaint { get; set; }
    
    [MaxLength(5000)]
    public string? DoctorNotes { get; set; }
    
    [MaxLength(1000)]
    public string? Diagnosis { get; set; }
    
    [MaxLength(2000)]
    public string? TreatmentPlan { get; set; }

    // Template Tracking
    public Guid? TemplateId { get; set; }
    public string? TemplateSnapshot { get; set; }
    public bool CreatedFromScratch { get; set; } = true;

    // Follow-Up Tracking
    public DateTime? FollowUpDate { get; set; }
    public int? FollowUpDays { get; set; }
    public bool FollowUpScheduled { get; set; } = false;

    // Metadata
    public bool IsStructured { get; set; } = true;

    [MaxLength(500)]
    public string? GeneratedPdfPath { get; set; }

    // Navigation Properties
    public Patient Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
    public Appointment? Appointment { get; set; }
    public ICollection<HealthAttribute> CustomAttributes { get; set; } = new List<HealthAttribute>();
    public virtual Template? Template { get; set; }
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}
