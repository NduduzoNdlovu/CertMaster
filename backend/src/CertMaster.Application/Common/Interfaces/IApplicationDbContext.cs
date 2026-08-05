using CertMaster.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CertMaster.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the EF Core DbContext so the Application layer never
/// depends directly on Infrastructure or EF Core's provider-specific pieces.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Certification> Certifications { get; }
    DbSet<Topic> Topics { get; }
    DbSet<QuestionBankVersion> QuestionBankVersions { get; }
    DbSet<Question> Questions { get; }
    DbSet<QuestionOption> QuestionOptions { get; }
    DbSet<QuestionReport> QuestionReports { get; }
    DbSet<Bookmark> Bookmarks { get; }
    DbSet<ExamAttempt> ExamAttempts { get; }
    DbSet<ExamAnswer> ExamAnswers { get; }
    DbSet<MaintenanceWindow> MaintenanceWindows { get; }
    DbSet<AuditLogEntry> AuditLogEntries { get; }
    DbSet<PaymentTransaction> PaymentTransactions { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<ImportJob> ImportJobs { get; }
    DbSet<ImportJobImage> ImportJobImages { get; }
    DbSet<ImportedQuestion> ImportedQuestions { get; }
    DbSet<ImportedQuestionOption> ImportedQuestionOptions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
