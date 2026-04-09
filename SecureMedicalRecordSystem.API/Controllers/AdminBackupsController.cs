using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureMedicalRecordSystem.Core.Interfaces;
using System.Security.Claims;

namespace SecureMedicalRecordSystem.API.Controllers.Admin;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/backups")]
public class AdminBackupsController : ControllerBase
{
    private readonly IBackupService _backupService;

    public AdminBackupsController(IBackupService backupService)
    {
        _backupService = backupService;
    }

    [HttpGet("download")]
    public async Task<IActionResult> DownloadSnapshot()
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminId))
        {
            return Unauthorized("Admin ID could not be identified.");
        }

        try
        {
            var snapshotBytes = await _backupService.GenerateDatabaseSnapshotAsync(adminId);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var fileName = $"sajilo_snapshot_{timestamp}.json";

            return File(snapshotBytes, "application/json", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to generate system snapshot.", error = ex.Message });
        }
    }
}
