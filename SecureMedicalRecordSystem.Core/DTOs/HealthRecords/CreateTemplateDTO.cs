using System.ComponentModel.DataAnnotations;
using SecureMedicalRecordSystem.Core.Enums;

namespace SecureMedicalRecordSystem.Core.DTOs.HealthRecords;

public class CreateTemplateDTO
{
    [Required]
    [MaxLength(200)]
    public string TemplateName { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public VisibilityLevel Visibility { get; set; }
    
    public Guid? SourceRecordId { get; set; }
    
    public TemplateSchemaDTO? Schema { get; set; }
}
