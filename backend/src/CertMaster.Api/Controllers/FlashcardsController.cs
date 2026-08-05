using CertMaster.Application.Features.Flashcards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertMaster.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/flashcards")]
public class FlashcardsController : ControllerBase
{
    private readonly FlashcardService _flashcardService;

    public FlashcardsController(FlashcardService flashcardService)
    {
        _flashcardService = flashcardService;
    }

    [HttpGet]
    public async Task<ActionResult<List<FlashcardDto>>> GetFlashcards(
        [FromQuery] Guid? certificationId, [FromQuery] string? topic, [FromQuery] int take = 20, CancellationToken ct = default)
    {
        return Ok(await _flashcardService.GetFlashcardsAsync(certificationId, topic, take, ct));
    }
}
