using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureMedicalRecordSystem.Core.Interfaces;
using System.Security.Claims;
using SecureMedicalRecordSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SecureMedicalRecordSystem.Core.Entities;

namespace SecureMedicalRecordSystem.API.Controllers;

[ApiController]
[Route("api/analysis")]
[Authorize] // require authentication — Doctor or Admin roles only
public class AnalysisController : ControllerBase
{
    private readonly IHealthAnalysisService _analysisService;
    private readonly IAnalysisReportService _analysisReportService;
    private readonly ApplicationDbContext _context;

    public AnalysisController(
        IHealthAnalysisService analysisService, 
        IAnalysisReportService analysisReportService,
        ApplicationDbContext context)
    {
        _analysisService = analysisService;
        _analysisReportService = analysisReportService;
        _context = context;
    }

    [HttpGet("patient/{patientId}/trends")]
    public async Task<IActionResult> GetVitalTrends(Guid patientId)
    {
        var effectiveId = await GetEffectivePatientId(patientId);
        if (effectiveId == Guid.Empty) return Forbid();
        
        var result = await _analysisService.GetVitalTrendsAsync(effectiveId);
        return Ok(result);
    }

    [HttpGet("patient/{patientId}/medication-correlation")]
    public async Task<IActionResult> GetMedicationCorrelation(Guid patientId)
    {
        var effectiveId = await GetEffectivePatientId(patientId);
        if (effectiveId == Guid.Empty) return Forbid();
        
        var result = await _analysisService.GetMedicationCorrelationsAsync(effectiveId);
        return Ok(result);
    }

    [HttpGet("patient/{patientId}/abnormality-patterns")]
    public async Task<IActionResult> GetAbnormalityPatterns(Guid patientId)
    {
        var effectiveId = await GetEffectivePatientId(patientId);
        if (effectiveId == Guid.Empty) return Forbid();
        
        var result = await _analysisService.GetAbnormalityPatternsAsync(effectiveId);
        return Ok(result);
    }

    [HttpGet("patient/{patientId}/stability-timeline")]
    public async Task<IActionResult> GetStabilityTimeline(Guid patientId)
    {
        var effectiveId = await GetEffectivePatientId(patientId);
        if (effectiveId == Guid.Empty) return Forbid();
        
        var result = await _analysisService.GetStabilityTimelineAsync(effectiveId);
        return Ok(result);
    }

    [HttpGet("patient/{patientId}/summary")]
    public async Task<IActionResult> GetAnalysisSummary(Guid patientId)
    {
        var effectiveId = await GetEffectivePatientId(patientId);
        if (effectiveId == Guid.Empty) return Forbid();
        
        var result = await _analysisService.GetAnalysisSummaryAsync(effectiveId);
        return Ok(result);
    }

    [HttpPost("patient/{patientId}/report/generate")]
    public async Task<IActionResult> GenerateReport(Guid patientId, [FromQuery] string patientFullName)
    {
        var effectiveId = await GetEffectivePatientId(patientId);
        if (effectiveId == Guid.Empty) return Forbid();
        
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var report = await _analysisReportService.GenerateAndStoreReportAsync(effectiveId, currentUserId, patientFullName);
        return Ok(new { 
            reportId = report.Id, 
            title = report.ReportTitle, 
            generatedAt = report.GeneratedAt 
        });
    }

    [HttpGet("patient/{patientId}/report/list")]
    public async Task<IActionResult> ListReports(Guid patientId)
    {
        var effectiveId = await GetEffectivePatientId(patientId);
        if (effectiveId == Guid.Empty) return Forbid();
        
        var reports = await _analysisReportService.GetReportsForPatientAsync(effectiveId);
        return Ok(reports.Select(r => new {
            r.Id,
            r.ReportTitle,
            r.GeneratedAt,
            r.GeneratedByDoctorId
        }));
    }

    [HttpGet("report/{reportId}/download")]
    public async Task<IActionResult> DownloadReport(Guid reportId)
    {
        var (stream, fileName) = await _analysisReportService.DownloadReportAsync(reportId);
        return File(stream, "application/pdf", fileName);
    }

    private async Task<Guid> GetEffectivePatientId(Guid patientId)
    {
        var currentUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserIdString)) return Guid.Empty;
        
        var currentUserId = Guid.Parse(currentUserIdString);
        var userRole = User.FindFirstValue(ClaimTypes.Role);

        if (userRole == "Patient")
        {
            // If the patient is requesting their own data, they might be using their UserId or PatientId.
            // We verify that the requested ID corresponds to their own record.
            var patient = await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == currentUserId);

            if (patient == null) return Guid.Empty;

            // Allow if patientId is their UserId OR their actual PatientId
            if (patientId == currentUserId || patientId == patient.Id)
            {
                return patient.Id;
            }
            return Guid.Empty;
        }

        // Doctors and Admins can access any patient's data (real patient ID expected)
        return (userRole == "Doctor" || userRole == "Admin") ? patientId : Guid.Empty;
    }
}
