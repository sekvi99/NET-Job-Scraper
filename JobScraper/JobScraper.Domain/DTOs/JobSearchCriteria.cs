using JobScraper.Domain.Enums;

namespace JobScraper.Domain.DTOs;

public record JobSearchCriteria
{
    public required List<string> Titles { get; init; } = [];
    public List<string>? Locations { get; init; }
    public List<Seniority>? Seniorities { get; init; }
    public DateTime? DateFrom { get; init; }
    public int? MaxPerSite { get; init; }
}