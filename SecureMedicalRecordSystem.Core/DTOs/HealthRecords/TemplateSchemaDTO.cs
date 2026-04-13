using System.Text.Json.Serialization;
using SecureMedicalRecordSystem.Core.Enums;

namespace SecureMedicalRecordSystem.Core.DTOs.HealthRecords;

public class TemplateSchemaDTO
{
    [JsonPropertyName("sections")]
    public List<TemplateSectionDTO> Sections { get; set; } = new();
}

public class TemplateSectionDTO
{
    [JsonPropertyName("section_name")]
    public string SectionName { get; set; } = string.Empty;

    [JsonPropertyName("display_order")]
    public int DisplayOrder { get; set; }

    [JsonPropertyName("fields")]
    public List<TemplateFieldDTO> Fields { get; set; } = new();
}

public class TemplateFieldDTO
{
    [JsonPropertyName("field_name")]
    public string FieldName { get; set; } = string.Empty;

    [JsonPropertyName("field_label")]
    public string FieldLabel { get; set; } = string.Empty;

    [JsonPropertyName("field_type")]
    public FieldType FieldType { get; set; }

    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    [JsonPropertyName("normal_range_min")]
    public decimal? NormalRangeMin { get; set; }

    [JsonPropertyName("normal_range_max")]
    public decimal? NormalRangeMax { get; set; }

    [JsonPropertyName("is_required")]
    public bool IsRequired { get; set; }

    [JsonPropertyName("display_order")]
    public int DisplayOrder { get; set; }

    [JsonPropertyName("dropdown_options")]
    public List<string>? DropdownOptions { get; set; }

    [JsonPropertyName("is_retired")]
    public bool IsRetired { get; set; } = false;
    // Set to true when a doctor deletes this field during consultation.
    // Retired fields are excluded from analysis but NOT deleted from
    // the schema — historical data integrity is preserved.

    [JsonPropertyName("rename_history")]
    public List<string>? RenameHistory { get; set; }
    // Ordered list of previous FieldLabel values, oldest first.
    // e.g. ["Blood Sugar", "Glucose"] means field was renamed twice.
    // Used by ExtractCustomAttributes to unify historical records
    // that used the old label with current records using the new label.
}
