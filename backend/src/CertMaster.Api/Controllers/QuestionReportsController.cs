using CertMaster.Application.Features.Bookmarks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertMaster.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/question-reports")]
public class QuestionReportsController : ControllerBase
{
    private readonly QuestionReportService _reportService;

    public QuestionReportsController(QuestionReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpPost]
    public async Task<ActionResult> Report(ReportQuestionRequest request, CancellationToken ct)
    {
        await _reportService.ReportAsync(request, ct);
        return Ok(new { message = "Thanks — our content team will review this question." });
    }

    [Authorize(Roles = "Administrator")]
    [HttpGet("open")]
    public async Task<ActionResult<List<QuestionReportDto>>> GetOpenReports(CancellationToken ct)
    {
        return Ok(await _reportService.GetOpenReportsAsync(ct));
    }

    [Authorize(Roles = "Administrator")]
    [HttpPost("{id:guid}/resolve")]
    public async Task<ActionResult> Resolve(Guid id, [FromBody] ResolveReportRequest? request, CancellationToken ct)
    {
        await _reportService.ResolveAsync(id, request ?? new ResolveReportRequest(), ct);
        return NoContent();
    }
}
