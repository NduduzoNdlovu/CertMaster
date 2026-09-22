using CertMaster.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CertMaster.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.FullName).HasMaxLength(200).IsRequired();
    }
}

public class CertificationConfiguration : IEntityTypeConfiguration<Certification>
{
    public void Configure(EntityTypeBuilder<Certification> builder)
    {
        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasMany(c => c.Topics).WithOne(t => t.Certification!).HasForeignKey(t => t.CertificationId);
        builder.HasMany(c => c.Questions).WithOne(q => q.Certification!).HasForeignKey(q => q.CertificationId);
    }
}

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.HasIndex(q => new { q.CertificationId, q.Topic, q.Status });
        builder.HasMany(q => q.Options)
            .WithOne(o => o.Question!)
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class BookmarkConfiguration : IEntityTypeConfiguration<Bookmark>
{
    public void Configure(EntityTypeBuilder<Bookmark> builder)
    {
        // A learner can only bookmark a given question once.
        builder.HasIndex(b => new { b.UserId, b.QuestionId }).IsUnique();

        builder.HasOne(b => b.Question)
            .WithMany()
            .HasForeignKey(b => b.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.User)
            .WithMany(u => u.Bookmarks)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class QuestionReportConfiguration : IEntityTypeConfiguration<QuestionReport>
{
    public void Configure(EntityTypeBuilder<QuestionReport> builder)
    {
        builder.HasOne(r => r.Question)
            .WithMany()
            .HasForeignKey(r => r.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ExamAttemptConfiguration : IEntityTypeConfiguration<ExamAttempt>
{
    public void Configure(EntityTypeBuilder<ExamAttempt> builder)
    {
        builder.HasIndex(a => new { a.UserId, a.StartedAtUtc });
        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("IX_ExamAttempts_UserId_OneActiveMock")
            .IsUnique()
            .HasFilter("\"CompletedAtUtc\" IS NULL AND \"Mode\" = 1");
        builder.HasMany(a => a.Answers)
            .WithOne(x => x.ExamAttempt!)
            .HasForeignKey(x => x.ExamAttemptId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasIndex(t => t.Token).IsUnique();
    }
}

public class ImportJobConfiguration : IEntityTypeConfiguration<ImportJob>
{
    public void Configure(EntityTypeBuilder<ImportJob> builder)
    {
        builder.HasIndex(j => new { j.CertificationId, j.Status });
        builder.HasIndex(j => j.QuestionBankVersionId);

        builder.HasOne(j => j.QuestionBankVersion)
            .WithMany(v => v.ImportJobs)
            .HasForeignKey(j => j.QuestionBankVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(j => j.ExtractedImages)
            .WithOne(i => i.ImportJob!)
            .HasForeignKey(i => i.ImportJobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(j => j.ImportedQuestions)
            .WithOne(q => q.ImportJob!)
            .HasForeignKey(q => q.ImportJobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ImportedQuestionConfiguration : IEntityTypeConfiguration<ImportedQuestion>
{
    public void Configure(EntityTypeBuilder<ImportedQuestion> builder)
    {
        builder.HasIndex(q => new { q.QuestionBankVersionId, q.ReviewStatus });

        builder.HasOne(q => q.QuestionBankVersion)
            .WithMany()
            .HasForeignKey(q => q.QuestionBankVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(q => q.Certification)
            .WithMany()
            .HasForeignKey(q => q.CertificationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(q => q.Options)
            .WithOne(o => o.ImportedQuestion!)
            .HasForeignKey(o => o.ImportedQuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
