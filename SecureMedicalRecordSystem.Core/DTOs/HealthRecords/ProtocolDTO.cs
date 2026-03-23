using System.Collections.Generic;

namespace SecureMedicalRecordSystem.Core.DTOs.HealthRecords;

public class ProtocolFieldDTO
{
    public string FieldName { get; set; } = string.Empty;
    public string? LastValue { get; set; }
    public string? Unit { get; set; }
    public string? NormalRange { get; set; }
    public string? FieldType { get; set; }
}

public class ProtocolSectionDTO
{
    public string SectionName { get; set; } = string.Empty;
    public List<ProtocolFieldDTO> Fields { get; set; } = new();
}

public class ProtocolDTO
{
    public string TemplateName { get; set; } = string.Empty;
    public List<ProtocolSectionDTO> Sections { get; set; } = new();
}
