using AngleSharp.Dom;
using JobScraper.Domain.DTOs;
using JobScraper.Domain.Entities;
using JobScraper.Domain.Enums;
using JobScraper.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace JobScraper.Infrastructure.Scrapers;

public class PracujScraper : BaseJobScraper
{
    public PracujScraper(HttpClient httpClient, ILogger<PracujScraper> logger, IConfigurationService config)
        : base(httpClient, logger, config) { }

    public override JobSource Source => JobSource.Pracuj;

    public override async Task<IEnumerable<RawJobOffer>> ScrapeJobsAsync(JobSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        var jobs = new List<RawJobOffer>();
        
        foreach (var title in criteria.Titles)
        {
            try
            {
                var searchUrl = BuildSearchUrl(title, criteria);
                _logger.LogInformation("Scraping Pracuj.pl: {Url}", searchUrl);
                
                var document = await GetDocumentAsync(searchUrl, cancellationToken);
                var jobCards = document.QuerySelectorAll("[data-test='default-offer']");
                
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
                        _logger.LogWarning(ex, "Failed to extract job from Pracuj.pl card");
                    }
                }

                _logger.LogInformation("Extracted {Count} jobs for title '{Title}' from Pracuj.pl", processedCount, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scrape Pracuj.pl for title: {Title}", title);
            }
        }

        return jobs;
    }

    private string BuildSearchUrl(string title, JobSearchCriteria criteria)
    {
        var baseUrl = "https://www.pracuj.pl/praca";
        var query = new List<string>();
        
        query.Add($"q={Uri.EscapeDataString(title)}");
        
        if (criteria.Locations != null && criteria.Locations.Any())
        {
            query.Add($"wp={string.Join(";", criteria.Locations.Select(Uri.EscapeDataString))}");
        }

        return query.Any() ? $"{baseUrl}?{string.Join("&", query)}" : baseUrl;
    }

    private RawJobOffer? ExtractJobFromCard(IElement jobCard)
    {
        try
        {
            var titleElement = jobCard.QuerySelector("h2 a");
            var companyElement = jobCard.QuerySelector("[data-test='text-company-name']");
            var locationElement = jobCard.QuerySelector("[data-test='text-region']");
            var salaryElement = jobCard.QuerySelector("[data-test='offer-salary']");

            var title = ExtractTextSafely(titleElement);
            var link = ExtractAttributeSafely(titleElement, "href");
            var company = ExtractTextSafely(companyElement);
            var location = ExtractTextSafely(locationElement);
            var salary = ExtractTextSafely(salaryElement);

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(link))
                return null;

            var fullLink = link.StartsWith("http") ? link : $"https://www.pracuj.pl{link}";

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
            _logger.LogWarning(ex, "Error extracting job from Pracuj.pl card");
            return null;
        }
    }
}
