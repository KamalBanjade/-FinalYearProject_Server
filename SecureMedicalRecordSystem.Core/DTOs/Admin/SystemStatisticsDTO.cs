namespace SecureMedicalRecordSystem.Core.DTOs.Admin;

public class SystemStatisticsDTO
{
    public string AdminFirstName { get; set; } = string.Empty;
    // User counts
    public int TotalUsers { get; set; }
    public int TotalDoctors { get; set; }
    public int TotalPatients { get; set; }
    public int TotalAdmins { get; set; }
    public int ActiveUsers { get; set; }
    public int NewUsersThisMonth { get; set; }

    // Record stats
    public int TotalRecordsUploaded { get; set; }
    public int TotalRecordsDraft { get; set; }
    public int TotalRecordsPending { get; set; }
    public int TotalRecordsCertified { get; set; }
    public int TotalRecordsEmergency { get; set; }
    public int TotalRecordsArchived { get; set; }
    public int RecordsUploadedThisMonth { get; set; }

    // QR / Access stats
    public int TotalQRScans { get; set; }
    public int NormalQRScans { get; set; }
    public int EmergencyQRScans { get; set; }
    public int ActiveAccessSessions { get; set; }

    // Appointment stats
    public int TotalAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int UpcomingAppointments { get; set; }

    // Audit stats
    public int TotalAuditLogs { get; set; }
    public int CriticalEvents24h { get; set; }
    public int WarningEvents24h { get; set; }
    public List<RecentCriticalEventDTO> RecentCriticalEvents { get; set; } = new();

    // Trend data for charts
    public List<TimeSeriesDataPointDTO> UserGrowthTrend { get; set; } = new();
    public List<TimeSeriesDataPointDTO> QRScanTrend { get; set; } = new();
}

public class TimeSeriesDataPointDTO
{
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
    public int Value2 { get; set; }
    public int Value3 { get; set; }
    public int Value4 { get; set; }
    public bool IsActivityDay { get; set; }
}

public class RecentCriticalEventDTO
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public DateTime Timestamp { get; set; }
    public string Severity { get; set; } = string.Empty;
}

public class SecurityAlertDTO
{
    public string AlertType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;  // Low, Medium, High, Critical
    public DateTime DetectedAt { get; set; }
    public Guid? RelatedUserId { get; set; }
    public string? RelatedUserEmail { get; set; }
    public string? RelatedUserName { get; set; }
    public int EventCount { get; set; }
    public string? IPAddress { get; set; }
}
