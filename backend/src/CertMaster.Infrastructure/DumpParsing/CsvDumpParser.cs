using System.Globalization;
using CertMaster.Application.Features.DumpUpload;
using CsvHelper;
using CsvHelper.Configuration;

namespace CertMaster.Infrastructure.DumpParsing;

/// <summary>
/// Parses CSV dumps with the expected header row:
/// Topic,Subtopic,Difficulty,Prompt,Option1,Option2,Option3,Option4,CorrectOptionIndex,Explanation,Reference
/// CorrectOptionIndex is 1-based in the file (matches how a non-technical content author would fill it in).
/// </summary>
public class CsvDumpParser : IDumpParser
{
    public IReadOnlyCollection<string> SupportedExtensions => new[] { ".csv" };

    public async Task<List<ParsedQuestion>> ParseAsync(Stream fileStream, CancellationToken ct)
    {
        var results = new List<ParsedQuestion>();

        using var reader = new StreamReader(fileStream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            BadDataFound = null,
        });

        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();

            var pq = new ParsedQuestion
            {
                Topic = TryGet(csv, "Topic"),
                Subtopic = TryGet(csv, "Subtopic"),
                DifficultyRaw = TryGet(csv, "Difficulty"),
                Prompt = TryGet(csv, "Prompt"),
                Explanation = TryGet(csv, "Explanation"),
                Reference = TryGet(csv, "Reference"),
            };

            foreach (var col in new[] { "Option1", "Option2", "Option3", "Option4", "Option5", "Option6" })
            {
                var value = TryGet(csv, col);
                if (!string.IsNullOrWhiteSpace(value))
                    pq.Options.Add(value);
            }

            var correctRaw = TryGet(csv, "CorrectOptionIndex");
            if (int.TryParse(correctRaw, out var oneBasedIndex))
                pq.CorrectOptionIndex = oneBasedIndex - 1;

            results.Add(pq);
        }

        return results;
    }

    private static string? TryGet(CsvReader csv, string column)
    {
        return csv.TryGetField<string>(column, out var value) ? value : null;
    }
}
