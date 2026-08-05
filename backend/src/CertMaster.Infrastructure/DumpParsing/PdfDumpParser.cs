using CertMaster.Application.Features.DumpUpload;
using UglyToad.PdfPig;

namespace CertMaster.Infrastructure.DumpParsing;

/// <summary>
/// Parses PDF dumps by extracting raw text per page and reusing the same
/// "Q: / A) / Answer: / Explanation:" block format as the TXT parser. PDF text
/// extraction quality varies by how the PDF was produced (native text vs. a
/// scanned image); scanned/image-only PDFs will yield no questions and should
/// be run through OCR before uploading.
/// </summary>
public class PdfDumpParser : IDumpParser
{
    public IReadOnlyCollection<string> SupportedExtensions => new[] { ".pdf" };

    public Task<List<ParsedQuestion>> ParseAsync(Stream fileStream, CancellationToken ct)
    {
        using var document = PdfDocument.Open(fileStream);
        var textBuilder = new System.Text.StringBuilder();

        foreach (var page in document.GetPages())
        {
            ct.ThrowIfCancellationRequested();
            textBuilder.AppendLine(page.Text);
            textBuilder.AppendLine("---"); // treat each page break as a potential question boundary
        }

        return Task.FromResult(TxtDumpParser.ParseBlocks(textBuilder.ToString()));
    }
}
