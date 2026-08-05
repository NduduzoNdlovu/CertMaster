using System.Globalization;
using CertMaster.Application.Common.Interfaces;
using CertMaster.Application.Features.Import;
using CertMaster.Domain.Entities;
using CertMaster.Domain.Enums;
using CsvHelper;
using Microsoft.EntityFrameworkCore;

namespace CertMaster.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds the database with a default administrator account, the initial certification
/// tracks, and starter sample content for local development. This is idempotent — safe
/// to run every time the API starts.
///
/// Importantly, the sample questions are NOT inserted directly into the live Question
/// table. They are built into an in-memory CSV and pushed through the exact same
/// ImportService pipeline a real administrator uses (upload → extract → validate →
/// stage for review → approve → publish). This seeder plays the role of "an admin
/// reviewed and approved this content" programmatically — there is no special-cased
/// bypass of the review workflow, even for local development data.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IPasswordHasher passwordHasher, ImportService importService)
    {
        await SeedAdminAsync(db, passwordHasher);

        var aPlusCore1 = await GetOrCreateCertificationAsync(db, "220-1201", "CompTIA A+ Core 1", "CompTIA", "Core Series 1200",
            "Mobile devices, networking, hardware, virtualization and cloud, and troubleshooting.");
        var aPlusCore2 = await GetOrCreateCertificationAsync(db, "220-1202", "CompTIA A+ Core 2", "CompTIA", "Core Series 1200",
            "Operating systems, security, software troubleshooting, and operational procedures.");
        var networkPlus = await GetOrCreateCertificationAsync(db, "N10-009", "CompTIA Network+", "CompTIA", "N10-009",
            "Networking concepts, infrastructure, network operations, security, and troubleshooting.");

        await db.SaveChangesAsync();

        await SeedViaImportPipelineAsync(db, importService, aPlusCore1, SeedQuestionBank.APlusCore1(), "seed-aplus-core1.csv");
        await SeedViaImportPipelineAsync(db, importService, aPlusCore2, SeedQuestionBank.APlusCore2(), "seed-aplus-core2.csv");
        await SeedViaImportPipelineAsync(db, importService, networkPlus, SeedQuestionBank.NetworkPlus(), "seed-networkplus.csv");
    }

    private static async Task SeedAdminAsync(AppDbContext db, IPasswordHasher passwordHasher)
    {
        if (await db.Users.AnyAsync(u => u.Role == UserRole.Administrator))
            return;

        db.Users.Add(new User
        {
            FullName = "CertMaster Administrator",
            Email = "admin@certmaster.local",
            PasswordHash = passwordHasher.Hash("ChangeMe123!"),
            Role = UserRole.Administrator,
            EmailConfirmed = true,
        });

        await db.SaveChangesAsync();
    }

    private static async Task<Certification> GetOrCreateCertificationAsync(
        AppDbContext db, string code, string name, string vendor, string version, string description)
    {
        var existing = await db.Certifications.FirstOrDefaultAsync(c => c.Code == code);
        if (existing is not null)
            return existing;

        var certification = new Certification
        {
            Code = code,
            Name = name,
            Vendor = vendor,
            CurrentVersion = version,
            Description = description,
        };

        db.Certifications.Add(certification);
        return certification;
    }

    private static async Task SeedViaImportPipelineAsync(
        AppDbContext db, ImportService importService, Certification certification,
        List<SeedQuestionBank.SeedQuestion> seedQuestions, string fileName)
    {
        // Idempotent: if this certification already has any live (published) questions,
        // assume seeding already happened (or an admin has since imported real content)
        // and don't touch it.
        var alreadyHasQuestions = await db.Questions.AnyAsync(q => q.CertificationId == certification.Id);
        if (alreadyHasQuestions) return;

        var csv = BuildCsv(seedQuestions);
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));

        var request = new StartImportRequest(certification.Id, null, "v1 - local development sample content");
        var job = await importService.StartImportAsync(request, stream, fileName, CancellationToken.None);

        if (job.Status != nameof(ImportJobStatus.ReadyForReview))
        {
            // Something went wrong parsing the generated seed CSV — leave it as a failed
            // import job for inspection rather than silently losing the sample content.
            return;
        }

        var detail = await importService.GetImportJobDetailAsync(job.Id, CancellationToken.None);
        var pendingIds = detail.Questions
            .Where(q => q.ReviewStatus == nameof(ImportedQuestionReviewStatus.PendingReview))
            .Select(q => q.Id)
            .ToList();

        if (pendingIds.Count == 0) return;

        await importService.ApproveAsync(new ApproveQuestionsRequest(pendingIds), CancellationToken.None);
        await importService.PublishVersionAsync(job.QuestionBankVersionId, CancellationToken.None);
    }

    /// <summary>Builds a CSV matching the column layout CsvDumpParser expects, so the
    /// seed data is parsed by the exact same code path a real CSV upload would use.</summary>
    private static string BuildCsv(List<SeedQuestionBank.SeedQuestion> questions)
    {
        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        foreach (var header in new[] { "Topic", "Subtopic", "Difficulty", "Prompt", "Option1", "Option2", "Option3", "Option4", "CorrectOptionIndex", "Explanation", "Reference" })
            csv.WriteField(header);
        csv.NextRecord();

        foreach (var q in questions)
        {
            csv.WriteField(q.Topic);
            csv.WriteField(q.Subtopic ?? string.Empty);
            csv.WriteField(q.Difficulty.ToString());
            csv.WriteField(q.Prompt);

            for (var i = 0; i < 4; i++)
                csv.WriteField(i < q.Options.Length ? q.Options[i].Text : string.Empty);

            var correctOneBasedIndex = Array.FindIndex(q.Options, o => o.IsCorrect) + 1;
            csv.WriteField(correctOneBasedIndex.ToString());
            csv.WriteField(q.Explanation);
            csv.WriteField(q.Reference ?? string.Empty);
            csv.NextRecord();
        }

        writer.Flush();
        return writer.ToString();
    }
}
