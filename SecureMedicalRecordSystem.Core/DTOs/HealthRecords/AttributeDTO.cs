namespace SecureMedicalRecordSystem.Core.DTOs.HealthRecords;

public class AttributeDTO
{
    public Guid Id { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string FieldLabel { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public string FieldValue { get; set; } = string.Empty;
    public string? FieldUnit { get; set; }
    public bool? IsAbnormal { get; set; }
    public string? NormalRange { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsFromTemplate { get; set; }
}
