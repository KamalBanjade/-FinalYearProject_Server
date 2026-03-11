using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.Entities;

public class TemplateUsageHistory : BaseEntity
{
    [Required]
    public Guid TemplateId { get; set; }

    [Required]
    public Guid RecordId { get; set; }

    [Required]
    public Guid DoctorId { get; set; }

    [Required]
    public DateTime UsedAt { get; set; }

    // Modification Tracking
    public int FieldsAdded { get; set; } = 0;
    public int FieldsRemoved { get; set; } = 0;
    public string? AddedFieldsJson { get; set; }
    public bool WasTemplateUpdated { get; set; } = false;

    // Performance
    public int? EntryTimeSeconds { get; set; }

    // Navigation Properties
    public Template Template { get; set; } = null!;
    public PatientHealthRecord Record { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
}
