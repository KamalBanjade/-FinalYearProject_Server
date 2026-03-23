namespace SecureMedicalRecordSystem.Core.Enums;

public enum VisitType
{
    FirstVisit,      // No previous records found
    FollowUp,        // Within 90 days of last visit
    RoutineCheckup,  // Between 90 and 365 days
    LongGapVisit     // More than 365 days
}
