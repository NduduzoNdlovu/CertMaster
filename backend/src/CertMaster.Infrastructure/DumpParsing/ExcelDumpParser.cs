using CertMaster.Application.Features.DumpUpload;
using ClosedXML.Excel;

namespace CertMaster.Infrastructure.DumpParsing;

/// <summary>
/// Parses XLSX dumps. Expects the same columns as the CSV format (in the header row
/// of the first worksheet, any order): Topic, Subtopic, Difficulty, Prompt, Option1..OptionN,
/// CorrectOptionIndex (1-based), Explanation, Reference.
/// </summary>
public class ExcelDumpParser : IDumpParser
{
    public IReadOnlyCollection<string> SupportedExtensions => new[] { ".xlsx" };

    public Task<List<ParsedQuestion>> ParseAsync(Stream fileStream, CancellationToken ct)
    {
        var results = new List<ParsedQuestion>();

        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheets.First();
        var headerRow = worksheet.FirstRowUsed();
        if (headerRow is null) return Task.FromResult(results);

        var columnIndexByName = headerRow.Cells()
            .Select((cell, i) => new { Name = cell.GetString().Trim(), Index = cell.Address.ColumnNumber })
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .ToDictionary(c => c.Name, c => c.Index, StringComparer.OrdinalIgnoreCase);

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRow.RowNumber();

        for (var rowNum = headerRow.RowNumber() + 1; rowNum <= lastRow; rowNum++)
        {
            ct.ThrowIfCancellationRequested();
            var row = worksheet.Row(rowNum);
            if (row.IsEmpty()) continue;

            string? Get(string column) =>
                columnIndexByName.TryGetValue(column, out var idx) ? row.Cell(idx).GetString().Trim() : null;

            var pq = new ParsedQuestion
            {
                Topic = Get("Topic"),
                Subtopic = Get("Subtopic"),
                DifficultyRaw = Get("Difficulty"),
                Prompt = Get("Prompt"),
                Explanation = Get("Explanation"),
                Reference = Get("Reference"),
            };

            foreach (var col in new[] { "Option1", "Option2", "Option3", "Option4", "Option5", "Option6" })
            {
                var value = Get(col);
                if (!string.IsNullOrWhiteSpace(value))
                    pq.Options.Add(value);
            }

            if (int.TryParse(Get("CorrectOptionIndex"), out var oneBasedIndex))
                pq.CorrectOptionIndex = oneBasedIndex - 1;

            if (!string.IsNullOrWhiteSpace(pq.Prompt))
                results.Add(pq);
        }

        return Task.FromResult(results);
    }
}
