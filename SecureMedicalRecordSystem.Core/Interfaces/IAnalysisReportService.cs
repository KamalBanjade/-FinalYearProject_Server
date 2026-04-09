namespace SecureMedicalRecordSystem.Core.Interfaces;

using SecureMedicalRecordSystem.Core.Entities;

public interface IAnalysisReportService
{
    Task<AnalysisReport> GenerateAndStoreReportAsync(
        Guid patientId, 
        Guid doctorId, 
        string patientFullName);
        
    Task<(Stream decryptedStream, string fileName)> DownloadReportAsync(Guid reportId);
    
    Task<List<AnalysisReport>> GetReportsForPatientAsync(Guid patientId);
}
