using Microsoft.AspNetCore.Mvc;
using SecureMedicalRecordSystem.Core.Interfaces;

namespace SecureMedicalRecordSystem.API.Controllers;

[ApiController]
[Route("api/system")]
public class SystemController : ControllerBase
{
    private readonly ILocalUrlProvider _urlProvider;

    public SystemController(ILocalUrlProvider urlProvider)
    {
        _urlProvider = urlProvider;
    }

    [HttpGet("info")]
    public IActionResult GetSystemInfo()
    {
        return Ok(new
        {
            Success = true,
            Data = new
            {
                LocalIp = _urlProvider.LocalIpAddress,
                FrontendUrl = _urlProvider.FrontendIpBaseUrl,
                BackendUrl = _urlProvider.BackendBaseUrl,
                Os = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                Time = DateTime.UtcNow
            }
        });
    }
}
