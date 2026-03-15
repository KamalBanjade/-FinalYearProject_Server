using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureMedicalRecordSystem.Core.DTOs.HealthRecords;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.Enums;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Data;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Layout.Borders;
using iText.IO.Image;
using System.Security.Cryptography;
using iText.Kernel.Colors;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class HealthRecordService : IHealthRecordService
{
    private readonly ApplicationDbContext _context;
    private readonly ITemplateService _templateService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<HealthRecordService> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITigrisStorageService _storageService;
    private readonly IEncryptionService _encryptionService;

    public HealthRecordService(
        ApplicationDbContext context,
        ITemplateService templateService,
        IAuditLogService auditLogService,
        ILogger<HealthRecordService> logger,
        UserManager<ApplicationUser> userManager,
        ITigrisStorageService storageService,
        IEncryptionService encryptionService)
    {
        _context = context;
        _templateService = templateService;
        _auditLogService = auditLogService;
        _logger = logger;
        _userManager = userManager;
        _storageService = storageService;
        _encryptionService = encryptionService;
    }

    public async Task<(bool Success, string Message, HealthRecordDTO? Data)> CreateStructuredRecordAsync(
        CreateHealthRecordDTO request, Guid doctorId)
    {
        try
        {
            // 1. Validate doctor authorization:
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.UserId == doctorId);
            
            if (doctor == null)
                return (false, "Not authorized as doctor", null);

            // 2. Validate patient exists:
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == request.PatientId);
            if (patient == null)
                return (false, "Patient not found", null);

            // 3. Create base record:
            var record = new PatientHealthRecord
            {
                Id = Guid.NewGuid(),
                PatientId = request.PatientId,
                DoctorId = doctor.Id,
                AppointmentId = request.AppointmentId,
                RecordDate = request.RecordDate ?? DateTime.UtcNow,
                RecordType = request.RecordType,
                
                // Base vitals from nested DTO
                BloodPressureSystolic = request.Vitals?.BloodPressureSystolic,
                BloodPressureDiastolic = request.Vitals?.BloodPressureDiastolic,
                HeartRate = request.Vitals?.HeartRate,
                Temperature = request.Vitals?.Temperature,
                Weight = request.Vitals?.Weight,
                Height = request.Vitals?.Height,
                SpO2 = request.Vitals?.SpO2,
                
                // Free text
                ChiefComplaint = request.ChiefComplaint,
                DoctorNotes = request.DoctorNotes,
                Diagnosis = request.Diagnosis,
                TreatmentPlan = request.TreatmentPlan,
                
                // Template tracking
                TemplateId = request.TemplateId,
                CreatedFromScratch = request.TemplateId == null,
                
                // Metadata
                IsStructured = true,
                CreatedAt = DateTime.UtcNow
            };

            // 4. Calculate BMI if height and weight provided:
            if (record.Weight.HasValue && record.Height.HasValue && record.Height > 0)
            {
                var heightInMeters = record.Height.Value / 100;
                record.BMI = Math.Round(record.Weight.Value / (heightInMeters * heightInMeters), 1);
            }

            // 5. Add to database:
            await _context.PatientHealthRecords.AddAsync(record);
            await _context.SaveChangesAsync();

            // 6. If using template or custom sections, apply fields:
            if (request.Sections != null && request.Sections.Any())
            {
                if (request.TemplateId.HasValue)
                {
                    var template = await _context.Templates.FindAsync(request.TemplateId.Value);
                    if (template != null)
                    {
                        record.TemplateSnapshot = template.TemplateSchema;
                        template.UsageCount++;
                        template.LastUsedAt = DateTime.UtcNow;
                    }
                }

                foreach (var section in request.Sections)
                {
                    foreach (var attr in section.Attributes)
                    {
                        var attribute = new HealthAttribute
                        {
                            Id = Guid.NewGuid(),
                            RecordId = record.Id,
                            SectionName = section.SectionName,
                            FieldName = attr.Name,
                            FieldLabel = attr.Name, // We'll use name as label for now
                            FieldValue = attr.Value ?? "",
                            IsFromTemplate = request.TemplateId.HasValue,
                            CreatedAt = DateTime.UtcNow,
                            AddedBy = doctorId
                        };
                        
                        await _context.HealthAttributes.AddAsync(attribute);
                    }
                }
            }

            // 8. Save all changes:
            await _context.SaveChangesAsync();

            // 9. Log creation:
            await _auditLogService.LogAsync(
                doctorId,
                "Structured health record created",
                $"Record for patient {patient.User.FirstName} {patient.User.LastName}",
                "0.0.0.0",
                "Service");

            // 10. Generate PDF, Upload to Tigris, and link as MedicalRecord (Pre-verified)
            try
            {
                var pdfUrl = await GeneratePdfReportInternalAsync(record);
                record.GeneratedPdfPath = pdfUrl;
                await _context.SaveChangesAsync();
            }
            catch (Exception pdfEx)
            {
                _logger.LogWarning(pdfEx, "Health record saved, but PDF automated generation failed.");
            }

            // 11. Return DTO:
            var dto = await MapToDTO(record);
            return (true, "Record created successfully", dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create structured record");
            return (false, "An error occurred while creating the record", null);
        }
    }

    private async Task<HealthRecordDTO> MapToDTO(PatientHealthRecord record)
    {
        var dto = new HealthRecordDTO
        {
            Id = record.Id,
            PatientId = record.PatientId,
            DoctorId = record.DoctorId,
            AppointmentId = record.AppointmentId,
            RecordDate = record.RecordDate,
            RecordType = record.RecordType,
            
            BloodPressure = record.BloodPressureSystolic.HasValue && record.BloodPressureDiastolic.HasValue 
                ? $"{record.BloodPressureSystolic}/{record.BloodPressureDiastolic} mmHg" : null,
            IsBloodPressureAbnormal = IsBloodPressureAbnormal(record.BloodPressureSystolic, record.BloodPressureDiastolic),
            
            HeartRate = record.HeartRate.HasValue ? $"{record.HeartRate} bpm" : null,
            IsHeartRateAbnormal = IsHeartRateAbnormal(record.HeartRate),

            Temperature = record.Temperature.HasValue ? $"{record.Temperature} °F" : null,
            IsTemperatureAbnormal = IsTemperatureAbnormal(record.Temperature),

            Weight = record.Weight.HasValue ? $"{record.Weight} kg" : null,
            Height = record.Height.HasValue ? $"{record.Height} cm" : null,
            
            SpO2 = record.SpO2.HasValue ? $"{record.SpO2} %" : null,
            IsSpO2Abnormal = record.SpO2.HasValue && record.SpO2 < 95,

            BMI = record.BMI,
            BMICategory = GetBmiCategory(record.BMI),
            
            ChiefComplaint = record.ChiefComplaint,
            DoctorNotes = record.DoctorNotes,
            Diagnosis = record.Diagnosis,
            TreatmentPlan = record.TreatmentPlan,
            TemplateId = record.TemplateId,
            GeneratedPdfUrl = record.GeneratedPdfPath,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        };

        if (record.Doctor != null && record.Doctor.User != null)
        {
            dto.DoctorName = $"{record.Doctor.User.FirstName} {record.Doctor.User.LastName}";
        }
        
        if (record.Patient != null && record.Patient.User != null)
        {
            dto.PatientName = $"{record.Patient.User.FirstName} {record.Patient.User.LastName}";
        }

        // Map custom attributes to sections
        if (record.CustomAttributes != null && record.CustomAttributes.Any())
        {
            dto.Sections = record.CustomAttributes
                .GroupBy(a => a.SectionName ?? "General Assessment")
                .OrderBy(g => g.Min(a => a.DisplayOrder))
                .Select(g => new SectionDTO
                {
                    SectionName = g.Key,
                    DisplayOrder = g.Min(a => a.DisplayOrder),
                    Attributes = g.OrderBy(a => a.DisplayOrder).Select(a => new AttributeDTO
                    {
                        Id = a.Id,
                        FieldName = a.FieldName,
                        FieldLabel = a.FieldLabel,
                        FieldType = a.FieldType.ToString(),
                        FieldValue = a.FieldValue,
                        FieldUnit = a.FieldUnit,
                        IsAbnormal = a.IsAbnormal,
                        NormalRange = (a.NormalRangeMin.HasValue && a.NormalRangeMax.HasValue) 
                            ? $"{a.NormalRangeMin} - {a.NormalRangeMax}" 
                            : null,
                        DisplayOrder = a.DisplayOrder,
                        IsFromTemplate = a.IsFromTemplate
                    }).ToList()
                }).ToList();
        }

        return dto;
    }

    private string? GetBmiCategory(decimal? bmi)
    {
        if (!bmi.HasValue) return null;
        if (bmi < 18.5m) return "Underweight";
        if (bmi < 25m) return "Normal";
        if (bmi < 30m) return "Overweight";
        return "Obese";
    }

    // Implementing the remaining stubs
    public Task<(bool Success, string Message, HealthRecordDTO? Data)> UpdateStructuredRecordAsync(Guid recordId, UpdateHealthRecordDTO request, Guid doctorId)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool Success, string Message, HealthRecordDTO? Data)> GetStructuredRecordAsync(Guid recordId, Guid requestingUserId)
    {
        try
        {
            var record = await _context.PatientHealthRecords
                .Include(r => r.Patient).ThenInclude(p => p.User)
                .Include(r => r.Doctor).ThenInclude(d => d.User)
                .Include(r => r.CustomAttributes)
                .FirstOrDefaultAsync(r => r.Id == recordId);

            if (record == null)
                return (false, "Record not found", null);

            var dto = await MapToDTO(record);
            return (true, "Record retrieved successfully", dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get structured record {RecordId}", recordId);
            return (false, "An error occurred while retrieving the record", null);
        }
    }

    public async Task<(bool Success, string Message, List<HealthRecordDTO>? Data)> GetPatientStructuredRecordsAsync(Guid patientId, Guid requestingUserId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _context.PatientHealthRecords
                .Include(r => r.Patient).ThenInclude(p => p.User)
                .Include(r => r.Doctor).ThenInclude(d => d.User)
                .Include(r => r.CustomAttributes)
                .Where(r => r.PatientId == patientId && r.IsStructured);

            if (startDate.HasValue)
                query = query.Where(r => r.RecordDate >= startDate.Value);
            
            if (endDate.HasValue)
                query = query.Where(r => r.RecordDate <= endDate.Value);

            var records = await query
                .OrderByDescending(r => r.RecordDate)
                .ToListAsync();

            var dtos = new List<HealthRecordDTO>();
            foreach (var record in records)
            {
                dtos.Add(await MapToDTO(record));
            }

            return (true, $"{records.Count} records retrieved", dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get structured records for patient {PatientId}", patientId);
            return (false, "An error occurred while retrieving records", null);
        }
    }

    public async Task<(bool Success, string Message)> DeleteStructuredRecordAsync(Guid recordId, Guid doctorId)
    {
        try
        {
            var record = await _context.PatientHealthRecords
                .Include(r => r.CustomAttributes)
                .FirstOrDefaultAsync(r => r.Id == recordId);

            if (record == null)
                return (false, "Record not found");

            if (record.DoctorId != doctorId)
                return (false, "You are not authorized to delete this record");

            _context.HealthAttributes.RemoveRange(record.CustomAttributes);
            _context.PatientHealthRecords.Remove(record);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                doctorId,
                "Structured record deleted",
                $"Record {recordId} deleted",
                "0.0.0.0",
                "Service");

            return (true, "Record deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete record {RecordId}", recordId);
            return (false, "An error occurred while deleting the record");
        }
    }

    public Task<(bool Success, string Message)> AddCustomAttributeAsync(Guid recordId, AddAttributeDTO request, Guid doctorId)
    {
        throw new NotImplementedException();
    }

    public Task<(bool Success, string Message)> RemoveAttributeAsync(Guid attributeId, Guid doctorId)
    {
        throw new NotImplementedException();
    }

    public async Task<string> GeneratePdfReportAsync(Guid recordId)
    {
        var record = await _context.PatientHealthRecords
            .Include(r => r.Patient).ThenInclude(p => p.User)
            .Include(r => r.Doctor).ThenInclude(d => d.User)
            .Include(r => r.CustomAttributes)
            .FirstOrDefaultAsync(r => r.Id == recordId);
            
        if (record == null) throw new Exception("Record not found.");
        
        return await GeneratePdfReportInternalAsync(record);
    }
    
    private async Task<string> GeneratePdfReportInternalAsync(PatientHealthRecord record)
    {
        // Explicitly load all attributes for this record directly from the database context
        var customAttributes = await _context.HealthAttributes
            .Where(a => a.RecordId == record.Id)
            .ToListAsync();
            
        record.CustomAttributes = customAttributes;

        // Brand Palette
        DeviceRgb brandPrimary = new DeviceRgb(0, 59, 115); // #003B73
        DeviceRgb brandSecondary = new DeviceRgb(0, 163, 136); // #00A388

        // 1. Generate PDF Bytes
        using var memoryStream = new MemoryStream();
        var writer = new PdfWriter(memoryStream);
        var pdf = new PdfDocument(writer);
        var document = new Document(pdf);
        document.SetMargins(36, 36, 36, 36);

        // Header Section with Logo
        var headerTable = new Table(new float[] { 1, 3 }).UseAllAvailableWidth();
        headerTable.SetMarginBottom(20);

        try
        {
            // Logo path (Absolute path relative to workspace or hardcoded for now as confirmed)
            string logoPath = @"d:\finalyearproject\Source Code\Client\public\images\logo.png";
            if (File.Exists(logoPath))
            {
                ImageData imgData = ImageDataFactory.Create(logoPath);
                Image logo = new Image(imgData).SetWidth(60);
                headerTable.AddCell(new Cell().Add(logo).SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.MIDDLE));
            }
            else
            {
                headerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));
            }
        }
        catch
        {
            headerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));
        }

        var titleBlock = new Cell().Add(new Paragraph("CLINICAL HEALTH RECORD")
            .SetFontSize(24)
            .SetBold()
            .SetFontColor(brandPrimary))
            .Add(new Paragraph($"Generated on: {record.RecordDate:f}")
            .SetFontSize(10)
            .SetFontColor(ColorConstants.GRAY))
            .SetBorder(Border.NO_BORDER)
            .SetTextAlignment(TextAlignment.RIGHT)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE);
        
        headerTable.AddCell(titleBlock);
        document.Add(headerTable);

        // Horizontal Line
        document.Add(new iText.Layout.Element.LineSeparator(new iText.Kernel.Pdf.Canvas.Draw.SolidLine(1f))
            .SetFontColor(brandPrimary)
            .SetMarginBottom(20));

        // Patient / Doctor Info (Two Columns)
        var infoTable = new Table(new float[] { 1, 1 }).UseAllAvailableWidth();
        infoTable.SetMarginBottom(20);

        // Patient Card
        var patientCard = new Cell()
            .Add(new Paragraph("PATIENT DETAILS").SetBold().SetFontColor(brandPrimary).SetFontSize(11))
            .Add(new Paragraph($"Name: {record.Patient?.User?.FirstName} {record.Patient?.User?.LastName}").SetFontSize(10))
            .Add(new Paragraph($"DOB: {record.Patient?.DateOfBirth:d}").SetFontSize(10))
            .SetPadding(10)
            .SetBackgroundColor(new DeviceRgb(241, 245, 249))
            .SetBorder(Border.NO_BORDER);
        infoTable.AddCell(patientCard);

        // Doctor Card
        var doctorCard = new Cell()
            .Add(new Paragraph("ATTENDING PHYSICIAN").SetBold().SetFontColor(brandPrimary).SetFontSize(11))
            .Add(new Paragraph($"Dr. {record.Doctor?.User?.FirstName} {record.Doctor?.User?.LastName}").SetFontSize(10))
            .Add(new Paragraph($"Dept: {record.Doctor?.Department}").SetFontSize(10))
            .SetPadding(10)
            .SetBackgroundColor(new DeviceRgb(241, 245, 249))
            .SetBorder(Border.NO_BORDER);
        infoTable.AddCell(doctorCard);

        document.Add(infoTable);

        // Chief Complaint
        if (!string.IsNullOrEmpty(record.ChiefComplaint))
        {
            document.Add(new Paragraph("CHIEF COMPLAINT").SetBold().SetFontColor(brandPrimary).SetFontSize(12));
            document.Add(new Paragraph(record.ChiefComplaint).SetFontSize(11).SetMarginBottom(15).SetPaddingLeft(5));
        }

        // Vitals
        bool hasVitals = record.BloodPressureSystolic.HasValue || record.HeartRate.HasValue || 
                       record.Temperature.HasValue || record.Weight.HasValue || 
                       record.Height.HasValue || record.SpO2.HasValue;
                       
        if (hasVitals)
        {
            document.Add(new Paragraph("VITALS & MEASUREMENTS").SetBold().SetFontColor(brandPrimary).SetFontSize(12));
            var vitalsTable = new Table(new float[] { 2, 3 }).UseAllAvailableWidth();
            vitalsTable.SetMarginBottom(15);
            
            AddStyledVitalRow(vitalsTable, "Blood Pressure", record.BloodPressureSystolic.HasValue ? $"{record.BloodPressureSystolic}/{record.BloodPressureDiastolic} mmHg" : null);
            AddStyledVitalRow(vitalsTable, "Heart Rate", record.HeartRate.HasValue ? $"{record.HeartRate} bpm" : null);
            AddStyledVitalRow(vitalsTable, "Temperature", record.Temperature.HasValue ? $"{record.Temperature} °F" : null);
            AddStyledVitalRow(vitalsTable, "Weight", record.Weight.HasValue ? $"{record.Weight} kg" : null);
            AddStyledVitalRow(vitalsTable, "Height", record.Height.HasValue ? $"{record.Height} cm" : null);
            AddStyledVitalRow(vitalsTable, "BMI", record.BMI.HasValue ? record.BMI.ToString() : null);
            AddStyledVitalRow(vitalsTable, "SpO2", record.SpO2.HasValue ? $"{record.SpO2} %" : null);
            
            document.Add(vitalsTable);
        }

        // Custom Attributes (Protocol Fields)
        if (record.CustomAttributes != null && record.CustomAttributes.Any())
        {
            document.Add(new Paragraph("CLINICAL PROTOCOL ASSESSMENT").SetBold().SetFontColor(brandPrimary).SetFontSize(12).SetMarginTop(10));
            
            var sections = record.CustomAttributes.GroupBy(a => a.SectionName);
            foreach (var section in sections)
            {
                document.Add(new Paragraph((section.Key ?? "General").ToUpper()).SetBold().SetFontSize(10).SetFontColor(brandSecondary).SetMarginTop(5));
                var attrTable = new Table(new float[] { 2, 2, 1 }).UseAllAvailableWidth();
                attrTable.SetMarginBottom(10);

                var headerCell1 = new Cell().Add(new Paragraph("Field").SetBold().SetFontColor(ColorConstants.WHITE)).SetBackgroundColor(brandPrimary);
                var headerCell2 = new Cell().Add(new Paragraph("Value").SetBold().SetFontColor(ColorConstants.WHITE)).SetBackgroundColor(brandPrimary);
                var headerCell3 = new Cell().Add(new Paragraph("Range / Flag").SetBold().SetFontColor(ColorConstants.WHITE)).SetBackgroundColor(brandPrimary);
                
                attrTable.AddHeaderCell(headerCell1);
                attrTable.AddHeaderCell(headerCell2);
                attrTable.AddHeaderCell(headerCell3);

                foreach (var attr in section.OrderBy(a => a.DisplayOrder))
                {
                    attrTable.AddCell(new Cell().Add(new Paragraph(attr.FieldLabel ?? attr.FieldName).SetFontSize(9)));
                    attrTable.AddCell(new Cell().Add(new Paragraph($"{attr.FieldValue} {attr.FieldUnit}").SetFontSize(9)));
                    
                    var flagStr = "";
                    if (attr.NormalRangeMin.HasValue && attr.NormalRangeMax.HasValue)
                    {
                        flagStr = $"[{attr.NormalRangeMin} - {attr.NormalRangeMax}]";
                        if (attr.IsAbnormal == true) flagStr += " (!)";
                    }
                    else if (attr.IsAbnormal == true)
                    {
                        flagStr = "(!)";
                    }
                    
                    var flagCell = new Cell().Add(new Paragraph(flagStr).SetFontSize(9));
                    if (attr.IsAbnormal == true) flagCell.SetFontColor(ColorConstants.RED).SetBold();
                    attrTable.AddCell(flagCell);
                }
                document.Add(attrTable);
            }
        }

        // Clinical Assessment
        if (!string.IsNullOrEmpty(record.Diagnosis) || !string.IsNullOrEmpty(record.TreatmentPlan) || !string.IsNullOrEmpty(record.DoctorNotes))
        {
            document.Add(new Paragraph("CLINICAL ASSESSMENT & PLAN").SetBold().SetFontColor(brandPrimary).SetFontSize(12).SetMarginTop(10));
            
            if (!string.IsNullOrEmpty(record.Diagnosis))
            {
                document.Add(new Paragraph("Diagnosis:").SetBold().SetFontSize(10));
                document.Add(new Paragraph(record.Diagnosis).SetFontSize(10).SetMarginBottom(10));
            }
            
            if (!string.IsNullOrEmpty(record.TreatmentPlan))
            {
                document.Add(new Paragraph("Treatment Plan:").SetBold().SetFontSize(10));
                document.Add(new Paragraph(record.TreatmentPlan).SetFontSize(10).SetMarginBottom(10));
            }
            
            if (!string.IsNullOrEmpty(record.DoctorNotes))
            {
                document.Add(new Paragraph("Notes:").SetBold().SetFontSize(10));
                document.Add(new Paragraph(record.DoctorNotes).SetFontSize(10).SetMarginBottom(10));
            }
        }

        // Footer
        document.Add(new Paragraph("\n\n"));
        document.Add(new iText.Layout.Element.LineSeparator(new iText.Kernel.Pdf.Canvas.Draw.SolidLine(0.5f)).SetFontColor(ColorConstants.LIGHT_GRAY));
        document.Add(new Paragraph("This is an electronically generated document from the Secure Medical Record System. All data is encrypted and securely stored.")
            .SetFontSize(8)
            .SetFontColor(ColorConstants.GRAY)
            .SetTextAlignment(TextAlignment.CENTER));

        document.Close();
        var pdfBytes = memoryStream.ToArray();

        // 2. Encrypt PDF
        var encryptedBytes = _encryptionService.EncryptBytes(pdfBytes);

        // 3. Upload to Tigris Storage
        var objectKey = $"records/{record.PatientId}/{Guid.NewGuid()}.enc";
        using var encryptedStream = new MemoryStream(encryptedBytes);
        var uploadId = await _storageService.UploadFileAsync(
            encryptedStream, 
            objectKey, 
            "application/pdf", 
            $"Clinical_Report_{record.RecordDate:yyyyMMdd}.pdf");

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(encryptedBytes);
        var fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

        // 4. Create MedicalRecord entity linked to history timeline
        // Since a doctor created it directly via structured form, bypass review (Certified).
        var medRecord = new MedicalRecord
        {
            PatientId = record.PatientId,
            AssignedDoctorId = record.DoctorId,
            S3ObjectKey = uploadId,
            IsEncrypted = true,
            OriginalFileName = $"Clinical_Report_{record.RecordDate:yyyyMMdd}.pdf",
            FileHash = fileHash,
            FileSizeBytes = encryptedBytes.Length,
            MimeType = "application/pdf",
            State = RecordState.Certified,
            RecordType = "Clinical Report (Auto-Generated)",
            Description = $"Structured session generated report for {record.ChiefComplaint ?? record.RecordType}",
            RecordDate = record.RecordDate,
            UploadedAt = DateTime.UtcNow,
            CertifiedAt = DateTime.UtcNow
        };

        await _context.MedicalRecords.AddAsync(medRecord);
        await _context.SaveChangesAsync();

        return uploadId;
    }


    public async Task<Dictionary<string, object>> ExportForAIAnalysisAsync(Guid patientId, DateTime? startDate = null, DateTime? endDate = null)
    {
        // 1. Get patient info
        var patient = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == patientId);
        
        if (patient == null)
            throw new KeyNotFoundException("Patient not found");
        
        // 2. Get structured records in date range
        var query = _context.PatientHealthRecords
            .Include(r => r.CustomAttributes)
            .Include(r => r.Doctor).ThenInclude(d => d.User)
            .Where(r => r.PatientId == patientId && r.IsStructured);
        
        if (startDate.HasValue)
            query = query.Where(r => r.RecordDate >= startDate.Value);
        
        if (endDate.HasValue)
            query = query.Where(r => r.RecordDate <= endDate.Value);
        
        var records = await query
            .OrderBy(r => r.RecordDate)
            .ToListAsync();
        
        if (!records.Any())
        {
             return new Dictionary<string, object> { ["message"] = "No structured records found for this patient in the specified range." };
        }

        // 3. Build AI-friendly export
        var export = new Dictionary<string, object>
        {
            ["export_metadata"] = new
            {
                export_date = DateTime.UtcNow,
                patient_id = patientId,
                patient_age = CalculateAge(patient.DateOfBirth),
                patient_gender = patient.Gender,
                record_count = records.Count,
                date_range_start = startDate ?? records.FirstOrDefault()?.RecordDate,
                date_range_end = endDate ?? records.LastOrDefault()?.RecordDate
            },
            
            ["patient_demographics"] = new
            {
                age = CalculateAge(patient.DateOfBirth),
                gender = patient.Gender,
                blood_type = patient.BloodType,
                allergies = patient.Allergies,
                chronic_conditions = patient.ChronicConditions
            },
            
            ["time_series_data"] = records.Select(r => new
            {
                record_id = r.Id,
                timestamp = r.RecordDate,
                record_type = r.RecordType,
                doctor_specialty = r.Doctor?.Department,
                
                // Base vitals (normalized)
                vitals = new
                {
                    blood_pressure = new
                    {
                        systolic = r.BloodPressureSystolic,
                        diastolic = r.BloodPressureDiastolic,
                        unit = "mmHg",
                        is_abnormal = IsBloodPressureAbnormal(r.BloodPressureSystolic, r.BloodPressureDiastolic)
                    },
                    heart_rate = new
                    {
                        value = r.HeartRate,
                        unit = "bpm",
                        is_abnormal = IsHeartRateAbnormal(r.HeartRate)
                    },
                    temperature = new
                    {
                        value = r.Temperature,
                        unit = "F",
                        is_abnormal = IsTemperatureAbnormal(r.Temperature)
                    },
                    weight = new
                    {
                        value = r.Weight,
                        unit = "kg"
                    },
                    bmi = new
                    {
                        value = r.BMI,
                        category = GetBmiCategory(r.BMI)
                    },
                    spo2 = new
                    {
                        value = r.SpO2,
                        unit = "%",
                        is_abnormal = r.SpO2 < 95
                    }
                },
                
                // Custom attributes (grouped by section)
                custom_measurements = r.CustomAttributes
                    .GroupBy(a => a.SectionName)
                    .ToDictionary(
                        g => (g.Key ?? "default").ToLower().Replace(" ", "_"),
                        g => g.ToDictionary(
                            a => a.FieldName,
                            a => new
                            {
                                value = ParseValue(a.FieldValue, a.FieldType),
                                unit = a.FieldUnit,
                                is_abnormal = a.IsAbnormal,
                                normal_range = new
                                {
                                    min = a.NormalRangeMin,
                                    max = a.NormalRangeMax
                                },
                                data_type = a.FieldType.ToString()
                            }
                        )
                    ),
                
                // Clinical notes (text)
                clinical_notes = new
                {
                    chief_complaint = r.ChiefComplaint,
                    diagnosis = r.Diagnosis,
                    treatment_plan = r.TreatmentPlan,
                    doctor_notes = r.DoctorNotes
                }
            }).ToList(),
            
            // Aggregate statistics
            ["aggregate_stats"] = new
            {
                total_visits = records.Count,
                first_visit = records.FirstOrDefault()?.RecordDate,
                last_visit = records.LastOrDefault()?.RecordDate,
                
                // Average vitals
                avg_blood_pressure_systolic = records
                    .Where(r => r.BloodPressureSystolic.HasValue)
                    .Average(r => r.BloodPressureSystolic),
                
                avg_blood_pressure_diastolic = records
                    .Where(r => r.BloodPressureDiastolic.HasValue)
                    .Average(r => r.BloodPressureDiastolic),
                
                avg_heart_rate = records
                    .Where(r => r.HeartRate.HasValue)
                    .Average(r => r.HeartRate),
                
                avg_bmi = records
                    .Where(r => r.BMI.HasValue)
                    .Average(r => r.BMI),
                
                // Most common diagnoses
                common_diagnoses = ExtractCommonTerms(records.Select(r => r.Diagnosis)),
                
                // Medications mentioned
                medications_prescribed = ExtractMedications(records.Select(r => r.TreatmentPlan))
            }
        };
        
        return export;
    }

    // Helper Methods for AI Export
    private object? ParseValue(string? value, FieldType type)
    {
        if (string.IsNullOrEmpty(value)) return null;
        
        try {
            switch (type) {
                case FieldType.Number: return decimal.TryParse(value, out var d) ? d : null;
                case FieldType.Boolean: return bool.TryParse(value, out var b) ? b : null;
                case FieldType.Date: return DateTime.TryParse(value, out var dt) ? dt : null;
                default: return value;
            }
        } catch { return value; }
    }

    private bool IsBloodPressureAbnormal(int? systolic, int? diastolic)
    {
        if (!systolic.HasValue || !diastolic.HasValue) return false;
        return systolic > 140 || systolic < 90 || diastolic > 90 || diastolic < 60;
    }

    private bool IsHeartRateAbnormal(int? hr)
    {
        if (!hr.HasValue) return false;
        return hr > 100 || hr < 60;
    }

    private bool IsTemperatureAbnormal(decimal? temp)
    {
        if (!temp.HasValue) return false;
        return temp > 99.5m || temp < 96.0m;
    }


    private int CalculateAge(DateTime? dob)
    {
        if (!dob.HasValue) return 0;
        var today = DateTime.Today;
        var age = today.Year - dob.Value.Year;
        if (dob.Value.Date > today.AddYears(-age)) age--;
        return age;
    }

    private List<string> ExtractCommonTerms(IEnumerable<string?> texts)
    {
        // Simple mock NLP: word frequency count (excluding stop words)
        var stopWords = new HashSet<string> { "a", "the", "and", "with", "patient", "showed", "observed" };
        return texts
            .Where(t => !string.IsNullOrEmpty(t))
            .SelectMany(t => t!.Split(new[] { ' ', ',', '.', ';' }, StringSplitOptions.RemoveEmptyEntries))
            .Select(w => w.ToLower())
            .Where(w => w.Length > 3 && !stopWords.Contains(w))
            .GroupBy(w => w)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key)
            .ToList();
    }

    private List<string> ExtractMedications(IEnumerable<string?> texts)
    {
        // Patterns for common medication mentions (Simplified)
        var medDictionary = new[] { "metformin", "amlodipine", "atorvastatin", "lisinopril", "insulin", "paracetamol", "ibuprofen" };
        var found = new HashSet<string>();
        
        foreach (var text in texts.Where(t => !string.IsNullOrEmpty(t)))
        {
            var lowerText = text!.ToLower();
            foreach (var med in medDictionary)
            {
                if (lowerText.Contains(med)) found.Add(med);
            }
        }
        
        return found.ToList();
    }

    private void AddStyledVitalRow(Table table, string label, string? value)
    {
        if (string.IsNullOrEmpty(value)) return;

        table.AddCell(new Cell().Add(new Paragraph(label).SetBold().SetFontSize(10)).SetBorder(Border.NO_BORDER).SetPaddingTop(2).SetPaddingBottom(2));
        table.AddCell(new Cell().Add(new Paragraph(value).SetFontSize(10)).SetBorder(Border.NO_BORDER).SetPaddingTop(2).SetPaddingBottom(2));
    }
}
