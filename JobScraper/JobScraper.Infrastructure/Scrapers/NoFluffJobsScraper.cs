using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using JobScraper.Domain.DTOs;
using JobScraper.Domain.Entities;
using JobScraper.Domain.Enums;
using JobScraper.Domain.Interfaces;
using JobScraper.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;

namespace JobScraper.Infrastructure.Scrapers;

public class NoFluffJobsScraper : BaseSeleniumScraper
{
    public NoFluffJobsScraper(ILogger<PracujScraper> logger, IConfigurationService config)
        : base(logger, config) { }

    public override JobSource Source => JobSource.NoFluffJobs;

    public override async Task<IEnumerable<RawJobOffer>> ScrapeJobsAsync(JobSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Scraping JustJoin.it with Selenium");
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
                        _logger.LogWarning(ex, "Failed to extract job from NoFluffJobs element");
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

    public override string BuildSearchUrl(string title, JobSearchCriteria criteria)
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

    private async Task<RawJobOffer?> ExtractJobFromElement(IWebElement jobElement, CancellationToken cancellationToken)
    {
        try
        {
            var title = jobElement.SafeGetText(".posting-title__position");
        
            // Extract company name from footer
            var company = jobElement.SafeGetText("footer .company-name");
        
            // Extract salary - try multiple selectors
            var salary = jobElement.SafeGetTextMultiple(
                ".posting-item-salary",
                "[data-cy*='salary']",
                ".salary", 
                ".posting-tag span",
                ".tw-cursor-pointer span",
                ".posting-tag"
            );
        
            // Extract location - try multiple selectors  
            var location = jobElement.SafeGetTextMultiple(
                ".posting-item-city",
                "[data-cy*='location']", 
                ".location",
                ".posting-info__location",
                ".posting-info__location span",
                ".tw-text-gray"
            );
            
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
                Link = fullLink,
                Source = Source
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting job from NoFluffJobs element");
            return null;
        }
    }
    
    private string ExtractJobLink(IWebElement jobElement)
    {
        try
        {
            // Try multiple approaches to find the link
            IWebElement linkElement = null;
        
            // Approach 1: Look for specific link with job URL
            try
            {
                linkElement = jobElement.FindElement(By.CssSelector("a[href*='/job/']"));
            }
            catch (NoSuchElementException)
            {
                // Approach 2: Look for the first a tag
                try
                {
                    linkElement = jobElement.FindElement(By.TagName("a"));
                }
                catch (NoSuchElementException)
                {
                    // Approach 3: Look for link in header
                    try
                    {
                        linkElement = jobElement.FindElement(By.CssSelector("header a"));
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
            return relativeLink.StartsWith("http") ? relativeLink : $"https://nofluffjobs.com{relativeLink}";
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}