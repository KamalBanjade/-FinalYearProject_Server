using System;

namespace SecureMedicalRecordSystem.Infrastructure.Utils;

public static class DateGroupingHelper
{
    public static string GetTimePeriodLabel(DateTime uploadedAt)
    {
        var now = DateTime.UtcNow;
        var diff = now - uploadedAt;

        if (diff.TotalDays <= 7)
            return "THIS_WEEK";

        if (uploadedAt.Month == now.Month && uploadedAt.Year == now.Year)
            return "THIS_MONTH";

        if (uploadedAt.Year == now.Year)
            return "EARLIER_THIS_YEAR";

        if (uploadedAt.Year == now.Year - 1)
            return "LAST_YEAR";

        return "OLDER";
    }

    public static string GetRelativeTimeString(DateTime uploadedAt, string? timezone = null)
    {
        // For simplicity, we assume the input is already in the preferred timezone or we use UTC
        // In a real app, we'd use the patient's timezone offset
        var now = DateTime.UtcNow;
        var diff = now - uploadedAt;

        if (uploadedAt.Date == now.Date)
            return $"Today at {uploadedAt:h:mm tt}";

        if (uploadedAt.Date == now.Date.AddDays(-1))
            return $"Yesterday at {uploadedAt:h:mm tt}";

        if (diff.TotalDays <= 7)
            return $"{(int)Math.Floor(diff.TotalDays)} days ago ({uploadedAt:MMM d})";

        if (uploadedAt.Month == now.Month && uploadedAt.Year == now.Year)
        {
            int weeks = (int)Math.Ceiling(diff.TotalDays / 7);
            return $"{weeks} weeks ago ({uploadedAt:MMM d})";
        }

        if (uploadedAt.Year == now.Year)
            return uploadedAt.ToString("MMMM d");

        if (uploadedAt.Year == now.Year - 1)
            return uploadedAt.ToString("MMMM yyyy");

        return uploadedAt.ToString("yyyy");
    }

    public static string GetSectionDisplayName(string timePeriod, DateTime? referenceDate = null)
    {
        var date = referenceDate ?? DateTime.UtcNow;
        
        return timePeriod switch
        {
            "THIS_WEEK" => "📌 THIS WEEK",
            "THIS_MONTH" => $"📌 THIS MONTH - {date:MMMM yyyy}".ToUpper(),
            "EARLIER_THIS_YEAR" => "📌 EARLIER THIS YEAR",
            "LAST_YEAR" => $"📌 LAST YEAR ({date.Year - 1})",
            "OLDER" => "📌 OLDER RECORDS",
            _ => "📌 RECORDS"
        };
    }
}
