namespace JobScraper.Domain.Interfaces;

public interface IConfigurationService
{
    string OpenAiApiKey { get; }
    string SqliteConnectionString { get; }
    int DefaultMaxPerSite { get; }
    int RateLimitDelayMs { get; }
    int MaxRetries { get; }
    int DefaultTimeOut { get; }
}