using Microsoft.AspNetCore.Authorization;

namespace SecureMedicalRecordSystem.API.Authorization;

public class AdminOnlyAttribute : AuthorizeAttribute
{
    public AdminOnlyAttribute()
    {
        Policy = "AdminPolicy";
    }
}

public class DoctorOnlyAttribute : AuthorizeAttribute
{
    public DoctorOnlyAttribute()
    {
        Policy = "DoctorPolicy";
    }
}

public class PatientOnlyAttribute : AuthorizeAttribute
{
    public PatientOnlyAttribute()
    {
        Policy = "PatientPolicy";
    }
}

public class DoctorOrAdminAttribute : AuthorizeAttribute
{
    public DoctorOrAdminAttribute()
    {
        Policy = "DoctorOrAdminPolicy";
    }
}
