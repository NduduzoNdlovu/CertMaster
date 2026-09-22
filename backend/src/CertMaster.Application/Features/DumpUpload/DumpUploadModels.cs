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
    // Multiple-response questions ("Select two", "Choose all") are common in
    // CompTIA material, so parsers must not collapse the answer to one option.
    public List<int> CorrectOptionIndexes { get; set; } = new(); // 0-based
    public int CorrectOptionIndex
    {
        get => CorrectOptionIndexes.FirstOrDefault(-1);
        set
        {
            CorrectOptionIndexes.Clear();
            if (value >= 0) CorrectOptionIndexes.Add(value);
        }
    }
    public string? Explanation { get; set; }
    public string? Reference { get; set; }
    public int? SourcePageStart { get; set; }
    public int? SourcePageEnd { get; set; }
    public string QuestionType { get; set; } = "Choice";
    public bool RequiresManualReview { get; set; }

    public List<string> ValidationErrors { get; } = new();

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Prompt) &&
        Options.Count(o => !string.IsNullOrWhiteSpace(o)) >= 2 &&
        CorrectOptionIndexes.Count > 0 &&
        CorrectOptionIndexes.All(i => i >= 0 && i < Options.Count);

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
