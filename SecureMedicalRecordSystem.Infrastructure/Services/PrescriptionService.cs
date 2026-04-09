using Microsoft.EntityFrameworkCore;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Data;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class PrescriptionService : IPrescriptionService
{
    private readonly ApplicationDbContext _context;

    public PrescriptionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Prescription>> GetPrescriptionsForRecordAsync(Guid recordId)
    {
        return await _context.Prescriptions
            .Where(p => p.PatientHealthRecordId == recordId && !p.IsDeleted)
            .ToListAsync();
    }

    public async Task<List<Prescription>> GetAllPrescriptionsForPatientAsync(Guid patientId)
    {
        return await _context.Prescriptions
            .Include(p => p.PatientHealthRecord)
            .Where(p => p.PatientHealthRecord.PatientId == patientId && !p.IsDeleted)
            .OrderBy(p => p.PatientHealthRecord.RecordDate)
            .ToListAsync();
    }

    public async Task SeedPrescriptionsFromTreatmentPlanAsync(Guid recordId, string treatmentPlanText)
    {
        // Only seed if no prescriptions exist for this record yet
        bool alreadySeeded = await _context.Prescriptions
            .AnyAsync(p => p.PatientHealthRecordId == recordId && !p.IsDeleted);

        if (alreadySeeded) return;

        var medications = ExtractMedications(treatmentPlanText);

        foreach (var med in medications)
        {
            _context.Prescriptions.Add(new Prescription
            {
                Id = Guid.NewGuid(),
                PatientHealthRecordId = recordId,
                MedicationName = med,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system-seeder"
            });
        }

        await _context.SaveChangesAsync();
    }

    // Replicated from HealthRecordService.ExtractMedications() — kept local to avoid cross-service coupling
    private static List<string> ExtractMedications(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();

        var knownMedications = new[]
        {
            "metformin", "amlodipine", "lisinopril", "atorvastatin", "aspirin",
            "omeprazole", "paracetamol", "ibuprofen", "amoxicillin", "insulin"
        };

        var lower = text.ToLower();
        return knownMedications.Where(m => lower.Contains(m)).ToList();
    }
}
