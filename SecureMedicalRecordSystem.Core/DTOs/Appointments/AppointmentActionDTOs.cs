using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.DTOs.Appointments;

public class RescheduleAppointmentDTO
{
    [Required]
    public DateTime NewAppointmentDate { get; set; }
}

public class CancelAppointmentDTO
{
    [Required]
    [MaxLength(200)]
    public string CancellationReason { get; set; } = string.Empty;
}

public class CompleteAppointmentDTO
{
    [Required]
    [MaxLength(1000)]
    public string ConsultationNotes { get; set; } = string.Empty;
}

public class LinkRecordDTO
{
    [Required]
    public Guid MedicalRecordId { get; set; }

    [MaxLength(200)]
    public string? Notes { get; set; }
}
