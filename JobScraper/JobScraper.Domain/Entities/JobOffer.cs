using JobScraper.Domain.Enums;
using JobScraper.Domain.ValueObjects;

namespace JobScraper.Domain.Entities;

public class JobOffer
{
    public required string Title { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public required string Link { get; set; }
    public SalaryInfo? Salary { get; set; }
    public YearsExperience? RequiredYearsExperience { get; set; }
    public required List<string> RequiredSkills { get; set; } = [];
    public string? Company { get; set; }
    public string? Location { get; set; }
    public required JobSource Source { get; set; }
    public DateTime? PostedDate { get; set; }
    public string? RawTextSnapshot { get; set; }
    public DateTime IngestedAt { get; set; } = DateTime.UtcNow;
}