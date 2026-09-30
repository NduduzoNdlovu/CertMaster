using CertMaster.Application.Features.DumpUpload;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using System.Text.RegularExpressions;
using System.Text;

namespace CertMaster.Infrastructure.DumpParsing;

/// <summary>
/// Parses common certification-dump layouts. It deliberately accepts several
/// question, option and answer styles instead of relying on one vendor format.
/// </summary>
public class PdfDumpParser : IDumpParser
{
    public IReadOnlyCollection<string> SupportedExtensions => new[] { ".pdf" };

    public Task<List<ParsedQuestion>> ParseAsync(Stream fileStream, CancellationToken ct)
    {
        using var document = PdfDocument.Open(fileStream);
        var pages = new List<(int Number, string Text)>();

        foreach (var page in document.GetPages())
        {
            ct.ThrowIfCancellationRequested();
            pages.Add((page.Number, SelectBestPageText(page)));
        }

        var questions = ParsePages(pages);
        if (questions.Count == 0)
        {
            var preview = Regex.Replace(string.Join(" ", pages.Take(2).Select(p => p.Text)), @"\s+", " ").Trim();
            if (preview.Length > 350) preview = preview[..350];
            throw new InvalidOperationException(
                $"PDF text was read, but no question headings were recognised. Extraction preview: {preview}");
        }

        return Task.FromResult(questions);
    }

    /// <summary>
    /// PdfPig's high-level extractors can produce very different output for PDFs
    /// that use custom-positioned fonts. Score three representations and retain the
    /// one that exposes the largest number of question/answer/option markers.
    /// </summary>
    private static string SelectBestPageText(UglyToad.PdfPig.Content.Page page)
    {
        var candidates = new List<string>();
        try { candidates.Add(ContentOrderTextExtractor.GetText(page)); } catch { }
        try { candidates.Add(page.Text); } catch { }
        try { candidates.Add(ReconstructFromLetters(page)); } catch { }

        return candidates
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(NormalizeStructuralSpacing)
            .OrderByDescending(ExtractionScore)
            .ThenByDescending(s => s.Count(c => c == '\n'))
            .FirstOrDefault() ?? string.Empty;
    }

    private static int ExtractionScore(string text)
    {
        var questionMarkers = Regex.Matches(text, @"(?im)^\s*Question\s*(?:#\s*)?:?\s*\d+").Count;
        var answerMarkers = Regex.Matches(text, @"(?im)^\s*(?:Correct\s+)?Answer(?:\(s\))?\s*:").Count;
        var optionMarkers = Regex.Matches(text, @"(?im)^\s*(?:[•●▪◦]\s*)?[A-F]\s*[\)\.:]").Count;
        return questionMarkers * 100 + answerMarkers * 20 + optionMarkers;
    }

    private static string NormalizeStructuralSpacing(string text)
    {
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');
        foreach (var keyword in new[] { "Question", "Answer", "Correct", "Explanation", "Reference", "SIMULATION", "INSTRUCTIONS" })
        {
            var spaced = string.Join(@"\s*", keyword.Select(c => Regex.Escape(c.ToString())));
            text = Regex.Replace(text, $@"(?i)\b{spaced}\b", keyword);
        }

        // Repair structural punctuation commonly separated by positioned glyphs:
        // "Question # : 12", "A . option" and "Answer : B".
        text = Regex.Replace(text, @"(?im)^(\s*Question)\s*#\s*:\s*", "$1 #: ");
        text = Regex.Replace(text, @"(?im)^(\s*(?:Correct\s+)?Answer(?:\(s\))?)\s*:\s*", "$1: ");
        text = Regex.Replace(text, @"(?im)^(\s*[•●▪◦]?\s*[A-F])\s*([\)\.:])\s*", "$1$2 ");
        return text;
    }

    /// <summary>
    /// Rebuild lines and word spaces from letter coordinates. This is the fallback
    /// for PDFs where each glyph is individually positioned and high-level text is
    /// returned as "Q u e s t i o n" or as one unbroken stream.
    /// </summary>
    private static string ReconstructFromLetters(UglyToad.PdfPig.Content.Page page)
    {
        var letters = page.Letters
            .Where(l => !string.IsNullOrEmpty(l.Value))
            .ToList();
        if (letters.Count == 0) return string.Empty;

        var heights = letters.Select(l => Math.Max(1d, l.GlyphRectangle.Height)).OrderBy(h => h).ToList();
        var medianHeight = heights[heights.Count / 2];
        var lineTolerance = Math.Max(1.5d, medianHeight * 0.38d);
        var lines = new List<List<UglyToad.PdfPig.Content.Letter>>();

        foreach (var letter in letters.OrderByDescending(l => l.GlyphRectangle.Bottom).ThenBy(l => l.GlyphRectangle.Left))
        {
            var line = lines.FirstOrDefault(existing =>
                Math.Abs(existing.Average(l => l.GlyphRectangle.Bottom) - letter.GlyphRectangle.Bottom) <= lineTolerance);
            if (line is null)
            {
                line = new List<UglyToad.PdfPig.Content.Letter>();
                lines.Add(line);
            }
            line.Add(letter);
        }

        var output = new StringBuilder();
        foreach (var line in lines.OrderByDescending(l => l.Average(x => x.GlyphRectangle.Bottom)))
        {
            var ordered = line.OrderBy(l => l.GlyphRectangle.Left).ToList();
            var visibleWidths = ordered.Select(l => l.GlyphRectangle.Width).Where(w => w > 0).OrderBy(w => w).ToList();
            var typicalWidth = visibleWidths.Count == 0 ? 4d : visibleWidths[visibleWidths.Count / 2];
            UglyToad.PdfPig.Content.Letter? previous = null;

            foreach (var letter in ordered)
            {
                if (previous is not null)
                {
                    var gap = letter.GlyphRectangle.Left - previous.GlyphRectangle.Right;
                    if (gap > Math.Max(1.2d, typicalWidth * 0.45d) &&
                        output.Length > 0 && !char.IsWhiteSpace(output[^1]))
                        output.Append(' ');
                }
                output.Append(letter.Value);
                previous = letter;
            }
            output.AppendLine();
        }

        return output.ToString();
    }

    internal static List<ParsedQuestion> ParsePages(IReadOnlyList<(int Number, string Text)> pages)
    {
        const string pageMarker = "\n\u000cPAGE:{0}\u000c\n";
        var combined = string.Concat(pages.Select(p => string.Format(pageMarker, p.Number) + p.Text));
        var header = new Regex(@"(?im)^\s*Question\s*(?:#\s*)?:?\s*(\d+)\b[^\r\n]*", RegexOptions.Compiled);
        var matches = header.Matches(combined);
        var results = new List<ParsedQuestion>();

        for (var i = 0; i < matches.Count; i++)
        {
            var start = matches[i].Index;
            var end = i + 1 < matches.Count ? matches[i + 1].Index : combined.Length;
            var block = combined[start..end];
            var parsed = ParseQuestionBlock(block);
            if (parsed is null) continue;

            parsed.SourcePageStart = PageAt(combined, start);
            parsed.SourcePageEnd = PageAt(combined, Math.Max(start, end - 1));
            results.Add(parsed);
        }

        return results;
    }

    private static ParsedQuestion? ParseQuestionBlock(string block)
    {
        block = Regex.Replace(block, @"\n\u000cPAGE:\d+\u000c\n", "\n");
        var lines = block.Replace("\r\n", "\n").Split('\n');
        if (lines.Length == 0) return null;

        var pq = new ParsedQuestion();
        var category = Regex.Match(lines[0], @"\[([^\]]+)\]\s*$");
        if (category.Success) pq.Topic = category.Groups[1].Value.Trim();

        var optionRegex = new Regex(@"^\s*(?:[•●▪◦]\s*)?([A-Fa-f])[\)\.:]\s*(.*)$");
        var answerRegex = new Regex(@"^\s*(?:Correct\s+)?Answer(?:\(s\))?\s*:\s*(.*)$", RegexOptions.IgnoreCase);
        var prompt = new List<string>();
        var options = new SortedDictionary<char, List<string>>();
        var explanation = new List<string>();
        char? activeOption = null;
        var inExplanation = false;
        var waitingForAnswer = false;
        string answerText = string.Empty;

        for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex].Trim();
            if (line.Length == 0) continue;
            if (line.Equals("Verified Answer", StringComparison.OrdinalIgnoreCase)) { waitingForAnswer = false; continue; }

            if (Regex.IsMatch(line, @"^Explanation\s*:?.*$", RegexOptions.IgnoreCase))
            {
                inExplanation = true;
                activeOption = null;
                waitingForAnswer = false;
                var inline = Regex.Replace(line, @"^Explanation\s*:?\s*", string.Empty, RegexOptions.IgnoreCase);
                if (inline.Length > 0) explanation.Add(inline);
                continue;
            }

            var answer = answerRegex.Match(line);
            if (answer.Success)
            {
                answerText = answer.Groups[1].Value.Trim();
                waitingForAnswer = answerText.Length == 0;
                activeOption = null;
                continue;
            }

            if (waitingForAnswer && Regex.IsMatch(line, @"^(?:[A-Fa-f](?:\s*(?:,|and|&)?\s*))+\s*$"))
            {
                answerText = line;
                waitingForAnswer = false;
                continue;
            }

            if (inExplanation) { explanation.Add(line); continue; }

            var option = optionRegex.Match(line);
            if (option.Success)
            {
                activeOption = char.ToUpperInvariant(option.Groups[1].Value[0]);
                options[activeOption.Value] = new List<string>();
                if (option.Groups[2].Value.Trim().Length > 0) options[activeOption.Value].Add(option.Groups[2].Value.Trim());
                continue;
            }

            if (activeOption is not null)
                options[activeOption.Value].Add(line);
            else if (!Regex.IsMatch(line, @"^(Updated Dumps|Verified Questions|CertyIQ|Premium exam material|\d+ of \d+)", RegexOptions.IgnoreCase))
                prompt.Add(line);
        }

        pq.Prompt = string.Join(" ", prompt).Trim();
        foreach (var entry in options)
            pq.Options.Add(string.Join(" ", entry.Value).Trim());
        pq.Explanation = string.Join(" ", explanation).Trim();

        foreach (Match letter in Regex.Matches(answerText.ToUpperInvariant(), @"\b[A-F]\b"))
        {
            var index = letter.Value[0] - 'A';
            if (index >= 0 && index < pq.Options.Count && !pq.CorrectOptionIndexes.Contains(index))
                pq.CorrectOptionIndexes.Add(index);
        }

        var simulationText = pq.Prompt + " " + string.Join(" ", pq.Options);
        if (Regex.IsMatch(simulationText, @"\b(SIMULATION|INSTRUCTIONS|drag and drop|click on|enter commands|terminal)\b", RegexOptions.IgnoreCase))
        {
            pq.QuestionType = "Simulation";
            pq.RequiresManualReview = true;
        }
        else if (pq.CorrectOptionIndexes.Count > 1 || Regex.IsMatch(pq.Prompt, @"\bselect\s+(two|three|all)\b", RegexOptions.IgnoreCase))
        {
            pq.QuestionType = "MultipleResponse";
        }

        return string.IsNullOrWhiteSpace(pq.Prompt) ? null : pq;
    }

    private static int? PageAt(string combined, int position)
    {
        var prefix = combined[..Math.Min(position, combined.Length)];
        var matches = Regex.Matches(prefix, @"\u000cPAGE:(\d+)\u000c");
        return matches.Count == 0 ? null : int.Parse(matches[^1].Groups[1].Value);
    }
}
