using CertMaster.Application.Features.Admin;
using CertMaster.Application.Features.Auth;
using CertMaster.Application.Features.Billing;
using CertMaster.Application.Features.Bookmarks;
using CertMaster.Application.Features.Certifications;
using CertMaster.Application.Features.Dashboard;
using CertMaster.Application.Features.Exams;
using CertMaster.Application.Features.Flashcards;
using CertMaster.Application.Features.Import;
using CertMaster.Application.Features.Leaderboard;
using CertMaster.Application.Features.Notifications;
using CertMaster.Application.Features.Questions;
using CertMaster.Application.Features.Search;
using Microsoft.Extensions.DependencyInjection;

namespace CertMaster.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<CertificationService>();
        services.AddScoped<QuestionService>();
        services.AddScoped<ExamService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<AdminService>();
        services.AddScoped<ImportService>();
        services.AddScoped<SearchService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<FlashcardService>();
        services.AddScoped<BillingService>();
        services.AddScoped<BookmarkService>();
        services.AddScoped<QuestionReportService>();
        services.AddScoped<CertMaster.Application.Features.Profile.ProfileService>();
        services.AddScoped<LeaderboardService>();

        return services;
    }
}
