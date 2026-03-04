using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using SecureMedicalRecordSystem.API.Authorization;
using SecureMedicalRecordSystem.Core.DTOs;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Infrastructure.Data;
using SecureMedicalRecordSystem.Core.Interfaces;
using System.Security.Claims;
using SecureMedicalRecordSystem.Core.DTOs.QR;
using SecureMedicalRecordSystem.Core.Enums;

namespace SecureMedicalRecordSystem.API.Controllers;

[ApiController]
[Route("api/patient")]
[Authorize(Policy = "PatientPolicy")]
public class PatientController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMedicalRecordsService _medicalRecordsService;
    private readonly IAuditLogService _auditLogService;

    public PatientController(
        ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager,
        IMedicalRecordsService medicalRecordsService,
        IAuditLogService auditLogService)
    {
        _context = context;
        _userManager = userManager;
        _medicalRecordsService = medicalRecordsService;
        _auditLogService = auditLogService;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));
        }

        var patient = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
        {
            return NotFound(ApiResponse.FailureResult("Patient profile not found."));
        }

        var profile = new
        {
            patient.UserId,
            patient.User?.FirstName,
            patient.User?.LastName,
            patient.User?.Email,
            patient.DateOfBirth,
            patient.Gender,
            patient.BloodType,
            patient.Address,
            patient.EmergencyContactName,
            patient.EmergencyContactPhone,
            patient.Allergies,
            patient.ChronicConditions
        };

        return Ok(ApiResponse.SuccessResult(profile, "Profile retrieved successfully."));
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdatePatientProfileDTO updateDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));
        }

        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
        if (patient == null) return NotFound(ApiResponse.FailureResult("Patient profile not found."));

        try {
            if (!string.IsNullOrEmpty(updateDto.BloodType)) patient.BloodType = updateDto.BloodType;
            if (!string.IsNullOrEmpty(updateDto.Address)) patient.Address = updateDto.Address;
            if (!string.IsNullOrEmpty(updateDto.EmergencyContactName)) patient.EmergencyContactName = updateDto.EmergencyContactName;
            if (!string.IsNullOrEmpty(updateDto.EmergencyContactPhone)) patient.EmergencyContactPhone = updateDto.EmergencyContactPhone;
            if (!string.IsNullOrEmpty(updateDto.Allergies)) patient.Allergies = updateDto.Allergies;
            if (!string.IsNullOrEmpty(updateDto.ChronicConditions)) patient.ChronicConditions = updateDto.ChronicConditions;

            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResult((object?)null, "Profile updated successfully."));
        } catch (Exception ex) {
            return BadRequest(ApiResponse.FailureResult("Failed to update profile: " + ex.Message));
        }
    }

    [HttpGet("emergency-settings")]
    public async Task<IActionResult> GetEmergencySettings()
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null) return NotFound(ApiResponse.FailureResult("Patient data not found."));

        var dto = new EmergencySettingsDTO
        {
            BloodType = patient.BloodType,
            Allergies = patient.Allergies,
            CurrentMedications = patient.CurrentMedications,
            ChronicConditions = patient.ChronicConditions,
            EmergencyContactName = patient.EmergencyContactName,
            EmergencyContactPhone = patient.EmergencyContactPhone,
            EmergencyContactRelationship = patient.EmergencyContactRelationship,
            EmergencyNotesToResponders = patient.EmergencyNotesToResponders,
            LastUpdated = patient.EmergencyDataLastUpdated
        };

        return Ok(ApiResponse.SuccessResult(dto, "Emergency settings retrieved successfully."));
    }

    [HttpPut("emergency-settings")]
    public async Task<IActionResult> UpdateEmergencySettings([FromBody] UpdateEmergencySettingsDTO dto)
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null) return NotFound(ApiResponse.FailureResult("Patient data not found."));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers["User-Agent"].ToString() ?? "unknown";

        patient.BloodType = dto.BloodType;
        patient.Allergies = dto.Allergies;
        patient.CurrentMedications = dto.CurrentMedications;
        patient.ChronicConditions = dto.ChronicConditions;
        patient.EmergencyContactName = dto.EmergencyContactName;
        patient.EmergencyContactPhone = dto.EmergencyContactPhone;
        patient.EmergencyContactRelationship = dto.EmergencyContactRelationship;
        patient.EmergencyNotesToResponders = dto.EmergencyNotesToResponders;
        patient.EmergencyDataLastUpdated = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            Guid.Parse(userId!),
            "Emergency settings updated",
            "Emergency medical information updated by patient.",
            ipAddress,
            userAgent,
            "Patient",
            patient.Id.ToString(),
            AuditSeverity.Info);

        return Ok(ApiResponse.SuccessResult((object?)null, "Emergency settings updated successfully."));
    }

    [HttpGet("records")]
    public async Task<IActionResult> GetRecords()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));
        }

        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
        if (patient == null) return NotFound(ApiResponse.FailureResult("Patient profile not found."));

        var result = await _medicalRecordsService.GetPatientRecordsAsync(patient.Id, userId);
        if (!result.Success) return BadRequest(ApiResponse.FailureResult(result.Message));

        return Ok(ApiResponse.SuccessResult(result.Data, "Medical records retrieved successfully."));
    }

    [HttpPost("records/upload")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
    public async Task<IActionResult> UploadRecord([FromForm] SecureMedicalRecordSystem.Core.DTOs.MedicalRecords.UploadMedicalRecordDTO uploadDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
        if (patient == null) return NotFound(ApiResponse.FailureResult("Patient profile not found."));

        var result = await _medicalRecordsService.UploadRecordAsync(patient.Id, uploadDto);
        if (!result.Success) return BadRequest(ApiResponse.FailureResult(result.Message));

        return Ok(ApiResponse.SuccessResult(result.Data, result.Message));
    }

    [HttpGet("records/{recordId}/download")]
    public async Task<IActionResult> DownloadRecord(Guid recordId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var result = await _medicalRecordsService.DownloadRecordAsync(recordId, userId);
        if (!result.Success)
        {
            if (result.Message == "Unauthorized access.") return Forbid();
            return BadRequest(ApiResponse.FailureResult(result.Message));
        }

        return File(result.FileStream!, result.ContentType!, result.FileName);
    }

    [HttpGet("records/{recordId}")]
    public async Task<IActionResult> GetRecord(Guid recordId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var result = await _medicalRecordsService.GetRecordDetailsAsync(recordId, userId);
        if (!result.Success)
        {
            if (result.Message == "Unauthorized access.") return Forbid();
            return BadRequest(ApiResponse.FailureResult(result.Message));
        }

        return Ok(ApiResponse.SuccessResult(result.Data, result.Message));
    }

    [HttpPut("records/{recordId}/metadata")]
    public async Task<IActionResult> UpdateRecordMetadata(Guid recordId, [FromBody] SecureMedicalRecordSystem.Core.DTOs.MedicalRecords.UpdateMedicalRecordMetadataDTO updateDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var result = await _medicalRecordsService.UpdateRecordMetadataAsync(recordId, updateDto, userId);
        if (!result.Success)
        {
            if (result.Message == "Unauthorized access.") return Forbid();
            return BadRequest(ApiResponse.FailureResult(result.Message));
        }

        return Ok(ApiResponse.SuccessResult((object?)null, result.Message));
    }

    [HttpDelete("records/{recordId}")]
    public async Task<IActionResult> DeleteRecord(Guid recordId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var result = await _medicalRecordsService.DeleteRecordAsync(recordId, userId);
        if (!result.Success)
        {
            if (result.Message == "Unauthorized access.") return Forbid();
            return BadRequest(ApiResponse.FailureResult(result.Message));
        }

        return Ok(ApiResponse.SuccessResult((object?)null, result.Message));
    }

    [HttpGet("appointments")]
    public IActionResult GetAppointments()
    {
        // Placeholder for Phase 5
        return Ok(ApiResponse.SuccessResult(new List<object>(), "Appointments retrieved (placeholder)."));
    }

    // =====================
    // FSM TRANSITION ENDPOINTS
    // =====================

    [HttpPost("records/{recordId}/submit")]
    public async Task<IActionResult> Submit(Guid recordId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var result = await _medicalRecordsService.SubmitForReviewAsync(recordId, userId);
        if (!result.Success)
        {
            if (result.Message.Contains("Unauthorized")) return Forbid();
            return BadRequest(ApiResponse.FailureResult(result.Message));
        }
        return Ok(ApiResponse.SuccessResult(result.Data, result.Message));
    }

    [HttpPost("records/{recordId}/archive")]
    public async Task<IActionResult> Archive(Guid recordId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var result = await _medicalRecordsService.ArchiveRecordAsync(recordId, userId);
        if (!result.Success)
        {
            if (result.Message.Contains("Unauthorized")) return Forbid();
            return BadRequest(ApiResponse.FailureResult(result.Message));
        }
        return Ok(ApiResponse.SuccessResult(result.Data, result.Message));
    }

    // =====================
    // DOCTOR ASSIGNMENT ENDPOINTS
    // =====================

    [HttpGet("doctors/departments")]
    public async Task<IActionResult> GetDepartments()
    {
        var departments = await _context.Doctors
            .Include(d => d.Department)
            .Where(d => d.User.IsActive && !d.IsDeleted)
            .Select(d => d.Department.Name)
            .Distinct()
            .ToListAsync();

        return Ok(ApiResponse.SuccessResult(departments, "Departments retrieved successfully."));
    }

    [HttpGet("doctors")]
    public async Task<IActionResult> GetDoctorsByDepartment([FromQuery] string department)
    {
        if (string.IsNullOrWhiteSpace(department))
        {
            return BadRequest(ApiResponse.FailureResult("Department is required."));
        }

        var doctors = await _context.Doctors
            .Include(d => d.User)
            .Include(d => d.Department)
            .Where(d => d.User.IsActive && !d.IsDeleted && d.Department.Name == department)
            .Select(d => new DoctorBasicInfoDTO
            {
                Id = d.Id,
                FirstName = d.User.FirstName,
                LastName = d.User.LastName
            })
            .ToListAsync();

        return Ok(ApiResponse.SuccessResult(doctors, "Doctors retrieved successfully."));
    }

    /// <summary>
    /// Set (or update) the patient's primary doctor.
    /// Only affects smart suggestion ranking — does not change any access control.
    /// </summary>
    [HttpPut("set-primary-doctor")]
    public async Task<IActionResult> SetPrimaryDoctor([FromBody] SetPrimaryDoctorDTO dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
        if (patient == null) return NotFound(ApiResponse.FailureResult("Patient profile not found."));

        // Validate the doctor actually exists and is active
        var doctor = await _context.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == dto.DoctorId && d.User.IsActive && !d.IsDeleted);

        if (doctor == null)
            return BadRequest(ApiResponse.FailureResult("Doctor not found or is no longer available."));

        patient.PrimaryDoctorId = dto.DoctorId;
        patient.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResult(
            new { doctorId = doctor.Id, doctorName = $"Dr. {doctor.User.FirstName} {doctor.User.LastName}" },
            $"Dr. {doctor.User.FirstName} {doctor.User.LastName} set as your primary doctor."));
    }
    private async Task<Patient?> GetCurrentPatientAsync()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId)) 
            return null;

        return await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }
}

