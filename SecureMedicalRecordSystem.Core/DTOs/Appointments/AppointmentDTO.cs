using SecureMedicalRecordSystem.Core.Enums;

namespace SecureMedicalRecordSystem.Core.DTOs.Appointments;

public class AppointmentDTO
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int PatientAge { get; set; }
    public string PatientGender { get; set; } = string.Empty;
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string DoctorDepartment { get; set; } = string.Empty;
    public string? DoctorProfilePictureUrl { get; set; }
    public DateTime AppointmentDate { get; set; }
    public int Duration { get; set; }
    public string Status { get; set; } = string.Empty; // "Scheduled", "Confirmed", etc.
    public string? ReasonForVisit { get; set; }
    public string? ConsultationNotes { get; set; }
    public int LinkedRecordsCount { get; set; }
    public List<LinkedRecordSummaryDTO> LinkedRecords { get; set; } = new();
    public bool CanCancel { get; set; }
    public bool CanReschedule { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class LinkedRecordSummaryDTO
{
    public Guid RecordId { get; set; }
    public string RecordFileName { get; set; } = string.Empty;
    public string RecordType { get; set; } = string.Empty;
    public DateTime LinkedAt { get; set; }
    public string? Notes { get; set; }
}
