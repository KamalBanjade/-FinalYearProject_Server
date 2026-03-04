using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using SecureMedicalRecordSystem.API.Authorization;
using SecureMedicalRecordSystem.Core.DTOs;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Infrastructure.Data;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Core.DTOs.MedicalRecords;
using System.Security.Claims;

namespace SecureMedicalRecordSystem.API.Controllers;

[ApiController]
[Route("api/doctor")]
[Authorize(Policy = "DoctorPolicy")]
public class DoctorController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    private readonly IMedicalRecordsService _medicalRecordsService;

    public DoctorController(
        ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager,
        IMedicalRecordsService medicalRecordsService)
    {
        _context = context;
        _userManager = userManager;
        _medicalRecordsService = medicalRecordsService;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));
        }

        var doctor = await _context.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor == null)
        {
            return NotFound(ApiResponse.FailureResult("Doctor profile not found."));
        }

        var profile = new
        {
            doctor.UserId,
            doctor.User?.FirstName,
            doctor.User?.LastName,
            doctor.User?.Email,
            doctor.NMCLicense,
            doctor.Department,
            doctor.Specialization,
            doctor.HospitalAffiliation,
            doctor.ContactNumber
        };

        return Ok(ApiResponse.SuccessResult(profile, "Profile retrieved successfully."));
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateDoctorProfileDTO updateDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.FailureResult("Invalid session."));
        }

        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
        if (doctor == null) return NotFound(ApiResponse.FailureResult("Doctor profile not found."));

        try {
            if (!string.IsNullOrEmpty(updateDto.Department))
            {
                var deptEntity = await _context.Departments.FirstOrDefaultAsync(d => d.Name == updateDto.Department);
                if (deptEntity == null)
                {
                    deptEntity = new Department { Name = updateDto.Department, Description = "Auto-generated" };
                    _context.Departments.Add(deptEntity);
                    await _context.SaveChangesAsync();
                }
                doctor.DepartmentId = deptEntity.Id;
            }
            if (!string.IsNullOrEmpty(updateDto.Specialization)) doctor.Specialization = updateDto.Specialization;
            if (!string.IsNullOrEmpty(updateDto.HospitalAffiliation)) doctor.HospitalAffiliation = updateDto.HospitalAffiliation;
            if (!string.IsNullOrEmpty(updateDto.ContactNumber)) doctor.ContactNumber = updateDto.ContactNumber;

            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResult((object?)null, "Profile updated successfully."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.FailureResult($"Update failed: {ex.Message}"));
        }
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

}
