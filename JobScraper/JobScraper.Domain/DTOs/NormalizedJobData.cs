namespace JobScraper.Domain.DTOs;

public record NormalizedJobData
{
    public required string Title { get; init; }
    public required string Link { get; init; }
    public object? Salary { get; init; } // Can be string or structured object
    public object? RequiredYearsExperience { get; init; } // Can be number or string
    public required List<string> RequiredSkills { get; init; }
    public DateTime? ExpirationDate { get; init; }
    public string? Company { get; init; }
    public string? Location { get; init; }
    public required string Source { get; init; }
    public DateTime? PostedDate { get; init; }
}
