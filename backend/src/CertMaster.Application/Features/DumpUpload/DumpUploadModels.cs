using CertMaster.Domain.Enums;

namespace CertMaster.Application.Features.DumpUpload;

/// <summary>
/// A single question as extracted from an uploaded dump file, before it is
/// validated and persisted. Parsers (CSV, Excel, Word, TXT, PDF) all produce
/// this same shape regardless of source format.
/// </summary>
public class ParsedQuestion
{
    public string? Topic { get; set; }
    public string? Subtopic { get; set; }
    public string? DifficultyRaw { get; set; }
    public string? Prompt { get; set; }
    public List<string> Options { get; set; } = new();
    public int CorrectOptionIndex { get; set; } = -1; // 0-based
    public string? Explanation { get; set; }
    public string? Reference { get; set; }

    public List<string> ValidationErrors { get; } = new();

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Prompt) &&
        Options.Count(o => !string.IsNullOrWhiteSpace(o)) >= 2 &&
        CorrectOptionIndex >= 0 &&
        CorrectOptionIndex < Options.Count;

    public Difficulty ResolveDifficulty()
    {
        return DifficultyRaw?.Trim().ToLowerInvariant() switch
        {
            "easy" => Difficulty.Easy,
            "hard" => Difficulty.Hard,
            _ => Difficulty.Medium,
        };
    }
}

public interface IDumpParser
{
    /// <summary>File extensions this parser handles, e.g. ".csv", ".txt".</summary>
    IReadOnlyCollection<string> SupportedExtensions { get; }

    Task<List<ParsedQuestion>> ParseAsync(Stream fileStream, CancellationToken ct);
}

public record DumpUploadResultDto(
    Guid VersionId,
    string VersionLabel,
    int QuestionsExtracted,
    int QuestionsPublished,
    int QuestionsFlagged,
    int DuplicatesSkipped,
    List<string> Warnings);
