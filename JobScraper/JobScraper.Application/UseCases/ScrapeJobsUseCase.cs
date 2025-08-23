using JobScraper.Application.DTOs;
using JobScraper.Domain.DTOs;
using JobScraper.Domain.Entities;
using JobScraper.Domain.Enums;
using JobScraper.Domain.Interfaces;
using JobScraper.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace JobScraper.Application.UseCases;

public class ScrapeJobsUseCase
{
    private readonly IEnumerable<IJobScraper> _scrapers;
    private readonly IJobNormalizer _normalizer;
    private readonly IDuplicateDetector _duplicateDetector;
    private readonly IProgressReporter _progressReporter;
    private readonly ILogger<ScrapeJobsUseCase> _logger;
    private readonly IJobRepository _jobRepository;

    public ScrapeJobsUseCase(
        IEnumerable<IJobScraper> scrapers,
        IJobNormalizer normalizer,
        IDuplicateDetector duplicateDetector,
        IJobRepository jobRepository,
        IProgressReporter progressReporter,
        ILogger<ScrapeJobsUseCase> logger)
    {
        _scrapers = scrapers;
        _normalizer = normalizer;
        _jobRepository = jobRepository;
        _duplicateDetector = duplicateDetector;
        _progressReporter = progressReporter;
        _logger = logger;
    }

    public async Task<ScrapingResult> ExecuteAsync(JobSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting job scraping with criteria: {Criteria}", criteria);
        
        var result = new ScrapingResult();
        var allJobs = new List<JobOffer>();

        // Get existing links to avoid duplicates
        var existingLinks = (await _jobRepository.GetExistingLinksAsync(cancellationToken)).ToHashSet();
        
        foreach (var scraper in _scrapers)
        {
            try
            {
                _progressReporter.ReportProgress(new ScrapingProgress
                {
                    CurrentSource = scraper.Source,
                    CurrentActivity = "Scraping job listings"
                });

                _logger.LogInformation("Scraping jobs from {Source}", scraper.Source);
                var rawJobs = await scraper.ScrapeJobsAsync(criteria, cancellationToken);
                
                result.TotalFound += rawJobs.Count();
                _logger.LogInformation("Found {Count} raw jobs from {Source}", rawJobs.Count(), scraper.Source);

                var processedJobs = new List<JobOffer>();

                foreach (var rawJob in rawJobs)
                {
                    try
                    {
                        // Skip if already exists
                        if (existingLinks.Contains(rawJob.Link))
                        {
                            result.Skipped++;
                            continue;
                        }

                        _progressReporter.ReportProgress(new ScrapingProgress
                        {
                            CurrentSource = scraper.Source,
                            CurrentActivity = "Normalizing job data",
                            Processed = result.Processed,
                            TotalFound = result.TotalFound
                        });

                        var normalizedData = await _normalizer.NormalizeJobAsync(rawJob, cancellationToken);
                        if (normalizedData == null)
                        {
                            result.Failed++;
                            _logger.LogWarning("Failed to normalize job: {Link}", rawJob.Link);
                            continue;
                        }

                        var jobOffer = MapToJobOffer(normalizedData, rawJob);
                        processedJobs.Add(jobOffer);
                        result.Processed++;

                    }
                    catch (Exception ex)
                    {
                        result.Failed++;
                        _logger.LogError(ex, "Error processing job: {Link}", rawJob.Link);
                    }
                }

                allJobs.AddRange(processedJobs);
                result.ProcessedBySite[scraper.Source] = processedJobs.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scraping from {Source}", scraper.Source);
                result.FailedSites.Add(scraper.Source);
            }
        }

        // Remove duplicates
        var uniqueJobs = _duplicateDetector.RemoveDuplicates(allJobs).ToList();
        result.Duplicates = allJobs.Count - uniqueJobs.Count;

        // Save to Cache db
        if (uniqueJobs.Any())
        {
            try
            {
                _progressReporter.ReportProgress(new ScrapingProgress
                {
                    CurrentActivity = "Saving to sqlite database",
                    Processed = result.Processed,
                    TotalFound = result.TotalFound
                });

                await _jobRepository.SaveJobsAsync(uniqueJobs, cancellationToken);
                result.SavedToSheets = uniqueJobs.Count;
                _logger.LogInformation("Saved {Count} jobs to Google Sheets", uniqueJobs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save jobs to Google Sheets");
                result.SheetsSaveFailed = true;
            }
        }
        
        _logger.LogInformation("Scraping completed. Result: {@Result}", result);
        return result;
    }

    private static JobOffer MapToJobOffer(NormalizedJobData data, RawJobOffer raw)
    {
        return new JobOffer
        {
            Title = data.Title,
            Link = data.Link,
            Salary = MapSalary(data.Salary),
            RequiredYearsExperience = MapYearsExperience(data.RequiredYearsExperience),
            RequiredSkills = data.RequiredSkills,
            ExpirationDate = data.ExpirationDate,
            Company = data.Company,
            Location = data.Location,
            Source = Enum.Parse<JobSource>(data.Source, true),
            PostedDate = data.PostedDate,
            RawTextSnapshot = raw.CleanedDescription
        };
    }

    private static SalaryInfo? MapSalary(object? salary)
    {
        return salary switch
        {
            string text => SalaryInfo.FromText(text),
            Dictionary<string, object> dict => MapSalaryDict(dict),
            _ => null
        };
    }

    private static SalaryInfo? MapSalaryDict(Dictionary<string, object> dict)
    {
        var min = dict.TryGetValue("min", out var minVal) ? Convert.ToDecimal(minVal) : (decimal?)null;
        var max = dict.TryGetValue("max", out var maxVal) ? Convert.ToDecimal(maxVal) : (decimal?)null;
        var currency = dict.TryGetValue("currency", out var currVal) ? currVal?.ToString() : null;
        var period = dict.TryGetValue("period", out var periodVal) ? 
            Enum.Parse<SalaryPeriod>(periodVal?.ToString() ?? "Monthly", true) : SalaryPeriod.Monthly;
        var grossNet = dict.TryGetValue("grossNet", out var gnVal) ? 
            Enum.Parse<GrossNet>(gnVal?.ToString() ?? "Unspecified", true) : GrossNet.Unspecified;

        return currency != null ? SalaryInfo.FromStructured(min, max, currency, period, grossNet) : null;
    }

    private static YearsExperience? MapYearsExperience(object? experience)
    {
        return experience switch
        {
            string text => YearsExperience.FromText(text),
            int years => YearsExperience.FromNumber(years),
            double years => YearsExperience.FromNumber((int)years),
            _ => null
        };
    }
}
