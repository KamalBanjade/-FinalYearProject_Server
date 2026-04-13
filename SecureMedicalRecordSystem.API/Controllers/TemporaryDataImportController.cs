using Microsoft.AspNetCore.Mvc;
using SecureMedicalRecordSystem.Infrastructure.Data;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.DTOs.HealthRecords;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SecureMedicalRecordSystem.API.Controllers;

// [TEMPORARY] - Delete this controller after successful import.
[ApiController]
[Route("api/[controller]")]
public class TemporaryDataImportController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly SecureMedicalRecordSystem.Core.Interfaces.IPrescriptionService _prescriptionService;
    private readonly SecureMedicalRecordSystem.Core.Interfaces.IHealthRecordService _healthRecordService;

    // Updated with actual User IDs
    private static readonly Guid PATIENT_ID_HERE = Guid.Parse("efade380-8761-4ef3-98f4-885ff4712084");
    private static readonly Guid DOCTOR_ID_HERE = Guid.Parse("daec9952-a9ad-4e94-bd3c-fa7f34cdb778");

    public TemporaryDataImportController(
        ApplicationDbContext context, 
        SecureMedicalRecordSystem.Core.Interfaces.IPrescriptionService prescriptionService,
        SecureMedicalRecordSystem.Core.Interfaces.IHealthRecordService healthRecordService)
    {
        _context = context;
        _prescriptionService = prescriptionService;
        _healthRecordService = healthRecordService;
    }

    [HttpPost("import-30-days")]
    public async Task<IActionResult> ImportCsvData()
    {
        if (PATIENT_ID_HERE == Guid.Empty || DOCTOR_ID_HERE == Guid.Empty)
        {
            return BadRequest("You must update PATIENT_ID_HERE and DOCTOR_ID_HERE inside TemporaryDataImportController.cs before running this script.");
        }

        // --- AUTO-CLEANUP FOR RE-RUNS ---
        var cutoff = DateTime.UtcNow.AddDays(-31);
        var existingRecords = await _context.PatientHealthRecords
            .Where(r => r.PatientId == PATIENT_ID_HERE && r.CreatedAt >= cutoff)
            .ToListAsync();

        if (existingRecords.Any())
        {
            _context.PatientHealthRecords.RemoveRange(existingRecords);
            // This will auto-cascade and delete the orphaned CustomAttributes/Prescriptions too!
            await _context.SaveChangesAsync();
        }

        // --- TEMPLATE LOOKUP ---
        var template = await _context.Templates
            .FirstOrDefaultAsync(t => t.TemplateName == "Type 2 Diabetes");

        if (template == null || string.IsNullOrEmpty(template.TemplateSchema))
        {
            return NotFound("Template 'Type 2 Diabetes' not found or has no schema. Cannot map custom attributes safely.");
        }

        TemplateSchemaDTO? schema;
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            schema = JsonSerializer.Deserialize<TemplateSchemaDTO>(template.TemplateSchema, options);
        }
        catch (Exception ex) { return BadRequest($"Malformed TemplateSchema format: {ex.Message}"); }

        if (schema == null) return BadRequest("Schema deserialized to null.");

        // Flatten template fields for easy O(1) label lookups
        var templateFieldsMap = schema.Sections
            .SelectMany(s => s.Fields.Select(f => new { s.SectionName, Field = f }))
            .ToDictionary(k => k.Field.FieldLabel, k => k, StringComparer.OrdinalIgnoreCase);

        // --- TARGET CSV ---
        // Project root should be d:\finalyearproject\Source Code\Server\...
        // the API working dir is d:\finalyearproject\Source Code\Server\SecureMedicalRecordSystem.API
        var projectRootPath = Path.Combine(Directory.GetCurrentDirectory(), "..");
        var csvPath = Path.Combine(projectRootPath, "patient_medical_records_cleaned_2.csv");

        if (!System.IO.File.Exists(csvPath))
        {
            return NotFound($"CSV File not found at path: {Path.GetFullPath(csvPath)}. Please verify the name and location.");
        }

        var records = new List<PatientHealthRecord>();

        // --- CSV PARSING ---
        using var reader = new StreamReader(csvPath);
        string? headerLine = await reader.ReadLineAsync();
        
        if (string.IsNullOrEmpty(headerLine)) return BadRequest("CSV File is empty or headers are missing.");

        string[] headers = headerLine.Split(',');

        int rowIndex = 1; // 1-indexed for the chron logic (-30 + 1)
        while (!reader.EndOfStream)
        {
            string? line = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(line)) continue;
            
            string[] fields = line.Split(',');

            var newRecord = new PatientHealthRecord
            {
                PatientId = PATIENT_ID_HERE,
                DoctorId = DOCTOR_ID_HERE,
                RecordDate = DateTime.UtcNow.AddDays(-30 + rowIndex), // Chronological Stamping
                TemplateId = template.Id,
                TemplateSnapshot = template.TemplateSchema,
                CreatedFromScratch = false,
                IsStructured = true,
                CreatedAt = DateTime.UtcNow,
                CustomAttributes = new List<HealthAttribute>()
            };

            for (int i = 0; i < headers.Length; i++)
            {
                if (i >= fields.Length) break;

                string header = headers[i].Trim();
                string val = fields[i].Trim();
                if (string.IsNullOrEmpty(val)) continue;

                // --- CATEGORY 4: IGNORED COLUMNS ---
                if (header.Equals("Appointment_No", StringComparison.OrdinalIgnoreCase) ||
                    header.Equals("Clinical_Stage", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // --- CATEGORY 3: CLINICAL NOTES ---
                if (header.Equals("Private_Doctor_Observations", StringComparison.OrdinalIgnoreCase))
                    { newRecord.DoctorNotes = val; continue; }
                if (header.Equals("Management_Plan", StringComparison.OrdinalIgnoreCase))
                    { newRecord.TreatmentPlan = val; continue; }
                if (header.Equals("Final_Diagnosis", StringComparison.OrdinalIgnoreCase))
                    { newRecord.Diagnosis = val; continue; }

                // --- CATEGORY 1: BASE VITAL COLUMNS ---
                if (header.Equals("Systolic Blood Pressure", StringComparison.OrdinalIgnoreCase))
                    { if (decimal.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal v)) newRecord.BloodPressureSystolic = (int)Math.Round(v); continue; }
                if (header.Equals("Diastolic Blood Pressure", StringComparison.OrdinalIgnoreCase))
                    { if (decimal.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal v)) newRecord.BloodPressureDiastolic = (int)Math.Round(v); continue; }
                if (header.Equals("Heart Rate", StringComparison.OrdinalIgnoreCase))
                    { if (decimal.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal v)) newRecord.HeartRate = (int)Math.Round(v); continue; }
                if (header.Equals("SpO2", StringComparison.OrdinalIgnoreCase))
                    { if (decimal.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal v)) newRecord.SpO2 = v; continue; }
                if (header.Equals("Body Temperature", StringComparison.OrdinalIgnoreCase))
                    { if (decimal.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal v)) newRecord.Temperature = v; continue; }
                if (header.Equals("Weight", StringComparison.OrdinalIgnoreCase))
                    { if (decimal.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal v)) newRecord.Weight = v; continue; }
                if (header.Equals("Height", StringComparison.OrdinalIgnoreCase))
                    { if (decimal.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal v)) newRecord.Height = v; continue; }
                if (header.Equals("BMI", StringComparison.OrdinalIgnoreCase))
                    { if (decimal.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal v)) newRecord.BMI = v; continue; }



                // --- CATEGORY 2: TEMPLATE LAB FIELDS (CUSTOM ATTRIBUTES) ---
                if (templateFieldsMap.TryGetValue(header, out var match))
                {
                    newRecord.CustomAttributes.Add(new HealthAttribute
                    {
                        SectionName = match.SectionName,
                        FieldName = match.Field.FieldName,
                        FieldLabel = match.Field.FieldLabel,
                        FieldType = match.Field.FieldType,
                        FieldValue = val,
                        FieldUnit = match.Field.Unit,
                        NormalRangeMin = match.Field.NormalRangeMin,
                        NormalRangeMax = match.Field.NormalRangeMax,
                        IsRequired = match.Field.IsRequired,
                        DisplayOrder = match.Field.DisplayOrder,
                        IsFromTemplate = true,
                        AddedBy = DOCTOR_ID_HERE,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    // Edge case: "Waist Circumference" was mentioned as a category 1 but it's not a base entity property and not in the schema. Check if it's meant to be a custom attribute.
                    if (header.Equals("Waist Circumference", StringComparison.OrdinalIgnoreCase))
                    {
                        newRecord.CustomAttributes.Add(new HealthAttribute
                        {
                            SectionName = "Vital Signs",
                            FieldName = "waist_circumference",
                            FieldLabel = "Waist Circumference",
                            FieldType = SecureMedicalRecordSystem.Core.Enums.FieldType.Number,
                            FieldValue = val,
                            FieldUnit = "cm",
                            IsFromTemplate = false,
                            AddedBy = DOCTOR_ID_HERE,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            records.Add(newRecord);
            rowIndex++;
        }

        _context.PatientHealthRecords.AddRange(records);
        await _context.SaveChangesAsync();

        return Ok($"Successfully parsed and inserted {records.Count} chronologically spaced records based on the Type 2 Diabetes template.");
    }

    [HttpPost("extract-medications")]
    public async Task<IActionResult> ExtractMedications()
    {
        if (PATIENT_ID_HERE == Guid.Empty) return BadRequest("Set PATIENT_ID_HERE.");

        // Grab records from the last 30 days we just imported
        var recentRecords = await _context.PatientHealthRecords
            .Where(r => r.PatientId == PATIENT_ID_HERE && !string.IsNullOrEmpty(r.TreatmentPlan))
            .ToListAsync();

        int seededCount = 0;
        foreach (var r in recentRecords)
        {
            // The service checks if it's already seeded internally, so it's safe to call unconditionally!
            await _prescriptionService.SeedPrescriptionsFromTreatmentPlanAsync(r.Id, r.TreatmentPlan!);
            seededCount++;
        }

        return Ok($"Scanned {seededCount} records and extracted medications based on the Master Dictionary.");
    }

    [HttpPost("generate-documents")]
    public async Task<IActionResult> GenerateDocuments()
    {
        if (PATIENT_ID_HERE == Guid.Empty) return BadRequest("Set PATIENT_ID_HERE.");

        // Grab records from the last 30 days we just imported
        var recentRecords = await _context.PatientHealthRecords
            .Where(r => r.PatientId == PATIENT_ID_HERE && r.GeneratedPdfPath == null)
            .OrderBy(r => r.RecordDate)
            .ToListAsync();

        int generatedCount = 0;
        foreach (var r in recentRecords)
        {
            // Generate PDF Report. Internally, this creates and binds the `MedicalRecord` entry!
            try
            {
                var pdfUrl = await _healthRecordService.GeneratePdfReportAsync(r.Id);
                r.GeneratedPdfPath = pdfUrl;
                generatedCount++;
            }
            catch (Exception ex)
            {
                // Skip on single failure so others can proceed
                Console.WriteLine($"Failed to generate DB PDF for record {r.Id}: {ex.Message}");
            }
        }
        await _context.SaveChangesAsync();

        return Ok($"Successfully generated {generatedCount} Medical Record Document files and pushed them to the Storage Bucket.");
    }

    [HttpPost("sync-timestamps")]
    public async Task<IActionResult> SyncTimestamps()
    {
        if (PATIENT_ID_HERE == Guid.Empty) return BadRequest("Set PATIENT_ID_HERE.");

        var recentRecords = await _context.PatientHealthRecords
            .Include(r => r.CustomAttributes)
            .Include(r => r.Prescriptions)
            .Where(r => r.PatientId == PATIENT_ID_HERE)
            .ToListAsync();

        int syncedCount = 0;
        foreach (var r in recentRecords)
        {
            // 1. Sync Base Record
            r.CreatedAt = r.RecordDate;

            // 2. Sync Attributes
            if (r.CustomAttributes != null)
            {
                foreach (var attr in r.CustomAttributes)
                    attr.CreatedAt = r.RecordDate;
            }

            // 3. Sync Prescriptions
            if (r.Prescriptions != null)
            {
                foreach (var med in r.Prescriptions)
                    med.CreatedAt = r.RecordDate;
            }

            // 4. Sync Medical Documents linked to this Record
            if (!string.IsNullOrEmpty(r.GeneratedPdfPath))
            {
                var doc = await _context.MedicalRecords
                    .FirstOrDefaultAsync(m => m.S3ObjectKey == r.GeneratedPdfPath || m.OriginalFileName.Contains(r.Id.ToString()));
                
                if (doc != null)
                {
                    doc.CreatedAt = r.RecordDate;
                    doc.UploadedAt = r.RecordDate;
                    doc.RecordDate = r.RecordDate;
                }
            }
            syncedCount++;
        }

        await _context.SaveChangesAsync();
        return Ok($"Successfully time-shifted {syncedCount} records and all their associated documents, labs, and medications backwards across the 30-day timeline!");
    }
}
