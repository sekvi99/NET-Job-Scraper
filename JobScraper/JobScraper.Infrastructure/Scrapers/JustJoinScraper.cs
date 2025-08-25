using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using JobScraper.Domain.DTOs;
using JobScraper.Domain.Entities;
using JobScraper.Domain.Enums;
using JobScraper.Domain.Interfaces;
using JobScraper.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;

namespace JobScraper.Infrastructure.Scrapers;

public class JustJoinScraper : BaseSeleniumScraper
{
    public JustJoinScraper(ILogger<PracujScraper> logger, IConfigurationService config)
        : base(logger, config) { }

    public override JobSource Source => JobSource.JustJoin;

    public override async Task<IEnumerable<RawJobOffer>> ScrapeJobsAsync(JobSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        var jobs = new List<RawJobOffer>();
        
        foreach (var title in criteria.Titles)
        {
            try
            {
                var (webJobs, maxJobs) = FindElements(title, criteria, ".posting-list-item");
                var processedCount = 0;
                var jobIndex = 0;


                foreach (var jobElement in webJobs)
                {
                    jobIndex++;
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        _logger.LogInformation($"Processing {jobIndex} out of {maxJobs} jobs");
                        var job = await ExtractJobFromElement(jobElement, cancellationToken);
                        if (job != null)
                        {
                            jobs.Add(job);
                            processedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to extract job from JustJoin.it element");
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

    public override string BuildSearchUrl(string title, JobSearchCriteria criteria)
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

    private async Task<RawJobOffer?> ExtractJobFromElement(IWebElement jobElement, CancellationToken cancellationToken)
    {
        try
        {
            var title = jobElement.SafeGetText("h3");
            var company = jobElement.SafeGetText("[data-test-id='company-name']");
            var location = jobElement.SafeGetText("[data-test-id='location-name']");
            var salary = jobElement.SafeGetText("[data-test-id='salary-range']");
            
            // Extract link safely
            var fullLink = ExtractJobLink(jobElement);

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(fullLink))
                return null;

            return new RawJobOffer
            {
                ScrapedTitle = title.CleanText(),
                ScrapedCompany = company.CleanText(),
                ScrapedLocation = location.CleanText(),
                ScrapedSalaryText = salary.CleanText(),
                CleanedDescription = title.CleanText(), // Simplified for this example
                Link = fullLink,
                Source = Source
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting job from JustJoin.it element");
            return null;
        }
    }

    private string ExtractJobLink(IWebElement jobElement)
    {
        try
        {
            // Try multiple approaches to find the link
            IWebElement linkElement = null;

            // Approach 1: Look for the first a tag (most common for JustJoin.it)
            try
            {
                linkElement = jobElement.FindElement(By.TagName("a"));
            }
            catch (NoSuchElementException)
            {
                // Approach 2: Look for specific link patterns
                try
                {
                    linkElement = jobElement.FindElement(By.CssSelector("a[href*='/offer/']"));
                }
                catch (NoSuchElementException)
                {
                    // Approach 3: Look for any clickable link
                    try
                    {
                        linkElement = jobElement.FindElement(By.CssSelector("a[href]"));
                    }
                    catch (NoSuchElementException)
                    {
                        return string.Empty;
                    }
                }
            }

            var relativeLink = linkElement?.GetAttribute("href");

            if (string.IsNullOrEmpty(relativeLink))
                return string.Empty;

            // Convert relative to absolute URL
            return relativeLink.StartsWith("http") ? relativeLink : $"https://justjoin.it{relativeLink}";
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}