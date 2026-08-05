using CertMaster.Application.Common.Interfaces;
using CertMaster.Application.Features.DumpUpload;
using CertMaster.Infrastructure.DumpParsing;
using CertMaster.Infrastructure.Email;
using CertMaster.Infrastructure.Identity;
using CertMaster.Infrastructure.Payments;
using CertMaster.Infrastructure.Persistence;
using CertMaster.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CertMaster.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddScoped<IDumpParser, CsvDumpParser>();
        services.AddScoped<IDumpParser, ExcelDumpParser>();
        services.AddScoped<IDumpParser, WordDumpParser>();
        services.AddScoped<IDumpParser, PdfDumpParser>();
        services.AddScoped<IDumpParser, TxtDumpParser>();

        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        var emailProvider = configuration.GetValue<string>($"{EmailSettings.SectionName}:Provider") ?? "Console";
        if (string.Equals(emailProvider, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddScoped<IEmailSender, ConsoleEmailSender>();
        }

        services.AddScoped<IPaymentGatewayProvider, MockPaymentGatewayProvider>();

        services.Configure<LocalStorageSettings>(configuration.GetSection(LocalStorageSettings.SectionName));
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "CertMaster:";
            });
        }
        else
        {
            // No Redis configured — falls back to an in-process cache so the app
            // still runs locally without Redis installed. Set ConnectionStrings:Redis
            // to enable real distributed caching (required for multi-instance deployments).
            services.AddDistributedMemoryCache();
        }

        return services;
    }
}
