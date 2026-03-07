using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.Entities;

public class AppointmentRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AppointmentId { get; set; }
    public Guid MedicalRecordId { get; set; }

    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
    public Guid LinkedBy { get; set; }

    [MaxLength(200)]
    public string? Notes { get; set; }

    // Navigation Properties
    public Appointment Appointment { get; set; } = null!;
    public MedicalRecord MedicalRecord { get; set; } = null!;
}
