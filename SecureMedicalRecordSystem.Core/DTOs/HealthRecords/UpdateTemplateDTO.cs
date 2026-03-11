using System.ComponentModel.DataAnnotations;
using SecureMedicalRecordSystem.Core.Enums;

namespace SecureMedicalRecordSystem.Core.DTOs.HealthRecords;

public class UpdateTemplateDTO
{
    [MaxLength(200)]
    public string? TemplateName { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public VisibilityLevel? Visibility { get; set; }

    public TemplateSchemaDTO? Schema { get; set; }
    
    public bool? IsActive { get; set; }
}
