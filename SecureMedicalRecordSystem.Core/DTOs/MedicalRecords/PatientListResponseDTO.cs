using System;

namespace SecureMedicalRecordSystem.Core.DTOs.MedicalRecords;

public class PatientListResponseDTO
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? BloodType { get; set; }
    public string? Allergies { get; set; }
    
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    
    public int SharedRecordsCount { get; set; }
    public int AppointmentCount { get; set; }
    public DateTime? LatestSharedRecordDate { get; set; }
    
    public DateTime? LastAppointmentDate { get; set; }
    public bool IsPrimary { get; set; }
}
