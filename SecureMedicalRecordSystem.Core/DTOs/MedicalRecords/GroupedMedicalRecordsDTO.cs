using System;
using System.Collections.Generic;

namespace SecureMedicalRecordSystem.Core.DTOs.MedicalRecords;

public class GroupedMedicalRecordsDTO
{
    public int TotalCount { get; set; }
    public List<RecordSectionDTO> Sections { get; set; } = new();
}

public class RecordSectionDTO
{
    public string TimePeriod { get; set; } = string.Empty; // THIS_WEEK, THIS_MONTH, etc.
    public string DisplayName { get; set; } = string.Empty; // 📌 THIS WEEK
    public int RecordCount { get; set; }
    public bool IsExpanded { get; set; }
    public List<MedicalRecordResponseDTO> Records { get; set; } = new();
}
