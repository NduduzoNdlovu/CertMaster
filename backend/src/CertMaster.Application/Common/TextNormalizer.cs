using System.Text.RegularExpressions;

namespace CertMaster.Application.Common;

/// <summary>
/// Normalizes raw extracted text before it's staged for review, so admins are reviewing
/// clean content rather than PDF/OCR artifacts (stray whitespace, curly quotes, bullet
/// characters, etc). Deliberately conservative — it never rewrites meaning, only tidies
/// formatting.
/// </summary>
public static class TextNormalizer
{
    public static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var text = value.Trim();

        // Collapse runs of whitespace (including embedded newlines from PDF line wraps)
        // into single spaces — extracted PDF text frequently wraps mid-sentence.
        text = Regex.Replace(text, @"\s+", " ");

        // Normalize curly quotes and dashes to their plain equivalents for consistency.
        text = text
            .Replace('\u2018', '\'').Replace('\u2019', '\'')
            .Replace('\u201C', '"').Replace('\u201D', '"')
            .Replace('\u2013', '-').Replace('\u2014', '-');

        // Strip common leading bullet/option markers left over from source formatting,
        // e.g. "- ", "* ", "• " at the start of a line.
        text = Regex.Replace(text, @"^[\-\*\u2022]\s+", "");

        return text.Trim();
    }

    public static string NormalizeTopic(string? value)
    {
        var normalized = NormalizeText(value);
        return string.IsNullOrEmpty(normalized) ? "General" : normalized;
    }
}
