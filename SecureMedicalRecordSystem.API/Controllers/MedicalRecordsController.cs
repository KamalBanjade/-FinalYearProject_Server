using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureMedicalRecordSystem.Core.DTOs;
using SecureMedicalRecordSystem.Core.DTOs.MedicalRecords;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Data;
using System.Security.Claims;

namespace SecureMedicalRecordSystem.API.Controllers;

[ApiController]
[Route("api/medical-records")]
[Authorize]
public class MedicalRecordsController : ControllerBase
{
    private readonly IMedicalRecordsService _medicalRecordsService;
    private readonly ILogger<MedicalRecordsController> _logger;
    private readonly ApplicationDbContext _patientContext;

    public MedicalRecordsController(
        IMedicalRecordsService medicalRecordsService,
        ILogger<MedicalRecordsController> logger,
        ApplicationDbContext patientContext)
    {
        _medicalRecordsService = medicalRecordsService;
        _logger = logger;
        _patientContext = patientContext;
    }

    [HttpPost("upload/{patientId}")]
    [Authorize(Policy = "PatientPolicy")]
    public async Task<IActionResult> Upload(Guid patientId, [FromForm] UploadMedicalRecordDTO uploadDto)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var result = await _medicalRecordsService.UploadRecordAsync(patientId, uploadDto);
        if (!result.Success) return BadRequest(ApiResponse.FailureResult(result.Message));

        return Ok(ApiResponse.SuccessResult(result.Data, result.Message));
    }

    // Removed legacy buffered endpoint


    [HttpGet("view/{recordId}")]
    public async Task<IActionResult> View(Guid recordId)
    {
        _logger.LogInformation("[STREAMING MODE ACTIVE] Patient viewing record {RecordId} inline", recordId);
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var result = await _medicalRecordsService.StreamDownloadRecordAsync(recordId, userId);

        if (!result.Success)
        {
            if (result.Message == "Unauthorized access.") return Forbid();
            return BadRequest(ApiResponse.FailureResult(result.Message));
        }

        // Medical data: prevent any browser/CDN caching of decrypted content
        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Content-Disposition"] = $"inline; filename=\"{result.FileName}\"";

        // enableRangeProcessing: true → browser PDF viewer can request pages on demand
        return File(result.FileStream!, result.ContentType!, enableRangeProcessing: true);
    }

    [HttpGet("stream-download/{recordId}")]
    public async Task<IActionResult> StreamDownload(Guid recordId)
    {
        _logger.LogInformation("[STREAMING MODE ACTIVE] Patient downloading record {RecordId} as attachment", recordId);
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var result = await _medicalRecordsService.StreamDownloadRecordAsync(recordId, userId);

        if (!result.Success)
        {
            if (result.Message == "Unauthorized access.") return Forbid();
            return BadRequest(ApiResponse.FailureResult(result.Message));
        }

        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        // attachment → browser opens Save As dialog
        return File(result.FileStream!, result.ContentType!, result.FileName, enableRangeProcessing: false);
    }

    [HttpGet("patient/{patientId}")]
    public async Task<IActionResult> GetPatientRecords(Guid patientId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var result = await _medicalRecordsService.GetPatientRecordsAsync(patientId, userId);
        
        if (!result.Success) 
        {
            if (result.Message == "Unauthorized access.") return Forbid();
            return BadRequest(ApiResponse.FailureResult(result.Message));
        }

        return Ok(ApiResponse.SuccessResult(result.Data, result.Message));
    }

    [HttpGet("{recordId}")]
    public async Task<IActionResult> GetRecordDetails(Guid recordId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var result = await _medicalRecordsService.GetRecordDetailsAsync(recordId, userId);
        
        if (!result.Success) 
        {
            if (result.Message == "Unauthorized access.") return Forbid();
            return BadRequest(ApiResponse.FailureResult(result.Message));
        }

        return Ok(ApiResponse.SuccessResult(result.Data, result.Message));
    }

    [HttpPatch("{recordId}/metadata")]
    public async Task<IActionResult> UpdateMetadata(Guid recordId, [FromBody] UpdateMedicalRecordMetadataDTO updateDto)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var result = await _medicalRecordsService.UpdateRecordMetadataAsync(recordId, updateDto, userId);
        
        if (!result.Success) 
        {
            if (result.Message == "Unauthorized access.") return Forbid();
            return BadRequest(ApiResponse.FailureResult(result.Message));
        }

        return Ok(ApiResponse.SuccessResult((object?)null, result.Message));
    }

    [HttpDelete("{recordId}")]
    public async Task<IActionResult> Delete(Guid recordId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var result = await _medicalRecordsService.DeleteRecordAsync(recordId, userId);
        
        if (!result.Success) 
        {
            if (result.Message == "Unauthorized access.") return Forbid();
            return BadRequest(ApiResponse.FailureResult(result.Message));
        }

        return Ok(ApiResponse.SuccessResult((object?)null, result.Message));
    }

    [HttpPost("{recordId}/verify-signature")]
    public async Task<IActionResult> VerifySignature(Guid recordId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        var result = await _medicalRecordsService.VerifyRecordSignatureAsync(recordId, userId);
        if (!result.Success) return BadRequest(ApiResponse.FailureResult(result.Message));

        return Ok(ApiResponse.SuccessResult(result.Data, result.Message));
    }

    [HttpGet("smart-doctor-suggestions")]
    [Authorize(Policy = "PatientPolicy")]
    public async Task<IActionResult> GetSmartDoctorSuggestions()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized(ApiResponse.FailureResult("Invalid session."));

        // Resolve patient entity ID from the user ID stored in JWT
        var patient = await _patientContext.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
        if (patient == null) return NotFound(ApiResponse.FailureResult("Patient profile not found."));

        var result = await _medicalRecordsService.GetSmartDoctorSuggestionsAsync(patient.Id);
        if (!result.Success) return BadRequest(ApiResponse.FailureResult(result.Message));

        return Ok(ApiResponse.SuccessResult(result.Data, result.Message));
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
