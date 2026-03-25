using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using SecureMedicalRecordSystem.API.Authorization;
using SecureMedicalRecordSystem.Core.DTOs;
using SecureMedicalRecordSystem.Core.DTOs.Doctor;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Infrastructure.Data;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Core.DTOs.MedicalRecords;
using System.Security.Claims;
using System.Text.Json;

namespace SecureMedicalRecordSystem.API.Controllers;

[ApiController]
[Route("api/doctor")]
[Authorize(Policy = "DoctorPolicy")]
public class DoctorController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMedicalRecordsService _medicalRecordsService;
    private readonly IImageStorageService _imageStorageService;
    private readonly IAuditLogService _auditLogService;
    private readonly ICachingService _cache;
    private readonly ILogger<DoctorController> _logger;
    private readonly IAuthService _authService;

    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public DoctorController(
        ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager,
        IMedicalRecordsService medicalRecordsService,
        IImageStorageService imageStorageService,
        IAuditLogService auditLogService,
        ICachingService cache,
        ILogger<DoctorController> logger,
        IAuthService authService)
    {
        _context = context;
        _userManager = userManager;
        _medicalRecordsService = medicalRecordsService;
        _imageStorageService = imageStorageService;
        _auditLogService = auditLogService;
        _cache = cache;
        _logger = logger;
        _authService = authService;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var cacheKey = $"doctor:profile:{userId}";
        
        var profile = await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Department)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (doctor == null) return null!;

            return BuildExtendedProfileDTO(doctor);
        }, TimeSpan.FromMinutes(30));

        if (profile == null)
            return NotFound(ApiResponse.FailureResult("Doctor profile not found."));

        return Ok(ApiResponse.SuccessResult(profile, "Profile retrieved successfully."));
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateDoctorExtendedProfileDTO updateDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
        if (doctor == null) return NotFound(ApiResponse.FailureResult("Doctor profile not found."));

        try
        {
            // Basic fields
            if (!string.IsNullOrEmpty(updateDto.Specialization)) doctor.Specialization = updateDto.Specialization;
            if (!string.IsNullOrEmpty(updateDto.HospitalAffiliation)) doctor.HospitalAffiliation = updateDto.HospitalAffiliation;
            if (!string.IsNullOrEmpty(updateDto.ContactNumber)) doctor.ContactNumber = updateDto.ContactNumber;

            // Extended identity
            if (updateDto.Biography != null) doctor.Biography = updateDto.Biography;
            if (updateDto.YearsOfExperience.HasValue) doctor.YearsOfExperience = updateDto.YearsOfExperience;
            if (updateDto.ConsultationFee != null) doctor.ConsultationFee = updateDto.ConsultationFee;
            if (updateDto.ConsultationHours != null) doctor.ConsultationHours = updateDto.ConsultationHours;
            if (updateDto.ConsultationLocation != null) doctor.ConsultationLocation = updateDto.ConsultationLocation;
            if (updateDto.AcceptsNewPatients.HasValue) doctor.AcceptsNewPatients = updateDto.AcceptsNewPatients;

            // JSON sections — always update when provided (allows clearing with empty list)
            if (updateDto.Education != null)
                doctor.EducationJson = JsonSerializer.Serialize(updateDto.Education);
            if (updateDto.Experience != null)
                doctor.ExperienceJson = JsonSerializer.Serialize(updateDto.Experience);
            if (updateDto.ProfessionalCertifications != null)
                doctor.ProfessionalCertificationsJson = JsonSerializer.Serialize(updateDto.ProfessionalCertifications);
            if (updateDto.Awards != null)
                doctor.AwardsJson = JsonSerializer.Serialize(updateDto.Awards);
            if (updateDto.Procedures != null)
                doctor.ProceduresJson = JsonSerializer.Serialize(updateDto.Procedures);
            if (updateDto.Languages != null)
                doctor.LanguagesJson = JsonSerializer.Serialize(updateDto.Languages);
            if (updateDto.CustomAttributes != null)
                doctor.CustomAttributesJson = JsonSerializer.Serialize(updateDto.CustomAttributes);

            await _context.SaveChangesAsync();
            await _cache.InvalidateAsync($"doctor:profile:{userId}");

            // Return updated profile with new score
            var updated = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Department)
                .OrderBy(d => d.Id)
                .FirstOrDefaultAsync(d => d.Id == doctor.Id);

            var result = BuildExtendedProfileDTO(updated!);
            return Ok(ApiResponse.SuccessResult(result, "Profile updated successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update doctor profile for userId {UserId}", userId);
            return BadRequest(ApiResponse.FailureResult($"Update failed: {ex.Message}"));
        }
    }

    [HttpPost("profile/picture")]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized(ApiResponse.FailureResult("User not found."));

        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse.FailureResult("No file uploaded."));

        try
        {
            if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            {
                await _imageStorageService.DeleteImageAsync(user.ProfilePictureUrl);
            }

            using var stream = file.OpenReadStream();
            var uploadResult = await _imageStorageService.UploadImageAsync(stream, file.FileName, "profile-pictures");
            user.ProfilePictureUrl = uploadResult;
            
            await _userManager.UpdateAsync(user);
            await _cache.InvalidateAsync($"doctor:profile:{user.Id}");

            return Ok(ApiResponse.SuccessResult(new { url = uploadResult }, "Profile picture updated successfully."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.FailureResult("Failed to upload image: " + ex.Message));
        }
    }

    [HttpDelete("profile/picture")]
    public async Task<IActionResult> DeleteProfilePicture()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized(ApiResponse.FailureResult("User not found."));

        if (string.IsNullOrEmpty(user.ProfilePictureUrl))
            return BadRequest(ApiResponse.FailureResult("No profile picture to delete."));

        try
        {
            await _imageStorageService.DeleteImageAsync(user.ProfilePictureUrl);
            user.ProfilePictureUrl = null;
            await _userManager.UpdateAsync(user);
            await _cache.InvalidateAsync($"doctor:profile:{user.Id}");

            return Ok(ApiResponse.SuccessResult((object?)null, "Profile picture deleted successfully."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.FailureResult("Failed to delete image: " + ex.Message));
        }
    }

    // =====================
    // SHARED HELPERS
    // =====================

    public static DoctorExtendedProfileDTO BuildExtendedProfileDTO(Doctor doctor)
    {
        var edu = DeserializeList<DoctorProfileSectionDTO>(doctor.EducationJson);
        var exp = DeserializeList<DoctorProfileSectionDTO>(doctor.ExperienceJson);
        var certs = DeserializeList<DoctorCertificationItemDTO>(doctor.ProfessionalCertificationsJson);
        var awards = DeserializeList<DoctorProfileSectionDTO>(doctor.AwardsJson);
        var procs = DeserializeList<string>(doctor.ProceduresJson);
        var langs = DeserializeList<string>(doctor.LanguagesJson);
        var custom = DeserializeList<DoctorCustomAttributeDTO>(doctor.CustomAttributesJson);

        var (score, missing) = CalculateProfileCompletion(doctor, edu, exp, certs, langs);

        return new DoctorExtendedProfileDTO
        {
            DoctorId = doctor.Id,
            UserId = doctor.UserId,
            FirstName = doctor.User?.FirstName ?? string.Empty,
            LastName = doctor.User?.LastName ?? string.Empty,
            Email = doctor.User?.Email ?? string.Empty,
            ProfilePictureUrl = doctor.User?.ProfilePictureUrl,
            NMCLicense = doctor.NMCLicense,
            DepartmentId = doctor.DepartmentId.ToString(),
            DepartmentName = doctor.Department?.Name ?? string.Empty,
            Specialization = doctor.Specialization,
            HospitalAffiliation = doctor.HospitalAffiliation,
            ContactNumber = doctor.ContactNumber,
            Biography = doctor.Biography,
            YearsOfExperience = doctor.YearsOfExperience,
            ConsultationFee = doctor.ConsultationFee,
            ConsultationHours = doctor.ConsultationHours,
            ConsultationLocation = doctor.ConsultationLocation,
            AcceptsNewPatients = doctor.AcceptsNewPatients,
            Education = edu,
            Experience = exp,
            ProfessionalCertifications = certs,
            Awards = awards,
            Procedures = procs,
            Languages = langs,
            CustomAttributes = custom,
            ProfileCompletionScore = score,
            MissingProfileFields = missing
        };
    }

    private static List<T> DeserializeList<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<T>();
        try { return JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? new List<T>(); }
        catch { return new List<T>(); }
    }

    private static (int score, List<string> missing) CalculateProfileCompletion(
        Doctor doctor,
        List<DoctorProfileSectionDTO> edu,
        List<DoctorProfileSectionDTO> exp,
        List<DoctorCertificationItemDTO> certs,
        List<string> langs)
    {
        var fieldWeights = new List<(string Field, bool HasValue, int Weight)>
        {
            ("Biography", !string.IsNullOrWhiteSpace(doctor.Biography), 20),
            ("Education", edu.Any(), 15),
            ("Experience", exp.Any(), 15),
            ("Contact Number", !string.IsNullOrWhiteSpace(doctor.ContactNumber), 10),
            ("Languages", langs.Any(), 10),
            ("Certifications", certs.Any(), 10),
            ("Hospital Affiliation", !string.IsNullOrWhiteSpace(doctor.HospitalAffiliation), 10),
            ("Consultation Info", !string.IsNullOrWhiteSpace(doctor.ConsultationHours), 10),
        };

        int totalWeight = fieldWeights.Sum(f => f.Weight);
        int earned = fieldWeights.Where(f => f.HasValue).Sum(f => f.Weight);
        var missing = fieldWeights.Where(f => !f.HasValue).Select(f => f.Field).ToList();

        return (totalWeight == 0 ? 0 : (int)Math.Round((double)earned / totalWeight * 100), missing);
    }

    [HttpGet("pending-records")]
    public async Task<IActionResult> GetPendingRecords()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var result = await _medicalRecordsService.GetPendingRecordsForDoctorAsync(userId);
        if (!result.Success) return BadRequest(ApiResponse.FailureResult(result.Message));
        
        return Ok(ApiResponse.SuccessResult(result.Data, result.Message));
    }

    [HttpGet("certified-records")]
    public async Task<IActionResult> GetCertifiedRecords()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var result = await _medicalRecordsService.GetCertifiedRecordsForDoctorAsync(userId);
        if (!result.Success) return BadRequest(ApiResponse.FailureResult(result.Message));

        return Ok(ApiResponse.SuccessResult(result.Data, result.Message));
    }

    [HttpGet("appointments")]
    public IActionResult GetAppointments()
    {
        // Placeholder for Phase 5
        return Ok(ApiResponse.SuccessResult(new List<object>(), "Doctor appointments retrieved (placeholder)."));
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var stats = await _auditLogService.GetDoctorStatisticsAsync(userId);
        return Ok(ApiResponse.SuccessResult(stats, "Doctor statistics retrieved successfully."));
    }

    [HttpGet("patients")]
    public async Task<IActionResult> GetPatients()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var result = await _medicalRecordsService.GetPatientsForDoctorAsync(userId);
        if (!result.Success) return BadRequest(ApiResponse.FailureResult(result.Message));

        return Ok(ApiResponse.SuccessResult(result.Data, result.Message));
    }

    [HttpGet("patients/{id}")]
    public async Task<IActionResult> GetPatientInfo(Guid id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var result = await _medicalRecordsService.GetPatientByIdForDoctorAsync(id, userId);
        if (!result.Success) return NotFound(ApiResponse.FailureResult(result.Message));

        return Ok(ApiResponse.SuccessResult(result.Data, result.Message));
    }

    // =====================
    // PATIENT MANAGEMENT
    // =====================

    [HttpPost("patients")]
    public async Task<IActionResult> CreatePatient([FromBody] SecureMedicalRecordSystem.Core.DTOs.Auth.CreatePatientRequestDTO request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
        if (doctor == null) return Unauthorized(ApiResponse.FailureResult("Doctor profile not found."));

        var result = await _authService.CreatePatientAccountAsync(request, doctor.Id, userId);
        if (result.Success)
        {
            return Ok(ApiResponse.SuccessResult((object?)null, result.Message));
        }
        return BadRequest(ApiResponse.FailureResult(result.Message));
    }

    // =====================
    // FSM TRANSITION ENDPOINTS
    // =====================

    [HttpPost("records/{recordId}/certify")]
    public async Task<IActionResult> Certify(Guid recordId, [FromBody] CertifyRecordDTO dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var result = await _medicalRecordsService.CertifyRecordAsync(recordId, userId, dto);
        if (!result.Success)
        {
            if (result.Message.Contains("Unauthorized")) return Forbid();
            return BadRequest(ApiResponse.FailureResult(result.Message));
        }
        return Ok(ApiResponse.SuccessResult(result.Data, result.Message));
    }

    [HttpPost("records/{recordId}/reject")]
    public async Task<IActionResult> Reject(Guid recordId, [FromBody] RejectRecordDTO dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var result = await _medicalRecordsService.RejectRecordAsync(recordId, userId, dto);
        if (!result.Success)
        {
            if (result.Message.Contains("Unauthorized")) return Forbid();
            return BadRequest(ApiResponse.FailureResult(result.Message));
        }
        return Ok(ApiResponse.SuccessResult(result.Data, result.Message));
    }

    // Removed legacy buffered download


    [HttpGet("records/{recordId}/view")]
    public async Task<IActionResult> ViewRecord(Guid recordId)
    {
        _logger.LogInformation("[STREAMING MODE ACTIVE] Doctor viewing record {RecordId} inline", recordId);
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var result = await _medicalRecordsService.StreamDownloadRecordAsync(recordId, userId);
        if (!result.Success)
        {
            if (result.Message == "Unauthorized access.") return Forbid();
            return BadRequest(ApiResponse.FailureResult(result.Message));
        }

        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Content-Disposition"] = $"inline; filename=\"{result.FileName}\"";
        return File(result.FileStream!, result.ContentType!, enableRangeProcessing: true);
    }

    [HttpGet("records/{recordId}/stream-download")]
    public async Task<IActionResult> StreamDownloadRecord(Guid recordId)
    {
        _logger.LogInformation("[STREAMING MODE ACTIVE] Doctor downloading record {RecordId} as attachment", recordId);
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var result = await _medicalRecordsService.StreamDownloadRecordAsync(recordId, userId);
        if (!result.Success)
        {
            if (result.Message == "Unauthorized access.") return Forbid();
            return BadRequest(ApiResponse.FailureResult(result.Message));
        }

        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(result.FileStream!, result.ContentType!, result.FileName, enableRangeProcessing: false);
    }

    [HttpDelete("patients/{id}")]
    public async Task<IActionResult> DeletePatient(Guid id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        // Permissions: Only the assigned/primary doctor or an admin can delete.
        // For now, any doctor who can see the patient in their directory (has shared records/primary) can delete.
        var result = await _medicalRecordsService.DeletePatientEntirelyAsync(id, userId);
        if (!result.Success)
        {
            return BadRequest(ApiResponse.FailureResult(result.Message));
        }

        return Ok(ApiResponse.SuccessResult((object?)null, result.Message));
    }

}
