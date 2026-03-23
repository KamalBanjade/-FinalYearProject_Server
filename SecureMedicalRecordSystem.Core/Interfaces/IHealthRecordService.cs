using SecureMedicalRecordSystem.Core.DTOs.HealthRecords;

namespace SecureMedicalRecordSystem.Core.Interfaces;

public interface IHealthRecordService
{
    Task<(bool Success, string Message, HealthRecordDTO? Data)> CreateStructuredRecordAsync(
        CreateHealthRecordDTO request,
        Guid doctorId);

    Task<(bool Success, string Message, HealthRecordDTO? Data)> UpdateStructuredRecordAsync(
        Guid recordId,
        UpdateHealthRecordDTO request,
        Guid doctorId);

    Task<(bool Success, string Message, HealthRecordDTO? Data)> GetStructuredRecordAsync(
        Guid recordId,
        Guid requestingUserId);

    Task<(bool Success, string Message, List<HealthRecordDTO>? Data)> GetPatientStructuredRecordsAsync(
        Guid patientId,
        Guid requestingUserId,
        DateTime? startDate = null,
        DateTime? endDate = null);

    Task<(bool Success, string Message)> DeleteStructuredRecordAsync(
        Guid recordId,
        Guid doctorId);

    Task<(bool Success, string Message)> AddCustomAttributeAsync(
        Guid recordId,
        AddAttributeDTO request,
        Guid doctorId);

    Task<(bool Success, string Message)> RemoveAttributeAsync(
        Guid attributeId,
        Guid doctorId);

    Task<string> GeneratePdfReportAsync(Guid recordId);

    Task<Dictionary<string, object>> ExportForAIAnalysisAsync(
        Guid patientId,
        DateTime? startDate = null,
        DateTime? endDate = null);

    Task<VisitContextDTO> GetVisitContextAsync(Guid patientId);
}
