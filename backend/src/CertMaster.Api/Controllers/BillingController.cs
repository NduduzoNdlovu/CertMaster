using CertMaster.Application.Features.Billing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertMaster.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/billing")]
public class BillingController : ControllerBase
{
    private readonly BillingService _billingService;

    public BillingController(BillingService billingService)
    {
        _billingService = billingService;
    }

    [HttpPost("upgrade")]
    public async Task<ActionResult<UpgradeResultDto>> Upgrade(UpgradeRequest request, CancellationToken ct)
    {
        var result = await _billingService.UpgradeAsync(request, ct);
        return Ok(result);
    }
}
