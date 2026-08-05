using CertMaster.Application.Features.Import;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertMaster.Api.Controllers;

[ApiController]
[Authorize(Roles = "Administrator")]
[Route("api/admin")]
public class ImportController : ControllerBase
{
    private const long MaxFileSizeBytes = 50 * 1024 * 1024;

    private readonly ImportService _importService;

    public ImportController(ImportService importService)
    {
        _importService = importService;
    }

    [HttpGet("certifications/{certificationId:guid}/versions")]
    public async Task<ActionResult<List<QuestionBankVersionDto>>> GetVersions(Guid certificationId, CancellationToken ct)
    {
        return Ok(await _importService.GetVersionsAsync(certificationId, ct));
    }

    [HttpPost("imports")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<ActionResult<ImportJobDto>> StartImport(
        [FromForm] Guid certificationId,
        [FromForm] Guid? questionBankVersionId,
        [FromForm] string? newVersionLabel,
        [FromForm] IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file was uploaded." });

        await using var stream = file.OpenReadStream();
        var request = new StartImportRequest(certificationId, questionBankVersionId, newVersionLabel);
        var result = await _importService.StartImportAsync(request, stream, file.FileName, ct);
        return Ok(result);
    }

    [HttpGet("imports")]
    public async Task<ActionResult<List<ImportJobDto>>> GetImportJobs([FromQuery] Guid? certificationId, CancellationToken ct)
    {
        return Ok(await _importService.GetImportJobsAsync(certificationId, ct));
    }

    [HttpGet("imports/{id:guid}")]
    public async Task<ActionResult<ImportJobDetailDto>> GetImportJobDetail(Guid id, CancellationToken ct)
    {
        return Ok(await _importService.GetImportJobDetailAsync(id, ct));
    }

    [HttpGet("imports/images/{imageId:guid}")]
    public async Task<ActionResult> GetImage(Guid imageId, CancellationToken ct)
    {
        var stream = await _importService.OpenImportJobImageAsync(imageId, ct);
        return File(stream, "image/png");
    }

    [HttpPut("imports/questions/{id:guid}")]
    public async Task<ActionResult<ImportedQuestionDto>> UpdateImportedQuestion(Guid id, UpdateImportedQuestionRequest request, CancellationToken ct)
    {
        return Ok(await _importService.UpdateImportedQuestionAsync(id, request, ct));
    }

    [HttpPost("imports/questions/approve")]
    public async Task<ActionResult> Approve(ApproveQuestionsRequest request, CancellationToken ct)
    {
        await _importService.ApproveAsync(request, ct);
        return NoContent();
    }

    [HttpPost("imports/questions/reject")]
    public async Task<ActionResult> Reject(RejectQuestionsRequest request, CancellationToken ct)
    {
        await _importService.RejectAsync(request, ct);
        return NoContent();
    }

    [HttpPost("versions/{versionId:guid}/publish")]
    public async Task<ActionResult<PublishVersionResultDto>> PublishVersion(Guid versionId, CancellationToken ct)
    {
        return Ok(await _importService.PublishVersionAsync(versionId, ct));
    }
}
