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

    [HttpPost("start")]
    public async Task<ActionResult<ExamSessionDto>> Start(StartExamRequest request, CancellationToken ct) => Ok(await _examService.StartOrResumeAsync(request, ct));

    [HttpGet("active")]
    public async Task<ActionResult<ExamSessionDto>> GetActive([FromQuery] Guid certificationId, [FromQuery] string mode, CancellationToken ct)
    {
        var session = await _examService.GetActiveAsync(certificationId, mode, ct);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPut("{attemptId:guid}/answers/{questionId:guid}")]
    public async Task<ActionResult> SaveAnswer(Guid attemptId, Guid questionId, SaveExamAnswerRequest request, CancellationToken ct)
    { await _examService.SaveAnswerAsync(attemptId, questionId, request, ct); return NoContent(); }

    [HttpPost("{attemptId:guid}/complete")]
    public async Task<ActionResult<ExamResultDto>> Complete(Guid attemptId, CancellationToken ct) => Ok(await _examService.CompleteAsync(attemptId, ct));

    [HttpGet("my-results")]
    public async Task<ActionResult<List<ExamAttemptSummaryDto>>> GetMyResults(CancellationToken ct) =>
        Ok(await _examService.GetMyResultsAsync(ct));

    [HttpGet("my-results/{attemptId:guid}")]
    public async Task<ActionResult<ExamAttemptDetailDto>> GetMyResult(Guid attemptId, CancellationToken ct)
    {
        var result = await _examService.GetMyResultAsync(attemptId, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
