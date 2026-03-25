using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecureMedicalRecordSystem.Core.DTOs.MedicalRecords;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.Enums;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Configuration;
using SecureMedicalRecordSystem.Infrastructure.Data;
using SecureMedicalRecordSystem.Infrastructure.Utils;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class MedicalRecordsService : IMedicalRecordsService
{
    private readonly ApplicationDbContext _context;
    private readonly ITigrisStorageService _tigrisService;
    private readonly IEncryptionService _encryptionService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly FileUploadSettings _fileSettings;
    private readonly IAuditLogService _auditLogService;
    private readonly IDigitalSignatureService _signatureService;
    private readonly ILogger<MedicalRecordsService> _logger;

    public MedicalRecordsService(
        ApplicationDbContext context,
        ITigrisStorageService tigrisService,
        IEncryptionService encryptionService,
        UserManager<ApplicationUser> userManager,
        IOptions<FileUploadSettings> fileSettings,
        IAuditLogService auditLogService,
        IDigitalSignatureService signatureService,
        ILogger<MedicalRecordsService> logger)
    {
        _context = context;
        _tigrisService = tigrisService;
        _encryptionService = encryptionService;
        _userManager = userManager;
        _fileSettings = fileSettings.Value;
        _auditLogService = auditLogService;
        _signatureService = signatureService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, MedicalRecordResponseDTO? Data)> UploadRecordAsync(
        Guid patientId,
        UploadMedicalRecordDTO uploadDto)
    {
        _logger.LogInformation("Starting upload for patient: {PatientId}", patientId);

        // 1. Validate patient exists
        var patient = await _context.Patients
            .Include(p => p.User)
            .OrderBy(p => p.Id)
            .FirstOrDefaultAsync(p => p.Id == patientId);

        if (patient == null)
            return (false, "Patient not found.", null);

        // 2. Validate file
        if (uploadDto.File.Length > _fileSettings.MaxFileSizeBytes)
            return (false, $"File size exceeds the limit of {_fileSettings.MaxFileSizeMB} MB.", null);

        var extension = Path.GetExtension(uploadDto.File.FileName).ToLowerInvariant();
        if (!_fileSettings.AllowedExtensions.Contains(extension))
            return (false, $"File type {extension} is not allowed.", null);

        if (!_fileSettings.AllowedMimeTypes.Contains(uploadDto.File.ContentType))
            return (false, $"MIME type {uploadDto.File.ContentType} is not allowed.", null);

        try
        {
            // 3. Compute original file hash (before encryption)
            string originalHash;
            using (var hashStream = uploadDto.File.OpenReadStream())
            {
                originalHash = await _encryptionService.ComputeFileHashAsync(hashStream);
            }

            // 4. Encrypt file
            using var rawStream = uploadDto.File.OpenReadStream();
            using var encryptedStream = await _encryptionService.EncryptFileAsync(rawStream);

            // 5. Upload to Tigris
            var timestamp = DateTime.Now;
            var objectKey = $"{patientId}/{timestamp:yyyy}/{timestamp:MM}/{Guid.NewGuid()}.enc";

            var uploadedKey = await _tigrisService.UploadFileAsync(
                encryptedStream,
                objectKey,
                uploadDto.File.ContentType,
                uploadDto.File.FileName);

            // 6. Create MedicalRecord entity
            var record = new MedicalRecord
            {
                PatientId = patientId,
                AssignedDoctorId = uploadDto.AssignedDoctorId,
                S3ObjectKey = uploadedKey,
                IsEncrypted = true,
                EncryptionAlgorithm = "AES-256-CBC",
                OriginalFileName = uploadDto.File.FileName,
                FileHash = originalHash,
                FileSizeBytes = uploadDto.File.Length,
                MimeType = uploadDto.File.ContentType,
                RecordType = uploadDto.RecordType,
                Description = uploadDto.Description,
                RecordDate = uploadDto.RecordDate ?? DateTime.Now,
                Tags = uploadDto.Tags,
                State = RecordState.Pending,
                Version = 1,
                IsLatestVersion = true,
                UploadedAt = DateTime.Now,
                CreatedBy = patient.User.Email ?? "System"
            };

            // 7. Save to database
            await _context.MedicalRecords.AddAsync(record);
            await _context.SaveChangesAsync();

            // 8. Log action
            await _auditLogService.LogAsync(
                patient.UserId,
                "Medical Record Uploaded",
                $"Uploaded {record.OriginalFileName} ({record.MimeType})",
                "0.0.0.0", "Service", "MedicalRecord", record.Id.ToString());

            return (true, "Record uploaded successfully.", MapToDto(record, patient.User.FirstName + " " + patient.User.LastName, true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading medical record for patient {PatientId}", patientId);
            return (false, "An error occurred during upload. Please try again.", null);
        }
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string Message, Stream? FileStream, string? FileName, string? ContentType)> StreamDownloadRecordAsync(
        Guid recordId,
        Guid requestingUserId)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("[PERF] [Stream] StreamDownloadRecordAsync started for Record: {RecordId}", recordId);

        // 1. DB Lookup (metadata only — no blob)
        var record = await _context.MedicalRecords
            .Include(r => r.Patient)
            .OrderBy(r => r.Id)
            .FirstOrDefaultAsync(r => r.Id == recordId && !r.IsDeleted);

        _logger.LogInformation("[PERF] [Stream] Phase 1: DB lookup in {Ms}ms", sw.ElapsedMilliseconds);

        if (record == null)
            return (false, "Record not found.", null, null, null);

        // 2. Permission check
        var canAccess = await CheckAccessAsync(record, requestingUserId);
        if (!canAccess)
            return (false, "Unauthorized access.", null, null, null);

        try
        {
            // 3. Fire audit log BEFORE streaming so latency is absorbed in parallel
            _ = _auditLogService.LogAsync(requestingUserId, "Medical Record Streamed",
                $"Streamed {record.OriginalFileName}", "0.0.0.0", "Service", "MedicalRecord", record.Id.ToString());

            // 4. Open raw S3 stream (no buffer copy — live network socket)
            _logger.LogInformation("[PERF] [Stream] Phase 3: Opening raw S3 socket stream");
            var s3Stream = await _tigrisService.OpenDownloadStreamAsync(record.S3ObjectKey);
            _logger.LogInformation("[PERF] [Stream] Phase 3: S3 socket open in {Ms}ms (TTFB achieved)", sw.ElapsedMilliseconds);

            // 5. Wrap in live CryptoStream — no copy, no MemoryStream
            var cryptoStream = _encryptionService.CreateDecryptingStream(s3Stream);
            _logger.LogInformation("[PERF] [Stream] Phase 4: CryptoStream pipeline assembled in {Ms}ms total", sw.ElapsedMilliseconds);

            // Return immediately — ASP.NET will pump chunks through the pipeline
            return (true, "Success", cryptoStream, record.OriginalFileName, record.MimeType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PERF] [Stream] CRITICAL FAILURE during stream setup for record {Id}", recordId);
            return (false, "An error occurred during streaming.", null, null, null);
        }
    }

    public async Task<(bool Success, string Message, List<MedicalRecordResponseDTO>? Data)> GetPatientRecordsFlatAsync(
        Guid patientId,
        Guid requestingUserId)
    {
        var (allowed, message, patientName) = await GetPatientMetadataAndCheckAccessAsync(patientId, requestingUserId);
        if (!allowed) return (false, message, null);

        // Direct projection: fetches ONLY the fields the DTO needs — no full entity hydration.
        // This is the core fix. Include().ThenInclude() was loading all ApplicationUser columns
        // (password hashes, security stamps, phone numbers...) just to get FirstName + LastName.
        var dtos = await ProjectToDto(_context.MedicalRecords
            .Where(r => r.PatientId == patientId && !r.IsDeleted), patientName!, canDownload: true)
            .ToListAsync();

        return (true, "Success", dtos);
    }

    public async Task<(bool Success, string Message, GroupedMedicalRecordsDTO? Data)> GetPatientRecordsAsync(
        Guid patientId,
        Guid requestingUserId)
    {
        var (allowed, message, patientName) = await GetPatientMetadataAndCheckAccessAsync(patientId, requestingUserId);
        if (!allowed) return (false, message, null);

        // Direct projection: same approach — one efficient SQL query, no entity hydration.
        var dtos = await ProjectToDto(_context.MedicalRecords
            .Where(r => r.PatientId == patientId && !r.IsDeleted), patientName!, canDownload: true)
            .ToListAsync();

        // Compute date-derived display fields in-memory (cannot run inside SQL).
        // This is O(N) over only the projected, lightweight DTOs — not full entity objects.
        foreach (var dto in dtos)
        {
            dto.TimePeriod = DateGroupingHelper.GetTimePeriodLabel(dto.UploadedAt);
            dto.RelativeTimeString = DateGroupingHelper.GetRelativeTimeString(dto.UploadedAt);
        }

        // Grouping logic (in-memory, fast since data is already projected)
        var groupedData = new GroupedMedicalRecordsDTO
        {
            TotalCount = dtos.Count,
            Sections = dtos
                .GroupBy(r => r.TimePeriod)
                .Select(g => new RecordSectionDTO
                {
                    TimePeriod = g.Key,
                    DisplayName = DateGroupingHelper.GetSectionDisplayName(g.Key),
                    RecordCount = g.Count(),
                    IsExpanded = g.Key switch
                    {
                        "THIS_WEEK" => true,
                        "THIS_MONTH" => g.Count() <= 10,
                        _ => false
                    },
                    Records = g.ToList()
                })
                .OrderBy(s => s.TimePeriod switch
                {
                    "THIS_WEEK" => 1,
                    "THIS_MONTH" => 2,
                    "EARLIER_THIS_YEAR" => 3,
                    "LAST_YEAR" => 4,
                    "OLDER" => 5,
                    _ => 6
                })
                .ToList()
        };

        return (true, "Success", groupedData);
    }

    /// <summary>
    /// Core projection helper: builds a MedicalRecordResponseDTO directly in EF LINQ.
    /// EF translates this to a single efficient SQL SELECT with only the needed columns —
    /// no Include() hydration of full entity graphs.
    /// </summary>
    private IQueryable<MedicalRecordResponseDTO> ProjectToDto(
        IQueryable<MedicalRecord> query,
        string? patientName,
        bool canDownload)
    {
        return query
            .AsNoTracking()
            .OrderByDescending(r => r.UploadedAt)
            .Select(r => new MedicalRecordResponseDTO
            {
                Id = r.Id,
                PatientId = r.PatientId,
                OriginalFileName = r.OriginalFileName,
                RecordType = r.RecordType,
                Description = r.Description,
                RecordDate = r.RecordDate,
                FileSize = r.FileSizeBytes,
                FileSizeFormatted = r.FileSizeBytes >= 1048576
                    ? $"{(r.FileSizeBytes / 1048576.0):n1} MB"
                    : r.FileSizeBytes >= 1024
                        ? $"{(r.FileSizeBytes / 1024.0):n1} KB"
                        : $"{r.FileSizeBytes} B",
                MimeType = r.MimeType,
                State = r.State,
                StateLabel = r.State == RecordState.Draft ? "Draft"
                    : r.State == RecordState.Pending ? "Awaiting Review"
                    : r.State == RecordState.Certified ? "Certified"
                    : r.State == RecordState.Archived ? "Archived"
                    : r.State == RecordState.Emergency ? "Emergency Access"
                    : r.State.ToString(),
                RejectionReason = r.State == RecordState.Draft && r.Notes != null && r.Notes.StartsWith("REJECTED:")
                    ? r.Notes.Substring("REJECTED:".Length).Trim()
                    : null,
                UploadedAt = r.UploadedAt,
                // Prefer auto-generated report author (assigned doctor) over generic uploader
                UploadedBy = (r.RecordType != null && (r.RecordType.Contains("Auto-Generated") || r.RecordType.Contains("Clinical Report")) && r.AssignedDoctor != null)
                    ? ("Dr. " + r.AssignedDoctor.User!.FirstName + " " + r.AssignedDoctor.User.LastName).Trim()
                    : (patientName ?? (r.Patient.User!.FirstName + " " + r.Patient.User.LastName)),
                PatientName = patientName ?? (r.Patient.User!.FirstName + " " + r.Patient.User.LastName),
                AssignedDoctorName = r.AssignedDoctor != null
                    ? ("Dr. " + r.AssignedDoctor.User!.FirstName + " " + r.AssignedDoctor.User.LastName).Trim()
                    : null,
                AssignedDoctorId = r.AssignedDoctorId,
                AssignedDepartment = r.AssignedDoctor != null ? r.AssignedDoctor.Department!.Name : null,
                IsCertified = r.Certifications.Any(c => c.IsValid) || (r.RecordType != null && (r.RecordType.Contains("Auto-Generated") || r.RecordType.Contains("Clinical Report"))),
                CertifiedBy = r.Certifications
                    .Where(c => c.IsValid)
                    .OrderByDescending(c => c.SignedAt)
                    .Select(c => c.Doctor.User!.FirstName + " " + c.Doctor.User.LastName)
                    .FirstOrDefault() ?? (r.RecordType != null && (r.RecordType.Contains("Auto-Generated") || r.RecordType.Contains("Clinical Report")) && r.AssignedDoctor != null ? r.AssignedDoctor.User!.FirstName + " " + r.AssignedDoctor.User.LastName : null),
                CertifiedById = r.Certifications
                    .Where(c => c.IsValid)
                    .OrderByDescending(c => c.SignedAt)
                    .Select(c => (Guid?)c.DoctorId)
                    .FirstOrDefault(),
                CertifiedAt = r.Certifications
                    .Where(c => c.IsValid)
                    .OrderByDescending(c => c.SignedAt)
                    .Select(c => (DateTime?)c.SignedAt)
                    .FirstOrDefault() ?? (r.RecordType != null && (r.RecordType.Contains("Auto-Generated") || r.RecordType.Contains("Clinical Report")) ? (r.RecordDate ?? r.UploadedAt) : (DateTime?)null),
                Version = r.Version,
                CanDownload = canDownload,
                Tags = r.Tags,
                // These are computed server-side in DateGroupingHelper (cannot be in SQL)
                // We set them to empty here; the ordering is correct due to UploadedAt sort.
                RelativeTimeString = "",
                TimePeriod = "",
            });
    }

    public async Task<(bool Success, string Message, MedicalRecordResponseDTO? Data)> GetRecordDetailsAsync(
        Guid recordId,
        Guid requestingUserId)
    {
        var record = await _context.MedicalRecords
            .Include(r => r.Patient)
            .ThenInclude(p => p.User)
            .Include(r => r.AssignedDoctor)
            .ThenInclude(d => d!.User)
            .Include(r => r.Certifications)
            .ThenInclude(c => c.Doctor)
            .ThenInclude(d => d.User)
            .OrderBy(r => r.Id)
            .FirstOrDefaultAsync(r => r.Id == recordId && !r.IsDeleted);

        if (record == null) return (false, "Record not found.", null);

        var canAccess = await CheckAccessAsync(record, requestingUserId);
        if (!canAccess) return (false, "Unauthorized access.", null);

        return (true, "Success", MapToDto(record, record.Patient.User.FirstName + " " + record.Patient.User.LastName, true));
    }

    public async Task<(bool Success, string Message, List<MedicalRecordResponseDTO>? Data)> GetPendingRecordsForDoctorAsync(
        Guid doctorUserId)
    {
        // Merged: doctor lookup + records query in a single joined query instead of two round-trips.
        // AsNoTracking() since this is read-only.
        var doctorId = await _context.Doctors
            .AsNoTracking()
            .Where(d => d.UserId == doctorUserId)
            .Select(d => d.Id)
            .FirstOrDefaultAsync();

        if (doctorId == Guid.Empty) return (false, "Doctor profile not found.", null);

        var records = await _context.MedicalRecords
            .AsNoTracking()
            .Where(r => r.State == RecordState.Pending && !r.IsDeleted && r.AssignedDoctorId == doctorId)
            .Include(r => r.Patient)
                .ThenInclude(p => p.User)
            .Include(r => r.AssignedDoctor)
                .ThenInclude(d => d!.User)
            .Include(r => r.Certifications)
                .ThenInclude(c => c.Doctor)
                    .ThenInclude(d => d.User)
            .OrderByDescending(r => r.UploadedAt)
            .ToListAsync();

        var dtos = records.Select(r => {
            var patientName = r.Patient?.User != null 
                ? $"{r.Patient.User.FirstName} {r.Patient.User.LastName}" 
                : "Unknown Patient";
            return MapToDto(r, patientName, true);
        }).ToList();
        return (true, "Success", dtos);
    }

    public async Task<(bool Success, string Message, List<MedicalRecordResponseDTO>? Data)> GetCertifiedRecordsForDoctorAsync(
        Guid doctorUserId)
    {
        // Merged: resolve doctorId inline in the WHERE clause — no separate round-trip.
        // AsNoTracking() since read-only. Certifications.Any() with DoctorId is supported by EF Core.
        var doctorId = await _context.Doctors
            .AsNoTracking()
            .Where(d => d.UserId == doctorUserId)
            .Select(d => d.Id)
            .FirstOrDefaultAsync();

        if (doctorId == Guid.Empty) return (false, "Doctor profile not found.", null);

        var dtos = await ProjectToDto(_context.MedicalRecords
            .Where(r => r.State == RecordState.Certified && !r.IsDeleted)
            .Where(r => r.Certifications.Any(c => c.DoctorId == doctorId && c.IsValid && !c.IsDeleted) || 
                       (r.AssignedDoctorId == doctorId && r.RecordType != null && (r.RecordType.Contains("Auto-Generated") || r.RecordType.Contains("Clinical Report")))),
            null, // Multi-patient query: determine name in projection
            canDownload: true)
            .OrderByDescending(r => r.CertifiedAt)
            .ToListAsync();

        return (true, "Success", dtos);
    }

    public async Task<(bool Success, string Message, PatientListResponseDTO? Data)> GetPatientByIdForDoctorAsync(Guid patientId, Guid doctorUserId)
    {
        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == doctorUserId);
        if (doctor == null)
            return (false, "Doctor profile not found.", null);

        // Fetch patient + user info
        var p = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == patientId);

        if (p == null)
            return (false, "Patient not found.", null);

        // Get record stats for this specific patient
        var sharedStats = await _context.MedicalRecords
            .AsNoTracking()
            .Where(r => r.PatientId == p.Id && r.AssignedDoctorId == doctor.Id && !r.IsDeleted)
            .Select(r => r.UploadedAt)
            .ToListAsync();

        var result = new PatientListResponseDTO
        {
            Id = p.Id,
            UserId = p.UserId,
            FirstName = p.User?.FirstName ?? "",
            LastName = p.User?.LastName ?? "",
            Email = p.User?.Email ?? "",
            PhoneNumber = p.User?.PhoneNumber,
            DateOfBirth = p.DateOfBirth,
            Gender = p.Gender,
            BloodType = p.BloodType,
            Allergies = p.Allergies,
            EmergencyContactName = p.EmergencyContactName,
            EmergencyContactPhone = p.EmergencyContactPhone,
            EmergencyContactRelationship = p.EmergencyContactRelationship,
            SharedRecordsCount = sharedStats.Count,
            AppointmentCount = await _context.Appointments.CountAsync(a => a.PatientId == p.Id && a.DoctorId == doctor.Id && !a.IsDeleted),
            LatestSharedRecordDate = sharedStats.Any() ? sharedStats.Max() : null
        };

        return (true, "Patient retrieved successfully.", result);
    }

    public async Task<(bool Success, string Message, List<PatientListResponseDTO>? Data)> GetPatientsForDoctorAsync(Guid doctorUserId)
    {
        var doctor = await _context.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.UserId == doctorUserId);
        if (doctor == null)
            return (false, "Doctor profile not found.", null);

        // Identify all patients linked to this doctor via records, appointments, or primary status
        var patientIds = await _context.MedicalRecords
            .Where(r => r.AssignedDoctorId == doctor.Id && !r.IsDeleted)
            .Select(r => r.PatientId)
            .Union(_context.Appointments
                .Where(a => a.DoctorId == doctor.Id && !a.IsDeleted)
                .Select(a => a.PatientId))
            .Union(_context.Patients
                .Where(p => p.PrimaryDoctorId == doctor.Id)
                .Select(p => p.Id))
            .Distinct()
            .ToListAsync();

        if (!patientIds.Any())
            return (true, "No patients found.", new List<PatientListResponseDTO>());

        var result = await _context.Patients
            .AsNoTracking()
            .Include(p => p.User)
            .Where(p => patientIds.Contains(p.Id))
            .Select(p => new PatientListResponseDTO
            {
                Id = p.Id,
                UserId = p.UserId,
                FirstName = p.User != null ? p.User.FirstName : "",
                LastName = p.User != null ? p.User.LastName : "",
                Email = p.User != null ? p.User.Email : "",
                PhoneNumber = p.User != null ? p.User.PhoneNumber : null,
                DateOfBirth = p.DateOfBirth,
                Gender = p.Gender,
                BloodType = p.BloodType,
                Allergies = p.Allergies,
                EmergencyContactName = p.EmergencyContactName,
                EmergencyContactPhone = p.EmergencyContactPhone,
                EmergencyContactRelationship = p.EmergencyContactRelationship,
                IsPrimary = p.PrimaryDoctorId == doctor.Id,
                SharedRecordsCount = _context.MedicalRecords.Count(r => r.PatientId == p.Id && r.AssignedDoctorId == doctor.Id && !r.IsDeleted),
                AppointmentCount = _context.Appointments.Count(a => a.PatientId == p.Id && a.DoctorId == doctor.Id && !a.IsDeleted),
                LatestSharedRecordDate = _context.MedicalRecords
                    .Where(r => r.PatientId == p.Id && r.AssignedDoctorId == doctor.Id && !r.IsDeleted)
                    .Max(r => (DateTime?)r.UploadedAt),
                LastAppointmentDate = _context.Appointments
                    .Where(a => a.PatientId == p.Id && a.DoctorId == doctor.Id && !a.IsDeleted)
                    .Max(a => (DateTime?)a.AppointmentDate)
            })
            .OrderByDescending(p => p.LastAppointmentDate ?? p.LatestSharedRecordDate ?? DateTime.MinValue)
            .ToListAsync();

        return (true, "Patients retrieved successfully.", result);
    }

    public async Task<(bool Success, string Message)> UpdateRecordMetadataAsync(
        Guid recordId,
        UpdateMedicalRecordMetadataDTO updateDto,
        Guid requestingUserId)
    {
        var record = await _context.MedicalRecords.FirstOrDefaultAsync(r => r.Id == recordId && !r.IsDeleted);
        if (record == null) return (false, "Record not found.");

        // Only patient or admin can update metadata
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == record.PatientId);
        if (patient?.UserId != requestingUserId)
        {
            var user = await _userManager.FindByIdAsync(requestingUserId.ToString());
            if (user == null || !await _userManager.IsInRoleAsync(user, "Admin"))
                return (false, "Unauthorized access.");
        }

        if (updateDto.RecordType != null) record.RecordType = updateDto.RecordType;
        if (updateDto.Description != null) record.Description = updateDto.Description;
        if (updateDto.RecordDate != null) record.RecordDate = updateDto.RecordDate.Value;
        if (updateDto.Tags != null) record.Tags = updateDto.Tags;

        record.LastModifiedAt = DateTime.Now;
        record.UpdatedAt = DateTime.Now;
        record.UpdatedBy = requestingUserId.ToString();

        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync(requestingUserId, "Medical Record Updated", $"Updated metadata for {record.OriginalFileName}", "0.0.0.0", "Service", "MedicalRecord", record.Id.ToString());

        return (true, "Record updated successfully.");
    }

    public async Task<(bool Success, string Message)> DeleteRecordAsync(
        Guid recordId,
        Guid requestingUserId)
    {
        var record = await _context.MedicalRecords.FirstOrDefaultAsync(r => r.Id == recordId && !r.IsDeleted);
        if (record == null) return (false, "Record not found.");

        // Check permissions (Owner or Admin)
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == record.PatientId);
        if (patient?.UserId != requestingUserId)
        {
            var user = await _userManager.FindByIdAsync(requestingUserId.ToString());
            if (user == null || !await _userManager.IsInRoleAsync(user, "Admin"))
                return (false, "Unauthorized access.");
        }

        record.IsDeleted = true;
        record.DeletedAt = DateTime.Now;
        record.DeletedBy = requestingUserId;
        record.IsLatestVersion = false;

        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync(requestingUserId, "Medical Record Deleted", $"Soft deleted {record.OriginalFileName}", "0.0.0.0", "Service", "MedicalRecord", record.Id.ToString());

        return (true, "Record deleted successfully.");
    }

    // ===========================
    // FSM TRANSITION METHODS
    // ===========================

    public async Task<(bool Success, string Message, MedicalRecordResponseDTO? Data)> SubmitForReviewAsync(
        Guid recordId, Guid patientUserId)
    {
        var record = await _context.MedicalRecords
            .Include(r => r.Patient).ThenInclude(p => p.User)
            .Include(r => r.Certifications).ThenInclude(c => c.Doctor).ThenInclude(d => d.User)
            .FirstOrDefaultAsync(r => r.Id == recordId && !r.IsDeleted);

        if (record == null) return (false, "Record not found.", null);

        // Verify ownership
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == record.PatientId);
        if (patient?.UserId != patientUserId)
            return (false, "Unauthorized: only the record owner can submit for review.", null);

        if (record.AssignedDoctorId == null)
            return (false, "Cannot submit: A doctor must be assigned to this record.", null);

        if (record.State != RecordState.Draft)
            return (false, $"Cannot submit: record is currently '{record.State}'. Only Draft records can be submitted.", null);

        record.State = RecordState.Pending;
        record.Notes = null; // Clear any prior rejection reason
        record.LastModifiedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(patientUserId, "Record Submitted for Review",
            $"{record.OriginalFileName} submitted for certification.",
            "0.0.0.0", "Service", "MedicalRecord", record.Id.ToString());

        return (true, "Record submitted for review.", MapToDto(record, patient.User.FirstName + " " + patient.User.LastName, true));
    }

    public async Task<(bool Success, string Message, MedicalRecordResponseDTO? Data)> CertifyRecordAsync(
        Guid recordId, Guid doctorUserId, CertifyRecordDTO dto)
    {
        var record = await _context.MedicalRecords
            .Include(r => r.Patient).ThenInclude(p => p.User)
            .Include(r => r.Certifications).ThenInclude(c => c.Doctor).ThenInclude(d => d.User)
            .FirstOrDefaultAsync(r => r.Id == recordId && !r.IsDeleted);

        if (record == null) return (false, "Record not found.", null);

        // Verify record integrity hash exists
        if (string.IsNullOrEmpty(record.FileHash))
        {
            _logger.LogError("Record {RecordId} has no file hash - cannot certify", recordId);
            return (false, "Record integrity hash missing", null);
        }

        // Verify doctor role
        var doctorUser = await _userManager.FindByIdAsync(doctorUserId.ToString());
        if (doctorUser == null || !await _userManager.IsInRoleAsync(doctorUser, "Doctor"))
            return (false, "Unauthorized: only doctors can certify records.", null);

        // Find the doctor entity
        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == doctorUserId);
        
        if (record.AssignedDoctorId != doctor?.Id)
            return (false, "Unauthorized: You are not the assigned doctor for this record.", null);

        if (record.State != RecordState.Pending)
            return (false, $"Cannot certify: record is currently '{record.State}'. Only Pending records can be certified.", null);

        if (doctor == null || string.IsNullOrEmpty(doctor.PrivateKeyEncrypted))
        {
            _logger.LogError("Doctor {DoctorUserId} has no private key available", doctorUserId);
            return (false, "Doctor signature key not available. Please contact admin.", null);
        }

        try {
            // Generate Real RSA Digital Signature
            var signature = await _signatureService.SignDataAsync(record.FileHash, doctor.PrivateKeyEncrypted);

            // Create the RecordCertification
            var certification = new RecordCertification
            {
                Id = Guid.NewGuid(),
                RecordId = record.Id,
                DoctorId = doctor.Id,
                RecordHash = record.FileHash, // Store hash that was signed
                DigitalSignature = signature,
                SignedAt = DateTime.Now,
                CertificationNotes = dto.CertificationNotes,
                IsValid = true,
                CreatedBy = doctorUser.Email ?? "Doctor"
            };

            record.State = RecordState.Certified;
            record.CertifiedAt = DateTime.Now;
            record.LastModifiedAt = DateTime.Now;

            await _context.RecordCertifications.AddAsync(certification);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Record {RecordId} certified by doctor {DoctorId} with digital signature",
                recordId, doctor.Id);

            var patientName = record.Patient?.User != null 
                ? $"{record.Patient.User.FirstName} {record.Patient.User.LastName}" 
                : "Unknown Patient";

            return (true, "Record certified successfully.", MapToDto(record, patientName, true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign or save certification for record {RecordId}", recordId);
            return (false, "Certification failed due to a cryptographic or database error.", null);
        }
    }

    public async Task<(bool Success, string Message, MedicalRecordResponseDTO? Data)> RejectRecordAsync(
        Guid recordId, Guid doctorUserId, RejectRecordDTO dto)
    {
        var record = await _context.MedicalRecords
            .Include(r => r.Patient).ThenInclude(p => p.User)
            .Include(r => r.Certifications).ThenInclude(c => c.Doctor).ThenInclude(d => d.User)
            .FirstOrDefaultAsync(r => r.Id == recordId && !r.IsDeleted);

        if (record == null) return (false, "Record not found.", null);

        var doctorUser = await _userManager.FindByIdAsync(doctorUserId.ToString());
        if (doctorUser == null || !await _userManager.IsInRoleAsync(doctorUser, "Doctor"))
            return (false, "Unauthorized: only doctors can reject records.", null);

        // Find the doctor entity
        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == doctorUserId);
        
        if (record.AssignedDoctorId != doctor?.Id)
            return (false, "Unauthorized: You are not the assigned doctor for this record.", null);

        if (record.State != RecordState.Pending)
            return (false, $"Cannot reject: record is currently '{record.State}'. Only Pending records can be rejected.", null);

        record.State = RecordState.Draft; // Send back to draft so patient can re-upload
        record.Notes = $"REJECTED: {dto.RejectionReason}";
        record.LastModifiedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(doctorUserId, "Record Rejected",
            $"{record.OriginalFileName} rejected. Reason: {dto.RejectionReason}",
            "0.0.0.0", "Service", "MedicalRecord", record.Id.ToString());

        var patientName = record.Patient?.User != null 
            ? $"{record.Patient.User.FirstName} {record.Patient.User.LastName}" 
            : "Unknown Patient";

        return (true, "Record rejected and returned to patient.", MapToDto(record, patientName, true));
    }

    public async Task<(bool Success, string Message, MedicalRecordResponseDTO? Data)> ArchiveRecordAsync(
        Guid recordId, Guid requestingUserId)
    {
        var record = await _context.MedicalRecords
            .Include(r => r.Patient).ThenInclude(p => p.User)
            .Include(r => r.Certifications).ThenInclude(c => c.Doctor).ThenInclude(d => d.User)
            .FirstOrDefaultAsync(r => r.Id == recordId && !r.IsDeleted);

        if (record == null) return (false, "Record not found.", null);

        // Verify: must be the patient or admin
        var patient = await _context.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == record.PatientId);
        if (patient?.UserId != requestingUserId)
        {
            var user = await _userManager.FindByIdAsync(requestingUserId.ToString());
            if (user == null || !await _userManager.IsInRoleAsync(user, "Admin"))
                return (false, "Unauthorized: only the record owner or an admin can archive records.", null);
        }

        if (record.State != RecordState.Certified)
            return (false, $"Cannot archive: record is currently '{record.State}'. Only Certified records can be archived.", null);

        record.State = RecordState.Archived;
        record.LastModifiedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(requestingUserId, "Record Archived",
            $"{record.OriginalFileName} has been archived.",
            "0.0.0.0", "Service", "MedicalRecord", record.Id.ToString());

        return (true, "Record archived.", MapToDto(record, patient!.User.FirstName + " " + patient.User.LastName, true));
    }

    public async Task<(bool Success, string Message, VerificationResultDTO? Data)> VerifyRecordSignatureAsync(
        Guid recordId, 
        Guid requestingUserId)
    {
        var record = await _context.MedicalRecords
            .Include(r => r.Certifications)
            .ThenInclude(c => c.Doctor)
            .ThenInclude(d => d.User)
            .FirstOrDefaultAsync(r => r.Id == recordId && !r.IsDeleted);

        if (record == null) return (false, "Record not found.", null);

        // Check permissions (same as download - patient, doctor, admin)
        var canAccess = await CheckAccessAsync(record, requestingUserId);
        if (!canAccess)
            return (false, "Unauthorized access.", null);

        var certification = record.Certifications.OrderByDescending(c => c.SignedAt).FirstOrDefault(c => c.IsValid);
        if (certification == null)
        {
            return (true, "Record not certified", new VerificationResultDTO
            {
                IsValid = false,
                IsCertified = false,
                IntegrityStatus = "Not Certified",
                Message = "This record has not been digitally certified by a doctor."
            });
        }

        var publicKey = certification.Doctor.PublicKey;
        if (string.IsNullOrEmpty(publicKey))
        {
            return (false, "Doctor public key not available", null);
        }

        // 1. Verify Digital Signature
        var isSignatureValid = await _signatureService.VerifySignatureAsync(
            certification.RecordHash,
            certification.DigitalSignature,
            publicKey);

        // 2. Verify File Integrity (Compare current record hash with certified hash)
        bool hashMatchesCurrentFile = true;
        if (!string.IsNullOrEmpty(record.FileHash))
        {
            hashMatchesCurrentFile = (record.FileHash == certification.RecordHash);
        }

        // 3. Determine Integrity Status
        string integrityStatus;
        if (isSignatureValid && hashMatchesCurrentFile)
            integrityStatus = "Valid";
        else if (!isSignatureValid)
            integrityStatus = "Invalid Signature";
        else if (!hashMatchesCurrentFile)
            integrityStatus = "File Tampered";
        else
            integrityStatus = "Unknown";

        _logger.LogInformation(
            "Record {RecordId} signature verification: {Status}",
            recordId, integrityStatus);

        return (true, "Verification complete", new VerificationResultDTO
        {
            IsValid = isSignatureValid && hashMatchesCurrentFile,
            Message = integrityStatus == "Valid" ? "Signature is valid and record is authentic." : $"Verification failed: {integrityStatus}",
            IsCertified = true,
            CertifiedBy = $"Dr. {certification.Doctor.User.FirstName} {certification.Doctor.User.LastName}",
            CertifiedAt = certification.SignedAt,
            RecordHash = certification.RecordHash,
            Signature = certification.DigitalSignature,
            HashMatchesCurrentFile = hashMatchesCurrentFile,
            IntegrityStatus = integrityStatus
        });
    }

    private async Task<bool> CheckAccessAsync(MedicalRecord record, Guid userId)
    {
        // Consolidated single query check: role + ownership + doctor relationships in one joined check.
        // This eliminates sequential DB round-trips for every access check.
        var accessInfo = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new 
            { 
                u.Role,
                IsOwner = u.PatientProfile != null && u.PatientProfile.Id == record.PatientId,
                IsAssignedDoctor = u.DoctorProfile != null && (record.AssignedDoctorId == u.DoctorProfile.Id),
                IsCertifyingDoctor = _context.RecordCertifications.Any(c => c.RecordId == record.Id && c.DoctorId == u.DoctorProfile!.Id && c.IsValid),
                HasClinicalRelationship = u.DoctorProfile != null && (
                    _context.Appointments.Any(a => a.PatientId == record.PatientId && a.DoctorId == u.DoctorProfile.Id) ||
                    _context.MedicalRecords.Any(r => r.PatientId == record.PatientId && r.AssignedDoctorId == u.DoctorProfile.Id && !r.IsDeleted)
                )
            })
            .FirstOrDefaultAsync();

        if (accessInfo == null) return false;
        if (accessInfo.Role == "Admin") return true;
        if (accessInfo.IsOwner) return true;
        
        if (accessInfo.Role == "Doctor")
        {
            if (accessInfo.IsAssignedDoctor) return true;
            if (accessInfo.IsCertifyingDoctor) return true;
            if (accessInfo.HasClinicalRelationship) return true;
            if (record.State == RecordState.Emergency) return true;
        }

        return false;
    }

    /// <summary>
    /// Unified helper to combine Role Check, Patient Ownership Check, and Patient Name retrieval
    /// into a single efficient database round-trip. Directly solves the 14s latency issue.
    /// </summary>
    private async Task<(bool Allowed, string Message, string? PatientName)> GetPatientMetadataAndCheckAccessAsync(Guid patientId, Guid requestingUserId)
    {
        var metadata = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == requestingUserId)
            .Select(u => new 
            {
                u.Role,
                // Check if this specific user owns the patient profile being queried
                IsOwner = u.PatientProfile != null && u.PatientProfile.Id == patientId,
                // Fetch the TARGET patient's name (might be different from requester if Doctor/Admin)
                TargetPatientName = _context.Patients
                    .Where(p => p.Id == patientId)
                    .Select(p => p.User!.FirstName + " " + p.User.LastName)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (metadata == null) return (false, "User session not found.", null);

        // Security Enforcement
        if (metadata.Role == "Patient" && !metadata.IsOwner)
            return (false, "Unauthorized access to these records.", null);

        // Fallback for TargetPatientName if patient doesn't exist
        var patientName = metadata.TargetPatientName ?? "Patient";

        return (true, "Access Granted", patientName);
    }

    private MedicalRecordResponseDTO MapToDto(MedicalRecord record, string uploaderName, bool canDownload)
    {
        var cert = record.Certifications.OrderByDescending(c => c.SignedAt).FirstOrDefault();

        string stateLabel = record.State switch
        {
            RecordState.Draft => "Draft",
            RecordState.Pending => "Awaiting Review",
            RecordState.Certified => "Certified",
            RecordState.Archived => "Archived",
            RecordState.Emergency => "Emergency Access",
            _ => record.State.ToString()
        };

        string? rejectionReason = null;
        if (record.State == RecordState.Draft && record.Notes != null && record.Notes.StartsWith("REJECTED:"))
            rejectionReason = record.Notes["REJECTED:".Length..].Trim();

        // Robust name resolution
        string? assignedDocName = null;
        if (record.AssignedDoctor?.User != null)
        {
            var firstName = record.AssignedDoctor.User.FirstName?.Trim();
            var lastName = record.AssignedDoctor.User.LastName?.Trim();
            if (!string.IsNullOrWhiteSpace(firstName) || !string.IsNullOrWhiteSpace(lastName))
            {
                assignedDocName = $"Dr. {firstName} {lastName}".Replace("  ", " ").Trim();
            }
        }

        string? certifiedByDocName = null;
        if (cert?.Doctor?.User != null)
        {
            var fName = cert.Doctor.User.FirstName?.Trim();
            var lName = cert.Doctor.User.LastName?.Trim();
            if (!string.IsNullOrWhiteSpace(fName) || !string.IsNullOrWhiteSpace(lName))
            {
                certifiedByDocName = $"{fName} {lName}".Replace("  ", " ").Trim();
            }
        }

        string finalUploadedBy = uploaderName;
        if (record.RecordType != null && record.RecordType.Contains("Auto-Generated") && assignedDocName != null)
        {
            finalUploadedBy = assignedDocName;
        }

        return new MedicalRecordResponseDTO
        {
            Id = record.Id,
            PatientId = record.PatientId,
            OriginalFileName = record.OriginalFileName,
            RecordType = record.RecordType,
            Description = record.Description,
            RecordDate = record.RecordDate,
            FileSize = record.FileSizeBytes,
            FileSizeFormatted = FormatFileSize(record.FileSizeBytes),
            MimeType = record.MimeType,
            State = record.State,
            StateLabel = stateLabel,
            RejectionReason = rejectionReason,
            UploadedAt = record.UploadedAt,
            UploadedBy = finalUploadedBy,
            PatientName = record.Patient?.User != null 
                ? record.Patient.User.FirstName + " " + record.Patient.User.LastName 
                : uploaderName, // Fallback if record.Patient wasn't included (Optimization for GetPatientRecordsAsync)
            AssignedDoctorName = assignedDocName,
            AssignedDoctorId = record.AssignedDoctorId,
            AssignedDepartment = record.AssignedDoctor?.Department?.Name,
            IsCertified = cert != null,
            CertifiedBy = certifiedByDocName,
            CertifiedById = cert?.DoctorId,
            CertifiedAt = cert?.SignedAt,
            Version = record.Version,
            CanDownload = canDownload,
            
            // New fields
            RelativeTimeString = DateGroupingHelper.GetRelativeTimeString(record.UploadedAt),
            TimePeriod = DateGroupingHelper.GetTimePeriodLabel(record.UploadedAt),
            Tags = record.Tags
        };
    }

    private string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:n1} {suffixes[counter]}";
    }

    public async Task<(bool Success, string Message, SmartDoctorSuggestionDTO? Data)> GetSmartDoctorSuggestionsAsync(Guid patientId)
    {
        var patient = await _context.Patients
            .Include(p => p.PrimaryDoctor)
                .ThenInclude(d => d!.User)
            .Include(p => p.PrimaryDoctor)
                .ThenInclude(d => d!.Department)
            .FirstOrDefaultAsync(p => p.Id == patientId);

        if (patient == null) return (false, "Patient not found.", null);

        var now = DateTime.UtcNow;
        var suggestion = new SmartDoctorSuggestionDTO();

        // 1. Upcoming appointment (within next 30 days, Confirmed or Requested)
        var appointment = await _context.Appointments
            .Include(a => a.Doctor)
                .ThenInclude(d => d.User)
            .Include(a => a.Doctor)
                .ThenInclude(d => d.Department)
            .Where(a => a.PatientId == patientId
                     && a.ScheduledAt >= now
                     && a.ScheduledAt <= now.AddDays(30)
                     && (a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.Scheduled))
            .OrderBy(a => a.ScheduledAt)
            .FirstOrDefaultAsync();

        if (appointment != null)
        {
            suggestion.UpcomingAppointmentDoctor = new DoctorSuggestionItem
            {
                Id = appointment.Doctor.Id.ToString(),
                UserId = appointment.Doctor.UserId.ToString(),
                FullName = $"Dr. {appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}",
                Department = appointment.Doctor.Department?.Name ?? "General",
                SuggestionType = "Appointment",
                SuggestionLabel = $"Appointment: {appointment.ScheduledAt:MMM d}",
                ProfilePictureUrl = appointment.Doctor.User.ProfilePictureUrl
            };
        }

        // 2. Primary doctor
        if (patient.PrimaryDoctor != null)
        {
            suggestion.PrimaryDoctor = new DoctorSuggestionItem
            {
                Id = patient.PrimaryDoctor.Id.ToString(),
                UserId = patient.PrimaryDoctor.UserId.ToString(),
                FullName = $"Dr. {patient.PrimaryDoctor.User.FirstName} {patient.PrimaryDoctor.User.LastName}",
                Department = patient.PrimaryDoctor.Department?.Name ?? "General",
                SuggestionType = "Primary",
                SuggestionLabel = "Your primary doctor",
                ProfilePictureUrl = patient.PrimaryDoctor.User.ProfilePictureUrl
            };
        }

        // 3. Recent doctor(s) from last 3 unique records.
        // Take(10) server-side to avoid loading the full records table into memory.
        var recentRecords = await _context.MedicalRecords
            .AsNoTracking()
            .Include(r => r.AssignedDoctor)
                .ThenInclude(d => d!.User)
            .Include(r => r.AssignedDoctor)
                .ThenInclude(d => d!.Department)
            .Where(r => r.PatientId == patientId
                     && r.AssignedDoctorId != null
                     && !r.IsDeleted)
            .OrderByDescending(r => r.UploadedAt)
            .Take(10)
            .ToListAsync();

        var seenIds = new HashSet<Guid>();
        foreach (var record in recentRecords)
        {
            if (record.AssignedDoctor == null) continue;
            if (!seenIds.Add(record.AssignedDoctor.Id)) continue;

            var daysAgo = (int)(now - record.UploadedAt).TotalDays;
            var label = daysAgo == 0 ? "Today" : daysAgo == 1 ? "Yesterday" : $"{daysAgo} days ago";

            suggestion.RecentDoctors.Add(new DoctorSuggestionItem
            {
                Id = record.AssignedDoctor.Id.ToString(),
                UserId = record.AssignedDoctor.UserId.ToString(),
                FullName = $"Dr. {record.AssignedDoctor.User.FirstName} {record.AssignedDoctor.User.LastName}",
                Department = record.AssignedDoctor.Department?.Name ?? "General",
                SuggestionType = "Recent",
                SuggestionLabel = $"Last visit {label}",
                ProfilePictureUrl = record.AssignedDoctor.User.ProfilePictureUrl
            });

            if (seenIds.Count >= 3) break;
        }

        // 4. Set recommended = first in priority chain
        suggestion.RecommendedDoctor =
            suggestion.UpcomingAppointmentDoctor
            ?? suggestion.PrimaryDoctor
            ?? suggestion.RecentDoctors.FirstOrDefault();

        return (true, "Suggestions retrieved.", suggestion);
    }

    public async Task<(bool Success, string Message)> DeletePatientEntirelyAsync(
        Guid patientId,
        Guid requestingUserId)
    {
        _logger.LogWarning("DANGER: Starting full deletion for patient {PatientId} requested by {UserId}", patientId, requestingUserId);

        var patient = await _context.Patients
            .Include(p => p.User)
            .Include(p => p.MedicalRecords)
            .Include(p => p.Appointments)
            .Include(p => p.StructuredRecords)
            .FirstOrDefaultAsync(p => p.Id == patientId);

        if (patient == null) return (false, "Patient not found.");

        try
        {
            // 1. Delete Files from Tigris (S3)
            var fileKeys = patient.MedicalRecords.Select(r => r.S3ObjectKey).Distinct().ToList();
            foreach (var key in fileKeys)
            {
                _logger.LogInformation("Deleting file {Key} for patient {PatientId}", key, patientId);
                await _tigrisService.DeleteFileAsync(key);
            }

            // 2. Delete Appointments (due to Restrict constraint)
            if (patient.Appointments.Any())
            {
                _context.Appointments.RemoveRange(patient.Appointments);
            }

            // 3. Delete Structured Records (due to Restrict constraint)
            if (patient.StructuredRecords.Any())
            {
                _context.PatientHealthRecords.RemoveRange(patient.StructuredRecords);
            }

            // 4. Delete Scan History (due to Restrict constraint)
            var scanHistory = await _context.ScanHistories.Where(s => s.PatientId == patientId).ToListAsync();
            if (scanHistory.Any())
            {
                _context.ScanHistories.RemoveRange(scanHistory);
            }

            // 5. Delete Access Sessions (due to Restrict/NoAction)
            var accessSessions = await _context.AccessSessions.Where(s => s.PatientId == patientId).ToListAsync();
            if (accessSessions.Any())
            {
                _context.AccessSessions.RemoveRange(accessSessions);
            }

            // 6. Delete ApplicationUser (Principal)
            // This will cascade to Patient, MedicalRecords, QRTokens, etc.
            var user = patient.User;
            if (user != null)
            {
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return (false, $"Failed to delete user account: {errors}");
                }
            }
            else
            {
                _context.Patients.Remove(patient);
                await _context.SaveChangesAsync();
            }

            // 7. Audit Log
            string patientName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown Patient";
            await _auditLogService.LogAsync(
                requestingUserId,
                "Patient Entirely Deleted",
                $"Deleted patient {patientName} and all associated data.",
                "0.0.0.0", "Service", "Patient", patientId.ToString(),
                AuditSeverity.Critical);

            return (true, "Patient and all associated data have been permanently deleted.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical failure during patient deletion for {PatientId}", patientId);
            return (false, $"Deletion failed: {ex.Message}");
        }
    }
}
