using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.DTOs.Appointments;

public class CreateAppointmentDTO
{
    [Required]
    public Guid DoctorId { get; set; }

    [Required]
    public DateTime AppointmentDate { get; set; }

    public int Duration { get; set; } = 30;

    [Required]
    [MaxLength(500)]
    public string ReasonForVisit { get; set; } = string.Empty;
}
