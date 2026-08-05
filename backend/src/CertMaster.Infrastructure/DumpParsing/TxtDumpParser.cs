using System.Text;
using System.Text.RegularExpressions;
using CertMaster.Application.Features.DumpUpload;

namespace CertMaster.Infrastructure.DumpParsing;

/// <summary>
/// Parses TXT dumps using a simple block format, one question per block,
/// blocks separated by a line containing only "---":
///
///   Topic: Networking Concepts
///   Subtopic: IP Addressing
///   Difficulty: Medium
///   Q: What is the default subnet mask for a /24 network?
///   A) 255.255.0.0
///   B) 255.255.255.0
///   C) 255.0.0.0
///   D) 255.255.255.255
///   Answer: B
///   Explanation: A /24 network uses 24 bits for the network portion...
///   Reference: Network+ Objective 1.4
///   ---
///
/// This same block parser is reused for text extracted from PDFs, since once
/// text is extracted the underlying structure is the same.
/// </summary>
public class TxtDumpParser : IDumpParser
{
    public IReadOnlyCollection<string> SupportedExtensions => new[] { ".txt" };

    private static readonly Regex OptionLine = new(@"^([A-Fa-f])\)\s*(.+)$", RegexOptions.Compiled);

    public async Task<List<ParsedQuestion>> ParseAsync(Stream fileStream, CancellationToken ct)
    {
        using var reader = new StreamReader(fileStream, Encoding.UTF8);
        var text = await reader.ReadToEndAsync();
        return ParseBlocks(text);
    }

    public static List<ParsedQuestion> ParseBlocks(string text)
    {
        var blocks = Regex.Split(text.Replace("\r\n", "\n"), @"^\s*---\s*$", RegexOptions.Multiline)
            .Select(b => b.Trim())
            .Where(b => b.Length > 0)
            .ToList();

        var results = new List<ParsedQuestion>();

        foreach (var block in blocks)
        {
            var pq = new ParsedQuestion();
            var optionTexts = new SortedDictionary<char, string>();
            string? answerLetter = null;

            foreach (var rawLine in block.Split('\n'))
            {
                var line = rawLine.Trim();
                if (line.Length == 0) continue;

                var optionMatch = OptionLine.Match(line);
                if (optionMatch.Success)
                {
                    optionTexts[char.ToUpperInvariant(optionMatch.Groups[1].Value[0])] = optionMatch.Groups[2].Value.Trim();
                    continue;
                }

                var (key, value) = SplitKeyValue(line);
                switch (key)
                {
                    case "topic": pq.Topic = value; break;
                    case "subtopic": pq.Subtopic = value; break;
                    case "difficulty": pq.DifficultyRaw = value; break;
                    case "q":
                    case "question": pq.Prompt = value; break;
                    case "answer":
                    case "correct": answerLetter = value?.Trim().ToUpperInvariant(); break;
                    case "explanation": pq.Explanation = value; break;
                    case "reference": pq.Reference = value; break;
                }
            }

            foreach (var kvp in optionTexts.OrderBy(o => o.Key))
                pq.Options.Add(kvp.Value);

            if (!string.IsNullOrEmpty(answerLetter) && answerLetter.Length == 1)
            {
                var index = answerLetter[0] - 'A';
                if (index >= 0 && index < pq.Options.Count)
                    pq.CorrectOptionIndex = index;
            }

            if (!string.IsNullOrWhiteSpace(pq.Prompt))
                results.Add(pq);
        }

        return results;
    }

    private static (string? key, string? value) SplitKeyValue(string line)
    {
        var separatorIndex = line.IndexOf(':');
        if (separatorIndex <= 0) return (null, null);

        var key = line[..separatorIndex].Trim().ToLowerInvariant();
        var value = line[(separatorIndex + 1)..].Trim();
        return (key, value);
    }
}
