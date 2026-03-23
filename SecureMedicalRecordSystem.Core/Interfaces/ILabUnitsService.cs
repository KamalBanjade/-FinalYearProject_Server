using SecureMedicalRecordSystem.Core.DTOs.LabUnits;

namespace SecureMedicalRecordSystem.Core.Interfaces;

public interface ILabUnitsService
{
    Task<List<LabUnitDTO>> SearchLabUnitsAsync(string query);
    Task<LabUnitDTO?> GetLabUnitByIdAsync(Guid id);
    Task<(bool Success, string Message, LabUnitDTO? Data)> CreateCustomLabUnitAsync(CreateCustomLabUnitDTO request, Guid doctorId);
}
