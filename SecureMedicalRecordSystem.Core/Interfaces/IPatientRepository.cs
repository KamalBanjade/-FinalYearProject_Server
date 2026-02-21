using SecureMedicalRecordSystem.Core.Entities;

namespace SecureMedicalRecordSystem.Core.Interfaces;

public interface IPatientRepository : IRepository<Patient>
{
    Task<Patient?> GetByNationalIdAsync(string nationalId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Patient>> SearchAsync(string query, CancellationToken cancellationToken = default);
    Task<Patient?> GetWithRecordsAsync(Guid patientId, CancellationToken cancellationToken = default);
}
