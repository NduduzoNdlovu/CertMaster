using CertMaster.Application.Features.DumpUpload;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace CertMaster.Infrastructure.DumpParsing;

/// <summary>
/// Parses DOCX dumps. Expects the same "Q: / A) / Answer: / Explanation:" block
/// format as the TXT parser (see TxtDumpParser), one question per paragraph group,
/// with a paragraph containing only "---" between questions.
/// </summary>
public class WordDumpParser : IDumpParser
{
    public IReadOnlyCollection<string> SupportedExtensions => new[] { ".docx" };

    public Task<List<ParsedQuestion>> ParseAsync(Stream fileStream, CancellationToken ct)
    {
        using var wordDoc = WordprocessingDocument.Open(fileStream, false);
        var body = wordDoc.MainDocumentPart?.Document?.Body;
        if (body is null) return Task.FromResult(new List<ParsedQuestion>());

        var lines = body.Elements<Paragraph>()
            .Select(p => p.InnerText)
            .ToList();

        var text = string.Join("\n", lines);
        return Task.FromResult(TxtDumpParser.ParseBlocks(text));
    }
}
