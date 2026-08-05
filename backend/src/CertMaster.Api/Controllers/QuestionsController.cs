using CertMaster.Application.Features.Questions;
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

    public QuestionsController(QuestionService questionService)
    {
        _questionService = questionService;
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
}
