namespace JobScraper.Infrastructure.Options;

public class JobScraperOptions
{
    public const string SectionName = "JobScraper";
    public string? OpenAiApiKey { get; set; }
    public int DefaultMaxPerSite { get; set; } = 50;
    public int RateLimitDelayMs { get; set; } = 1000;
    public int MaxRetries { get; set; } = 3;
    public GoogleSheetsOptions GoogleSheets { get; set; } = new();
}