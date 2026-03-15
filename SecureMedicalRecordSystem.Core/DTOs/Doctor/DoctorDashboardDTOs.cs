using System;
using System.Collections.Generic;

namespace SecureMedicalRecordSystem.Core.DTOs.Doctor;

public class DoctorDashboardStatsDTO
{
    public string FirstName { get; set; } = string.Empty;
    public int TodayAppointments { get; set; }
    public int PendingRecords { get; set; }
    public int WeekAppointments { get; set; }
    public int MonthPatients { get; set; }
    public int RecentScans { get; set; }
    public decimal CompletionRate { get; set; }
}

public class TodayAppointmentDTO
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public DateTime AppointmentTime { get; set; }
    public string Status { get; set; } = string.Empty; // Scheduled, InProgress, Completed, Cancelled
    public string Type { get; set; } = string.Empty;
    public int Duration { get; set; }
}

public class WeekScheduleDTO
{
    public DateTime WeekStart { get; set; }
    public List<DayScheduleDTO> Days { get; set; } = new();
}

public class DayScheduleDTO
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
    public int Scheduled { get; set; }
    public int Completed { get; set; }
    public int Cancelled { get; set; }
}

public class PatientVolumeTrendDTO
{
    public DateTime Date { get; set; }
    public int PatientCount { get; set; }
}

public class RecordStatusBreakdownDTO
{
    public int Draft { get; set; }
    public int Pending { get; set; }
    public int Certified { get; set; }
    public int Total { get; set; }
}

public class RecentScanDTO
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public DateTime ScannedAt { get; set; }
    public bool IsEmergency { get; set; }
    public bool TOTPVerified { get; set; }
}

public class TemplateUsageDTO
{
    public Guid TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public int UsageCount { get; set; }
}
