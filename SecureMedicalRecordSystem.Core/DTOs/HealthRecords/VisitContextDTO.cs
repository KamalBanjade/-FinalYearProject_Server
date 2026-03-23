using SecureMedicalRecordSystem.Core.Enums;
using System.Collections.Generic;

namespace SecureMedicalRecordSystem.Core.DTOs.HealthRecords;

public class VisitContextDTO
{
    public VisitType Type { get; set; }
    public int DaysSinceLastVisit { get; set; }
    public string? LastDiagnosis { get; set; }
    public bool PrePopulateVitals { get; set; }
    public bool PrePopulateProtocol { get; set; }
    
    public VitalsComparisonDTO VitalsComparison { get; set; } = new();
    public ProtocolDTO? ProtocolToLoad { get; set; }
    public HealthRecordDTO? PreviousRecord { get; set; }
}
