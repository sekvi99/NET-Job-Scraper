using JobScraper.Application.Services;
using JobScraper.Domain.Interfaces;
using JobScraper.Infrastructure.Configuration;
using JobScraper.Infrastructure.Options;
using JobScraper.Infrastructure.Persistence;
using JobScraper.Infrastructure.Scrapers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace JobScraper.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        services.Configure<JobScraperOptions>(configuration.GetSection(JobScraperOptions.SectionName));
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        // HTTP Client with Polly
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} after {timespan} seconds");
                });

        services.AddHttpClient<NoFluffJobsScraper>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        }).AddPolicyHandler(retryPolicy);

        services.AddHttpClient<PracujScraper>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        }).AddPolicyHandler(retryPolicy);

        services.AddHttpClient<JustJoinScraper>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        }).AddPolicyHandler(retryPolicy);

        // Services
        services.AddScoped<IJobScraper, NoFluffJobsScraper>();
        services.AddScoped<IJobScraper, PracujScraper>();
        services.AddScoped<IJobScraper, JustJoinScraper>();
        services.AddScoped<IJobNormalizer, OpenAIJobNormalizer>();
        services.AddScoped<IJobRepository, SqliteJobRepository>();
        services.AddScoped<IDuplicateDetector, DuplicateDetectorService>();

        return services;
    }
}
