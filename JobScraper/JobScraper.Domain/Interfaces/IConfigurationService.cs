namespace JobScraper.Domain.Interfaces;

public interface IConfigurationService
{
    string OpenAiApiKey { get; }
    string GoogleSheetsSpreadsheetId { get; }
    string GoogleSheetsSheetName { get; }
    string GoogleCredentialsPath { get; }
    int DefaultMaxPerSite { get; }
    int RateLimitDelayMs { get; }
    int MaxRetries { get; }
}