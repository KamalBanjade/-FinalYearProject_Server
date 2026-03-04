using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SecureMedicalRecordSystem.Core.DTOs.MedicalRecords;

public class UploadMedicalRecordDTO
{
    [Required(ErrorMessage = "Please select a file to upload.")]
    public IFormFile File { get; set; } = null!;

    [Required(ErrorMessage = "Record type is required.")]
    [MaxLength(100)]
    public string RecordType { get; set; } = string.Empty; // "Lab Report", "Prescription", etc.

    public string? Description { get; set; }

    public DateTime? RecordDate { get; set; }

    public Guid? AssignedDoctorId { get; set; }
}
