namespace SecureMedicalRecordSystem.Core.DTOs;

public class DepartmentDoctorsDTO
{
    public string Department { get; set; } = string.Empty;
    public List<DoctorBasicInfoDTO> Doctors { get; set; } = new();
}

public class DoctorBasicInfoDTO
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
