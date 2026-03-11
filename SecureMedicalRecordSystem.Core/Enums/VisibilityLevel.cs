namespace SecureMedicalRecordSystem.Core.Enums;

public enum VisibilityLevel
{
    Private = 0,     // Only creator can see/use
    Department = 1,  // All doctors in same department
    Hospital = 2     // All doctors in hospital (admin-approved)
}
