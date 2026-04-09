using Microsoft.EntityFrameworkCore;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Infrastructure.Data;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class VitalBaselineService
{
    private readonly ApplicationDbContext _context;

    public VitalBaselineService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task RecalculateBaselineAsync(Guid patientId)
    {
        var records = await _context.PatientHealthRecords
            .Where(r => r.PatientId == patientId && !r.IsDeleted)
            .OrderBy(r => r.RecordDate)
            .Take(3)
            .ToListAsync();

        if (records.Count == 0) return;

        var baseline = await _context.PatientVitalBaselines
            .FirstOrDefaultAsync(b => b.PatientId == patientId) 
            ?? new PatientVitalBaseline { Id = Guid.NewGuid(), PatientId = patientId, CreatedAt = DateTime.UtcNow, CreatedBy = "system" };

        baseline.AvgSystolic = records.Where(r => r.BloodPressureSystolic.HasValue).Select(r => (double)r.BloodPressureSystolic!.Value).DefaultIfEmpty().Average();
        baseline.AvgDiastolic = records.Where(r => r.BloodPressureDiastolic.HasValue).Select(r => (double)r.BloodPressureDiastolic!.Value).DefaultIfEmpty().Average();
        baseline.AvgHeartRate = records.Where(r => r.HeartRate.HasValue).Select(r => (double)r.HeartRate!.Value).DefaultIfEmpty().Average();
        baseline.AvgBmi = records.Where(r => r.BMI.HasValue).Select(r => (double)r.BMI!.Value).DefaultIfEmpty().Average();
        baseline.AvgSpo2 = records.Where(r => r.SpO2.HasValue).Select(r => (double)r.SpO2!.Value).DefaultIfEmpty().Average();
        baseline.AvgTemperature = records.Where(r => r.Temperature.HasValue).Select(r => (double)r.Temperature!.Value).DefaultIfEmpty().Average();
        baseline.RecordsUsedForBaseline = records.Count;
        baseline.LastCalculatedAt = DateTime.UtcNow;
        baseline.UpdatedAt = DateTime.UtcNow;
        baseline.UpdatedBy = "system";

        if (_context.Entry(baseline).State == EntityState.Detached)
        {
            _context.PatientVitalBaselines.Add(baseline);
        }
        else
        {
            _context.PatientVitalBaselines.Update(baseline);
        }
        
        await _context.SaveChangesAsync();
    }
}
