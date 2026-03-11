namespace SecureMedicalRecordSystem.Core.DTOs.HealthRecords;

public class SectionDTO
{
    public string SectionName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public List<AttributeDTO> Attributes { get; set; } = new();
}
