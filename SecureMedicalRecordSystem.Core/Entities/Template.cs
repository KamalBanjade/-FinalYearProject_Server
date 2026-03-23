using System.ComponentModel.DataAnnotations;
using SecureMedicalRecordSystem.Core.Enums;

namespace SecureMedicalRecordSystem.Core.Entities;

public class Template : BaseEntity
{
    [Required]
    [MaxLength(200)]
    public string TemplateName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    // Origin Tracking
    [Required]
    public Guid CreatorId { get; set; }

    public Guid? CreatedFromRecordId { get; set; }
    public Guid? BasedOnTemplateId { get; set; }

    // Sharing & Visibility
    [Required]
    public VisibilityLevel Visibility { get; set; }

    public Guid? DepartmentId { get; set; }

    // Schema
    [Required]
    public string TemplateSchema { get; set; } = string.Empty;

    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    // Usage Statistics
    public int UsageCount { get; set; } = 0;
    public DateTime? LastUsedAt { get; set; }
    public int? AverageEntryTimeSeconds { get; set; }

    // Navigation Properties
    public ApplicationUser Creator { get; set; } = null!;
    public Department? Department { get; set; }
    public virtual PatientHealthRecord? SourceRecord { get; set; }
    public Template? ParentTemplate { get; set; }
    public ICollection<TemplateUsageHistory> UsageHistory { get; set; } = new List<TemplateUsageHistory>();
}
