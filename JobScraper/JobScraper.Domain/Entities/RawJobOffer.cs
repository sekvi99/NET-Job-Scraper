using JobScraper.Domain.Enums;

namespace JobScraper.Domain.Entities;

public class RawJobOffer
{
    public required string ScrapedTitle { get; set; }
    public string? ScrapedCompany { get; set; }
    public string? ScrapedLocation { get; set; }
    public string? ScrapedSalaryText { get; set; }
    public string? ScrapedExpirationText { get; set; }
    public string? ScrapedPostedText { get; set; }
    public string? CleanedDescription { get; set; }
    public required string Link { get; set; }
    public required JobSource Source { get; set; }
}