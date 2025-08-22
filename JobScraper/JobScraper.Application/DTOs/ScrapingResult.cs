using JobScraper.Domain.Enums;

namespace JobScraper.Application.DTOs;

public class ScrapingResult
{
    public int TotalFound { get; set; }
    public int Processed { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public int Duplicates { get; set; }
    public int SavedToSheets { get; set; }
    public bool SheetsSaveFailed { get; set; }
    public Dictionary<JobSource, int> ProcessedBySite { get; } = new();
    public List<JobSource> FailedSites { get; } = new();
}
