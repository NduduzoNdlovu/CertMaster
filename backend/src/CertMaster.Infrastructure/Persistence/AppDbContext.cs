using CertMaster.Application.Common.Interfaces;
using CertMaster.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CertMaster.Infrastructure.Persistence;

public class AppDbContext : DbContext, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Certification> Certifications => Set<Certification>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<QuestionBankVersion> QuestionBankVersions => Set<QuestionBankVersion>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<QuestionReport> QuestionReports => Set<QuestionReport>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<ExamAttempt> ExamAttempts => Set<ExamAttempt>();
    public DbSet<ExamAnswer> ExamAnswers => Set<ExamAnswer>();
    public DbSet<MaintenanceWindow> MaintenanceWindows => Set<MaintenanceWindow>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();
    public DbSet<ImportJobImage> ImportJobImages => Set<ImportJobImage>();
    public DbSet<ImportedQuestion> ImportedQuestions => Set<ImportedQuestion>();
    public DbSet<ImportedQuestionOption> ImportedQuestionOptions => Set<ImportedQuestionOption>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
