using CertMaster.Application.Features.Exams;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertMaster.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/exams")]
public class ExamsController : ControllerBase
{
    private readonly ExamService _examService;

    public ExamsController(ExamService examService)
    {
        _examService = examService;
    }

    [HttpPost("submit")]
    public async Task<ActionResult<ExamResultDto>> Submit(SubmitExamRequest request, CancellationToken ct)
    {
        var result = await _examService.SubmitAsync(request, ct);
        return Ok(result);
    }
}
