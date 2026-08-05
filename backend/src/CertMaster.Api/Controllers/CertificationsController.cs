using CertMaster.Application.Features.Certifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertMaster.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/certifications")]
public class CertificationsController : ControllerBase
{
    private readonly CertificationService _certificationService;

    public CertificationsController(CertificationService certificationService)
    {
        _certificationService = certificationService;
    }

    [HttpGet]
    public async Task<ActionResult<List<CertificationDto>>> GetAll(CancellationToken ct)
    {
        return Ok(await _certificationService.GetActiveCertificationsAsync(ct));
    }
}
