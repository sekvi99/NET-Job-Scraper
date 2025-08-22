using AngleSharp.Dom;
using JobScraper.Domain.DTOs;
using JobScraper.Domain.Entities;
using JobScraper.Domain.Enums;
using JobScraper.Domain.Interfaces;
using JobScraper.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;

namespace JobScraper.Infrastructure.Scrapers;

public class NoFluffJobsScraper : BaseJobScraper
{
    public NoFluffJobsScraper(HttpClient httpClient, ILogger<NoFluffJobsScraper> logger, IConfigurationService config)
        : base(httpClient, logger, config) { }

    public override JobSource Source => JobSource.NoFluffJobs;

    public override async Task<IEnumerable<RawJobOffer>> ScrapeJobsAsync(JobSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        var jobs = new List<RawJobOffer>();
        
        foreach (var title in criteria.Titles)
        {
            try
            {
                var searchUrl = BuildSearchUrl(title, criteria);
                _logger.LogInformation("Scraping NoFluffJobs: {Url}", searchUrl);
                
                var document = await GetDocumentAsync(searchUrl, cancellationToken);
                var jobCards = document.QuerySelectorAll(".posting-list-item");
                
                var maxJobs = criteria.MaxPerSite ?? _config.DefaultMaxPerSite;
                var processedCount = 0;

                foreach (var jobCard in jobCards.Take(maxJobs))
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        var job = await ExtractJobFromCard(jobCard, cancellationToken);
                        if (job != null)
                        {
                            jobs.Add(job);
                            processedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to extract job from NoFluffJobs card");
                    }
                }

                _logger.LogInformation("Extracted {Count} jobs for title '{Title}' from NoFluffJobs", processedCount, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scrape NoFluffJobs for title: {Title}", title);
            }
        }

        return jobs;
    }

    private string BuildSearchUrl(string title, JobSearchCriteria criteria)
    {
        var baseUrl = "https://nofluffjobs.com/pl/jobs";
        var query = new List<string>();
        
        query.Add($"criteria={Uri.EscapeDataString(title)}");
        
        if (criteria.Locations != null && criteria.Locations.Any())
        {
            query.Add($"city={string.Join(",", criteria.Locations.Select(Uri.EscapeDataString))}");
        }
        
        if (criteria.Seniorities != null && criteria.Seniorities.Any())
        {
            var seniorities = criteria.Seniorities.Select(s => s.ToString().ToLower());
            query.Add($"seniority={string.Join(",", seniorities)}");
        }

        return query.Any() ? $"{baseUrl}?{string.Join("&", query)}" : baseUrl;
    }

    private async Task<RawJobOffer?> ExtractJobFromCard(IElement jobCard, CancellationToken cancellationToken)
    {
        try
        {
            var titleElement = jobCard.QuerySelector(".posting-title__position");
            var linkElement = jobCard.QuerySelector("a.posting-list-item");
            var companyElement = jobCard.QuerySelector(".posting-title__company");
            var locationElement = jobCard.QuerySelector(".posting-info__location");
            var salaryElement = jobCard.QuerySelector(".posting-info__salary");

            var title = ExtractTextSafely(titleElement);
            var relativeLink = ExtractAttributeSafely(linkElement, "href");
            var company = ExtractTextSafely(companyElement);
            var location = ExtractTextSafely(locationElement);
            var salary = ExtractTextSafely(salaryElement);

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(relativeLink))
                return null;

            var fullLink = relativeLink.StartsWith("http") ? relativeLink : $"https://nofluffjobs.com{relativeLink}";
            
            // Get job details page for description
            var description = await GetJobDescription(fullLink, cancellationToken);

            return new RawJobOffer
            {
                ScrapedTitle = CleanText(title),
                ScrapedCompany = CleanText(company),
                ScrapedLocation = CleanText(location),
                ScrapedSalaryText = CleanText(salary),
                CleanedDescription = description,
                Link = fullLink,
                Source = Source
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting job from NoFluffJobs card");
            return null;
        }
    }

    private async Task<string> GetJobDescription(string jobUrl, CancellationToken cancellationToken)
    {
        try
        {
            var document = await GetDocumentAsync(jobUrl, cancellationToken);
            var descriptionElement = document.QuerySelector(".posting-description");
            var requirementsElement = document.QuerySelector(".posting-requirements");
            
            var description = ExtractTextSafely(descriptionElement) ?? string.Empty;
            var requirements = ExtractTextSafely(requirementsElement) ?? string.Empty;
            
            return CleanText($"{description}\n{requirements}").Truncate(2000);
        }
        catch
        {
            return string.Empty;
        }
    }
}
