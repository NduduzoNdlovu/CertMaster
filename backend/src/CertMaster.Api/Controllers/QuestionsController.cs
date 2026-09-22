using CertMaster.Application.Features.Questions;
using CertMaster.Application.Features.Import;
using CertMaster.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertMaster.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/questions")]
public class QuestionsController : ControllerBase
{
    private readonly QuestionService _questionService;
    private readonly ImportService _importService;

    public QuestionsController(QuestionService questionService, ImportService importService)
    {
        _questionService = questionService;
        _importService = importService;
    }

    [HttpGet("practice")]
    public async Task<ActionResult<List<QuestionDto>>> GetPracticeQuestions(
        [FromQuery] Guid? certificationId,
        [FromQuery] string? topic,
        [FromQuery] Difficulty? difficulty,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var questions = await _questionService.GetPracticeQuestionsAsync(certificationId, topic, difficulty, take, ct);
        return Ok(questions);
    }

    [HttpGet("{id:guid}/image")]
    public async Task<ActionResult> GetQuestionImage(Guid id, CancellationToken ct)
    {
        var stream = await _importService.OpenPublishedQuestionImageAsync(id, ct);
        return File(stream, "image/png");
    }
}
