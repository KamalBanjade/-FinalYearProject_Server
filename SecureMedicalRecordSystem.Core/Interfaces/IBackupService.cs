using SecureMedicalRecordSystem.Core.DTOs.Admin;

namespace SecureMedicalRecordSystem.Core.Interfaces;

public interface IBackupService
{
    Task<byte[]> GenerateDatabaseSnapshotAsync(string adminUserId);
}
