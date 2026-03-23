using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureMedicalRecordSystem.Core.DTOs;
using SecureMedicalRecordSystem.Core.DTOs.LabUnits;
using SecureMedicalRecordSystem.Core.Interfaces;
using System.Security.Claims;

namespace SecureMedicalRecordSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LabUnitsController : ControllerBase
{
    private readonly ILabUnitsService _labUnitsService;

    public LabUnitsController(ILabUnitsService labUnitsService)
    {
        _labUnitsService = labUnitsService;
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<List<LabUnitDTO>>>> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Ok(ApiResponse<List<LabUnitDTO>>.SuccessResult(new List<LabUnitDTO>(), "Query is empty"));

        var results = await _labUnitsService.SearchLabUnitsAsync(query);
        return Ok(ApiResponse<List<LabUnitDTO>>.SuccessResult(results, "Search results retrieved"));
    }

    [Authorize(Roles = "Doctor")]
    [HttpPost("custom")]
    public async Task<ActionResult<ApiResponse<LabUnitDTO>>> CreateCustom([FromBody] CreateCustomLabUnitDTO request)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            return Unauthorized(ApiResponse<LabUnitDTO>.FailureResult("Invalid user ID"));

        var result = await _labUnitsService.CreateCustomLabUnitAsync(request, userId);

        if (!result.Success)
            return BadRequest(ApiResponse<LabUnitDTO>.FailureResult(result.Message));

        return Ok(ApiResponse<LabUnitDTO>.SuccessResult(result.Data, result.Message));
    }
}
