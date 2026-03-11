using SecureMedicalRecordSystem.Core.DTOs.HealthRecords;
using SecureMedicalRecordSystem.Core.Enums;

namespace SecureMedicalRecordSystem.Core.Interfaces;

public interface ITemplateService
{
    Task<(bool Success, string Message, TemplateDTO? Data)> CreateTemplateAsync(
        CreateTemplateDTO request,
        Guid creatorId);

    Task<(bool Success, string Message, TemplateDTO? Data)> CreateTemplateFromRecordAsync(
        Guid recordId,
        string templateName,
        string description,
        VisibilityLevel visibility,
        Guid creatorId);

    Task<(bool Success, string Message, TemplateDTO? Data)> UpdateTemplateAsync(
        Guid templateId,
        UpdateTemplateDTO request,
        Guid modifierId);

    Task<(bool Success, string Message, List<TemplateDTO>? Data)> GetDoctorTemplatesAsync(
        Guid doctorId,
        bool includeShared = true);

    Task<(bool Success, string Message, List<TemplateDTO>? Data)> SuggestTemplatesAsync(
        string chiefComplaint,
        Guid doctorId);

    Task<(bool Success, string Message)> DeleteTemplateAsync(
        Guid templateId,
        Guid doctorId);

    Task<(bool Success, string Message, TemplateDTO? Data)> ForkTemplateAsync(
        Guid sourceTemplateId,
        string newTemplateName,
        Guid doctorId);

    Task<(bool Success, string Message, TemplateDTO? Data)> GetTemplateAsync(
        Guid templateId,
        Guid doctorId);

    Task<(bool Success, string Message, object? Data)> GetTemplateUsageStatsAsync(
        Guid templateId,
        Guid doctorId);
}
