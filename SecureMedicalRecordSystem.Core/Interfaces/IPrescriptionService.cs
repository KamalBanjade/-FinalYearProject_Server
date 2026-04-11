using SecureMedicalRecordSystem.Core.Entities;

namespace SecureMedicalRecordSystem.Core.Interfaces;

public interface IPrescriptionService
{
    Task<List<Prescription>> GetPrescriptionsForRecordAsync(Guid recordId);
    Task<List<Prescription>> GetAllPrescriptionsForPatientAsync(Guid patientId);
    Task SeedPrescriptionsFromTreatmentPlanAsync(Guid recordId, string treatmentPlanText);
    Task<MasterMedication?> GetMedicationMetadataAsync(string medicationName);
}
