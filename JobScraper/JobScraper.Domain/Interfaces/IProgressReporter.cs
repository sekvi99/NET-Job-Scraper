using JobScraper.Domain.DTOs;

namespace JobScraper.Domain.Interfaces;

public interface IProgressReporter
{
    void ReportProgress(ScrapingProgress progress);
}