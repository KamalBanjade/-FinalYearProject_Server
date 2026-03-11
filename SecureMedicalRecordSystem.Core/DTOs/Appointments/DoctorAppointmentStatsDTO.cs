using System;

namespace SecureMedicalRecordSystem.Core.DTOs.Appointments;

public class DoctorAppointmentStatsDTO
{
    public int TotalAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int UpcomingAppointments { get; set; }
    public int CancelledAppointments { get; set; }
    public int PendingConfirmation { get; set; }
    public int TodayAppointments { get; set; }
}
