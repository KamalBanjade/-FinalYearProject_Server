using SecureMedicalRecordSystem.Core.Enums;

namespace SecureMedicalRecordSystem.Core.DTOs.HealthRecords;

public class TemplateDTO
{
    public Guid Id { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // Origin Tracking
    public Guid CreatedBy { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public Guid? CreatedFromRecordId { get; set; }
    public Guid? BasedOnTemplateId { get; set; }

    // Sharing & Visibility
    public VisibilityLevel Visibility { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }

    // Schema
    public TemplateSchemaDTO Schema { get; set; } = new();
    public int Version { get; set; }
    public bool IsActive { get; set; }

    // Usage Statistics
    public int UsageCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public int? AverageEntryTimeSeconds { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
