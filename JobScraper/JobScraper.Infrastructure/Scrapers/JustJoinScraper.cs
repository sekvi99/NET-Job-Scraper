using AngleSharp.Dom;
using JobScraper.Domain.DTOs;
using JobScraper.Domain.Entities;
using JobScraper.Domain.Enums;
using JobScraper.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace JobScraper.Infrastructure.Scrapers;

public class JustJoinScraper : BaseJobScraper
{
    public JustJoinScraper(HttpClient httpClient, ILogger<JustJoinScraper> logger, IConfigurationService config)
        : base(httpClient, logger, config) { }

    public override JobSource Source => JobSource.JustJoin;

    public override async Task<IEnumerable<RawJobOffer>> ScrapeJobsAsync(JobSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        var jobs = new List<RawJobOffer>();
        
        foreach (var title in criteria.Titles)
        {
            try
            {
                var searchUrl = BuildSearchUrl(title, criteria);
                _logger.LogInformation("Scraping JustJoin.it: {Url}", searchUrl);
                
                var document = await GetDocumentAsync(searchUrl, cancellationToken);
                var jobCards = document.QuerySelectorAll("[data-test-id='virtualized-list-item']");
                
                var maxJobs = criteria.MaxPerSite ?? _config.DefaultMaxPerSite;
                var processedCount = 0;

                foreach (var jobCard in jobCards.Take(maxJobs))
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        var job = ExtractJobFromCard(jobCard);
                        if (job != null)
                        {
                            jobs.Add(job);
                            processedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to extract job from JustJoin.it card");
                    }
                }

                _logger.LogInformation("Extracted {Count} jobs for title '{Title}' from JustJoin.it", processedCount, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scrape JustJoin.it for title: {Title}", title);
            }
        }

        return jobs;
    }

    private string BuildSearchUrl(string title, JobSearchCriteria criteria)
    {
        var baseUrl = "https://justjoin.it/all-locations";
        var fragment = new List<string>();
        
        fragment.Add($"keyword={Uri.EscapeDataString(title)}");
        
        if (criteria.Seniorities != null && criteria.Seniorities.Any())
        {
            var seniorities = criteria.Seniorities.Select(s => s.ToString().ToLower());
            fragment.Add($"experienceLevel={string.Join(",", seniorities)}");
        }

        return fragment.Any() ? $"{baseUrl}/#{string.Join("&", fragment)}" : baseUrl;
    }

    private RawJobOffer? ExtractJobFromCard(IElement jobCard)
    {
        try
        {
            var linkElement = jobCard.QuerySelector("a");
            var titleElement = jobCard.QuerySelector("h3");
            var companyElement = jobCard.QuerySelector("[data-test-id='company-name']");
            var locationElement = jobCard.QuerySelector("[data-test-id='location-name']");
            var salaryElement = jobCard.QuerySelector("[data-test-id='salary-range']");

            var link = ExtractAttributeSafely(linkElement, "href");
            var title = ExtractTextSafely(titleElement);
            var company = ExtractTextSafely(companyElement);
            var location = ExtractTextSafely(locationElement);
            var salary = ExtractTextSafely(salaryElement);

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(link))
                return null;

            var fullLink = link.StartsWith("http") ? link : $"https://justjoin.it{link}";

            return new RawJobOffer
            {
                ScrapedTitle = CleanText(title),
                ScrapedCompany = CleanText(company),
                ScrapedLocation = CleanText(location),
                ScrapedSalaryText = CleanText(salary),
                CleanedDescription = CleanText(title), // Simplified for this example
                Link = fullLink,
                Source = Source
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting job from JustJoin.it card");
            return null;
        }
    }
}
