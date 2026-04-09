namespace SecureMedicalRecordSystem.Core.DTOs.Admin;

public class SystemSnapshotDto
{
    public DateTime SnapshotGeneratedAt { get; set; } = DateTime.UtcNow;
    public string GeneratedByAdminId { get; set; } = string.Empty;
    public string Environment { get; set; } = "Production";
    
    // Core Relational Data
    public int TotalPatients { get; set; }
    public int TotalDoctors { get; set; }
    public int TotalRecords { get; set; }
    public int TotalAppointments { get; set; }
    public int TotalAuditLogs { get; set; }

    // Raw Data Dumps (Serialized as lists)
    public List<PatientSnapshot> Patients { get; set; } = new();
    public List<DoctorSnapshot> Doctors { get; set; } = new();
    public List<RecordSnapshot> MedicalRecords { get; set; } = new();
    public List<AppointmentSnapshot> Appointments { get; set; } = new();
}

public class PatientSnapshot
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class DoctorSnapshot
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class RecordSnapshot
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid? DoctorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AppointmentSnapshot
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
