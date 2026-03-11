namespace SecureMedicalRecordSystem.API.Hubs;

public interface IScannerHubClient
{
    Task DesktopRegistered(object data);
    Task MobilePaired(object data);
    Task PatientScanned(object data);
    Task ScanError(string message);
}
