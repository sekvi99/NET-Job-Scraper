using JobScraper.Domain.Enums;

namespace JobScraper.Domain.DTOs;

public record ScrapingProgress
{
    public int TotalFound { get; init; }
    public int Processed { get; init; }
    public int Normalized { get; init; }
    public int Failed { get; init; }
    public JobSource CurrentSource { get; init; }
    public string? CurrentActivity { get; init; }
}