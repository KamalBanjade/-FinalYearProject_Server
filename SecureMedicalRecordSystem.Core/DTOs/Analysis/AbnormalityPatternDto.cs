namespace SecureMedicalRecordSystem.Core.DTOs.Analysis;

public class AbnormalityPatternDto
{
    public string VitalName { get; set; } = string.Empty;
    public int MaxConsecutiveAbnormalVisits { get; set; }
    public List<AbnormalStreakDto> Streaks { get; set; } = new();
}

public class AbnormalStreakDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int ConsecutiveCount { get; set; }
    public List<double> Values { get; set; } = new();
}
