using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureMedicalRecordSystem.Core.DTOs.HealthRecords;
using SecureMedicalRecordSystem.Core.Interfaces;
using System.Security.Claims;
using SecureMedicalRecordSystem.Core.Enums;

namespace SecureMedicalRecordSystem.API.Controllers;

[ApiController]
[Route("api/templates")]
[Authorize(Policy = "DoctorPolicy")]
public class TemplateController : ControllerBase
{
    private readonly ITemplateService _templateService;

    public TemplateController(ITemplateService templateService)
    {
        _templateService = templateService;
    }

    private Guid GetDoctorId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdString, out var userId) ? userId : Guid.Empty;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateTemplateDTO request)
    {
        var doctorId = GetDoctorId();
        var (success, message, data) = await _templateService.CreateTemplateAsync(request, doctorId);
        
        if (!success) return BadRequest(new { Success = false, Message = message });
        return Ok(new { Success = true, Message = message, Data = data });
    }

    [HttpPost("from-record/{recordId}")]
    public async Task<IActionResult> CreateTemplateFromRecord(Guid recordId, [FromBody] CreateTemplateFromRecordRequest request)
    {
        var doctorId = GetDoctorId();
        var (success, message, data) = await _templateService.CreateTemplateFromRecordAsync(
            recordId, 
            request.TemplateName, 
            request.Description ?? string.Empty, 
            request.Visibility, 
            doctorId);
        
        if (!success) return BadRequest(new { Success = false, Message = message });
        return Ok(new { Success = true, Message = message, Data = data });
    }

    [HttpGet("my-templates")]
    public async Task<IActionResult> GetDoctorTemplates([FromQuery] bool includeShared = true)
    {
        var doctorId = GetDoctorId();
        var (success, message, data) = await _templateService.GetDoctorTemplatesAsync(doctorId, includeShared);
        
        if (!success) return BadRequest(new { Success = false, Message = message });
        return Ok(new { Success = true, Message = message, Data = data });
    }

    [HttpPost("suggest")]
    public async Task<IActionResult> SuggestTemplates([FromBody] SuggestTemplateRequest request)
    {
        var doctorId = GetDoctorId();
        var (success, message, data) = await _templateService.SuggestTemplatesAsync(request.ChiefComplaint, doctorId);
        
        if (!success) return BadRequest(new { Success = false, Message = message });
        return Ok(new { Success = true, Message = message, Data = data });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTemplate(Guid id, [FromBody] UpdateTemplateDTO request)
    {
        var doctorId = GetDoctorId();
        var (success, message, data) = await _templateService.UpdateTemplateAsync(id, request, doctorId);
        
        if (!success) return BadRequest(new { Success = false, Message = message });
        return Ok(new { Success = true, Message = message, Data = data });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTemplate(Guid id)
    {
        var doctorId = GetDoctorId();
        var (success, message) = await _templateService.DeleteTemplateAsync(id, doctorId);
        
        if (!success) return BadRequest(new { Success = false, Message = message });
        return Ok(new { Success = true, Message = message });
    }

    [HttpPost("{id}/fork")]
    public async Task<IActionResult> ForkTemplate(Guid id, [FromBody] ForkTemplateRequest request)
    {
        var doctorId = GetDoctorId();
        var (success, message, data) = await _templateService.ForkTemplateAsync(id, request.NewTemplateName, doctorId);
        
        if (!success) return BadRequest(new { Success = false, Message = message });
        return Ok(new { Success = true, Message = message, Data = data });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTemplateDetails(Guid id)
    {
        var doctorId = GetDoctorId();
        var (success, message, data) = await _templateService.GetTemplateAsync(id, doctorId);
        
        if (!success) return NotFound(new { Message = message });
        return Ok(new { Success = true, Message = message, Data = data });
    }

    [HttpGet("{id}/usage-stats")]
    public async Task<IActionResult> GetTemplateUsageStats(Guid id)
    {
        var doctorId = GetDoctorId();
        var (success, message, data) = await _templateService.GetTemplateUsageStatsAsync(id, doctorId);
        
        if (!success) return BadRequest(new { Success = false, Message = message });
        return Ok(new { Success = true, Message = message, Data = data });
    }
}

public class CreateTemplateFromRecordRequest
{
    public string TemplateName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public VisibilityLevel Visibility { get; set; }
}

public class SuggestTemplateRequest
{
    public string ChiefComplaint { get; set; } = string.Empty;
    public string? PatientId { get; set; }
}

public class ForkTemplateRequest
{
    public string NewTemplateName { get; set; } = string.Empty;
}
