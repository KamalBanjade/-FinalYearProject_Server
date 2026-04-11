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

        var medications = await ExtractMedicationsFromDbAsync(treatmentPlanText);

        foreach (var (canonicalName, drugCategory) in medications)
        {
            _context.Prescriptions.Add(new Prescription
            {
                Id = Guid.NewGuid(),
                PatientHealthRecordId = recordId,
                MedicationName = canonicalName, // always the canonical name
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system-seeder"
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task<MasterMedication?> GetMedicationMetadataAsync(string medicationName)
    {
        var lower = medicationName.ToLower();
        var allMeds = await _context.MasterMedications
            .AsNoTracking()
            .ToListAsync();

        return allMeds.FirstOrDefault(m =>
            m.Name.ToLower() == lower ||
            (!string.IsNullOrEmpty(m.Aliases) &&
             (System.Text.Json.JsonSerializer
                 .Deserialize<List<string>>(m.Aliases) ?? new List<string>())
                 .Any(a => a.ToLower() == lower)));
    }

    private async Task<List<(string CanonicalName, string DrugCategory)>>
        ExtractMedicationsFromDbAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<(string, string)>();

        var lower = text.ToLower();
        var allMedications = await _context.MasterMedications
            .AsNoTracking()
            .ToListAsync();

        var matched = new List<(string CanonicalName, string DrugCategory)>();
        var seenCanonical = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var med in allMedications)
        {
            bool isMatch = lower.Contains(med.Name.ToLower());

            if (!isMatch && !string.IsNullOrEmpty(med.Aliases))
            {
                // Parse JSON alias array and check each one
                var aliases = System.Text.Json.JsonSerializer
                    .Deserialize<List<string>>(med.Aliases) ?? new List<string>();
                isMatch = aliases.Any(alias =>
                    lower.Contains(alias.ToLower()));
            }

            if (isMatch && seenCanonical.Add(med.Name))
            {
                matched.Add((med.Name, med.DrugCategory));
            }
        }

        return matched;
    }
}
