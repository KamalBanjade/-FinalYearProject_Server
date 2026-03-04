using SecureMedicalRecordSystem.Core.DTOs.MedicalRecords;

namespace SecureMedicalRecordSystem.Core.Interfaces;

public interface IMedicalRecordsService
{
    Task<(bool Success, string Message, MedicalRecordResponseDTO? Data)> UploadRecordAsync(
        Guid patientId, 
        UploadMedicalRecordDTO uploadDto);

    Task<(bool Success, string Message, Stream? FileStream, string? FileName, string? ContentType)> DownloadRecordAsync(
        Guid recordId, 
        Guid requestingUserId);

    Task<(bool Success, string Message, List<MedicalRecordResponseDTO>? Data)> GetPatientRecordsAsync(
        Guid patientId, 
        Guid requestingUserId);

    Task<(bool Success, string Message, MedicalRecordResponseDTO? Data)> GetRecordDetailsAsync(
        Guid recordId, 
        Guid requestingUserId);

    Task<(bool Success, string Message, List<MedicalRecordResponseDTO>? Data)> GetPendingRecordsForDoctorAsync(
        Guid doctorUserId);

    Task<(bool Success, string Message, List<MedicalRecordResponseDTO>? Data)> GetCertifiedRecordsForDoctorAsync(
        Guid doctorUserId);

    Task<(bool Success, string Message)> UpdateRecordMetadataAsync(
        Guid recordId, 
        UpdateMedicalRecordMetadataDTO updateDto, 
        Guid requestingUserId);

    Task<(bool Success, string Message)> DeleteRecordAsync(
        Guid recordId, 
        Guid requestingUserId);

    // FSM Transitions
    Task<(bool Success, string Message, MedicalRecordResponseDTO? Data)> SubmitForReviewAsync(
        Guid recordId, 
        Guid patientUserId);

    Task<(bool Success, string Message, MedicalRecordResponseDTO? Data)> CertifyRecordAsync(
        Guid recordId, 
        Guid doctorUserId, 
        CertifyRecordDTO dto);

    Task<(bool Success, string Message, MedicalRecordResponseDTO? Data)> RejectRecordAsync(
        Guid recordId, 
        Guid doctorUserId, 
        RejectRecordDTO dto);

    Task<(bool Success, string Message, MedicalRecordResponseDTO? Data)> ArchiveRecordAsync(
        Guid recordId, 
        Guid requestingUserId);

    Task<(bool Success, string Message, VerificationResultDTO? Data)> VerifyRecordSignatureAsync(
        Guid recordId, 
        Guid requestingUserId);

    // Smart Assignment
    Task<(bool Success, string Message, SmartDoctorSuggestionDTO? Data)> GetSmartDoctorSuggestionsAsync(Guid patientId);
}

