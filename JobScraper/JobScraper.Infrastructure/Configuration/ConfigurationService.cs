using JobScraper.Domain.Interfaces;
using JobScraper.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace JobScraper.Infrastructure.Configuration;

public class ConfigurationService : IConfigurationService
{
    private readonly JobScraperOptions _options;

    public ConfigurationService(IOptions<JobScraperOptions> options)
    {
        _options = options.Value;
    }

    public string OpenAiApiKey => _options.OpenAiApiKey 
                                  ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
                                  ?? throw new InvalidOperationException("OpenAI API Key not configured");

    public string SqliteConnectionString => Environment.GetEnvironmentVariable("SQLITE_CONNECTION_STRING") 
                                            ?? throw new InvalidOperationException("Sqlite Connection not configured");

    public int DefaultMaxPerSite => _options.DefaultMaxPerSite;
    public int RateLimitDelayMs => _options.RateLimitDelayMs;
    public int MaxRetries => _options.MaxRetries;
}