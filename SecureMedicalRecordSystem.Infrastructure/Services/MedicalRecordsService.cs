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
            var timestamp = DateTime.UtcNow;
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
                RecordDate = uploadDto.RecordDate ?? DateTime.UtcNow,
                State = RecordState.Pending,
                Version = 1,
                IsLatestVersion = true,
                UploadedAt = DateTime.UtcNow,
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

    public async Task<(bool Success, string Message, Stream? FileStream, string? FileName, string? ContentType)> DownloadRecordAsync(
        Guid recordId,
        Guid requestingUserId)
    {
        var record = await _context.MedicalRecords
            .Include(r => r.Patient)
            .FirstOrDefaultAsync(r => r.Id == recordId && !r.IsDeleted);

        if (record == null)
            return (false, "Record not found.", null, null, null);

        // Check permissions
        var canAccess = await CheckAccessAsync(record, requestingUserId);
        if (!canAccess)
            return (false, "Unauthorized access.", null, null, null);

        try
        {
            // Download encrypted file
            var encryptedStream = await _tigrisService.DownloadFileAsync(record.S3ObjectKey);

            // Decrypt file
            var decryptedStream = await _encryptionService.DecryptFileAsync(encryptedStream);

            // Optional: Verify integrity
            // var currentHash = await _encryptionService.ComputeFileHashAsync(decryptedStream);
            // decryptedStream.Position = 0;
            // if (currentHash != record.FileHash) _logger.LogWarning("Integrity mismatch for record {Id}", recordId);

            await _auditLogService.LogAsync(
                requestingUserId,
                "Medical Record Downloaded",
                $"Downloaded {record.OriginalFileName}",
                "0.0.0.0", "Service", "MedicalRecord", record.Id.ToString());

            return (true, "Success", decryptedStream, record.OriginalFileName, record.MimeType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading record {Id}", recordId);
            return (false, "An error occurred during download.", null, null, null);
        }
    }

    public async Task<(bool Success, string Message, List<MedicalRecordResponseDTO>? Data)> GetPatientRecordsAsync(
        Guid patientId,
        Guid requestingUserId)
    {
        // Access check: a Patient can only see their own records.
        // Use a direct context predicate instead of two separate UserManager calls.
        var requestingUserRole = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == requestingUserId)
            .Select(u => u.Role)
            .FirstOrDefaultAsync();

        if (requestingUserRole == null) return (false, "User not found.", null);

        if (requestingUserRole == "Patient")
        {
            var ownerUserId = await _context.Patients
                .AsNoTracking()
                .Where(p => p.Id == patientId)
                .Select(p => p.UserId)
                .FirstOrDefaultAsync();

            if (requestingUserId != ownerUserId)
                return (false, "Unauthorized access.", null);
        }

        var records = await _context.MedicalRecords
            .AsNoTracking()
            .Where(r => r.PatientId == patientId && !r.IsDeleted)
            .Include(r => r.Patient)
                .ThenInclude(p => p.User)
            .Include(r => r.Certifications)
                .ThenInclude(c => c.Doctor)
                    .ThenInclude(d => d.User)
            .OrderByDescending(r => r.UploadedAt)
            .ToListAsync();

        var dtos = records.Select(r => MapToDto(r, r.Patient.User.FirstName + " " + r.Patient.User.LastName, true)).ToList();
        return (true, "Success", dtos);
    }

    public async Task<(bool Success, string Message, MedicalRecordResponseDTO? Data)> GetRecordDetailsAsync(
        Guid recordId,
        Guid requestingUserId)
    {
        var record = await _context.MedicalRecords
            .Include(r => r.Patient)
            .ThenInclude(p => p.User)
            .Include(r => r.Certifications)
            .ThenInclude(c => c.Doctor)
            .ThenInclude(d => d.User)
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

        var records = await _context.MedicalRecords
            .AsNoTracking()
            .Where(r => r.State == RecordState.Certified
                     && !r.IsDeleted
                     && r.Certifications.Any(c => c.DoctorId == doctorId && c.IsValid))
            .Include(r => r.Patient)
                .ThenInclude(p => p.User)
            .Include(r => r.Certifications)
                .ThenInclude(c => c.Doctor)
                    .ThenInclude(d => d.User)
            .OrderByDescending(r => r.CertifiedAt)
            .ToListAsync();

        var dtos = records.Select(r => {
            var patientName = r.Patient?.User != null 
                ? $"{r.Patient.User.FirstName} {r.Patient.User.LastName}" 
                : "Unknown Patient";
            return MapToDto(r, patientName, true);
        }).ToList();
        return (true, "Success", dtos);
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

        record.LastModifiedAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;
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
        record.DeletedAt = DateTime.UtcNow;
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
        record.LastModifiedAt = DateTime.UtcNow;
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
                SignedAt = DateTime.UtcNow,
                CertificationNotes = dto.CertificationNotes,
                IsValid = true,
                CreatedBy = doctorUser.Email ?? "Doctor"
            };

            record.State = RecordState.Certified;
            record.CertifiedAt = DateTime.UtcNow;
            record.LastModifiedAt = DateTime.UtcNow;

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
        record.LastModifiedAt = DateTime.UtcNow;
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
        record.LastModifiedAt = DateTime.UtcNow;
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
        // Single query to get the user's role — avoids 2 sequential UserManager calls.
        var userRole = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Role)
            .FirstOrDefaultAsync();

        if (userRole == null) return false;
        if (userRole == "Admin") return true;

        // Check if the requesting user is the patient who owns this record.
        var patientUserId = await _context.Patients
            .AsNoTracking()
            .Where(p => p.Id == record.PatientId)
            .Select(p => p.UserId)
            .FirstOrDefaultAsync();

        if (patientUserId == userId) return true;

        if (userRole == "Doctor")
        {
            var doctorId = await _context.Doctors
                .AsNoTracking()
                .Where(d => d.UserId == userId)
                .Select(d => d.Id)
                .FirstOrDefaultAsync();

            // Doctors can access records in Pending state, but ONLY if assigned to them.
            return record.State == RecordState.Pending && record.AssignedDoctorId == doctorId;
        }

        return false;
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
            UploadedBy = uploaderName,
            PatientName = record.Patient?.User?.FirstName + " " + record.Patient?.User?.LastName,
            AssignedDoctorName = record.AssignedDoctor != null 
                ? "Dr. " + record.AssignedDoctor.User?.FirstName + " " + record.AssignedDoctor.User?.LastName 
                : null,
            AssignedDepartment = record.AssignedDoctor?.Department?.Name,
            IsCertified = cert != null,
            CertifiedBy = cert?.Doctor?.User?.FirstName + " " + cert?.Doctor?.User?.LastName,
            CertifiedAt = cert?.SignedAt,
            Version = record.Version,
            CanDownload = canDownload
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
                     && (a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.Requested))
            .OrderBy(a => a.ScheduledAt)
            .FirstOrDefaultAsync();

        if (appointment != null)
        {
            suggestion.UpcomingAppointmentDoctor = new DoctorSuggestionItem
            {
                Id = appointment.Doctor.Id.ToString(),
                FullName = $"Dr. {appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}",
                Department = appointment.Doctor.Department?.Name ?? "General",
                SuggestionType = "Appointment",
                SuggestionLabel = $"Appointment: {appointment.ScheduledAt:MMM d}"
            };
        }

        // 2. Primary doctor
        if (patient.PrimaryDoctor != null)
        {
            suggestion.PrimaryDoctor = new DoctorSuggestionItem
            {
                Id = patient.PrimaryDoctor.Id.ToString(),
                FullName = $"Dr. {patient.PrimaryDoctor.User.FirstName} {patient.PrimaryDoctor.User.LastName}",
                Department = patient.PrimaryDoctor.Department?.Name ?? "General",
                SuggestionType = "Primary",
                SuggestionLabel = "Your primary doctor"
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
                FullName = $"Dr. {record.AssignedDoctor.User.FirstName} {record.AssignedDoctor.User.LastName}",
                Department = record.AssignedDoctor.Department?.Name ?? "General",
                SuggestionType = "Recent",
                SuggestionLabel = $"Last visit {label}"
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
}
