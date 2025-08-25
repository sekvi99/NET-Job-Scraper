using System.Reflection;
using JobScraper.Domain.DTOs;
using JobScraper.Domain.Entities;
using JobScraper.Domain.Enums;
using JobScraper.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace JobScraper.Infrastructure.Scrapers;

public abstract class BaseSeleniumScraper : IJobScraper,  IDisposable
{
    protected readonly IWebDriver _driver;
    protected readonly ILogger _logger;
    protected readonly IConfigurationService _config;

    protected BaseSeleniumScraper(ILogger logger, IConfigurationService config)
    {
        _logger = logger;
        _config = config;
        
        var options = new ChromeOptions();
        options.AddArguments("--headless"); // Run in background
        options.AddArguments("--no-sandbox");
        options.AddArguments("--disable-dev-shm-usage");
        options.AddArguments("--disable-gpu");
        options.AddArguments("--window-size=1920,1080");
        
        _driver = new ChromeDriver(options);
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(_config.DefaultTimeOut);
    }
    public abstract JobSource Source { get; }
    public abstract Task<IEnumerable<RawJobOffer>> ScrapeJobsAsync(JobSearchCriteria criteria, CancellationToken cancellationToken = default);
    public abstract string BuildSearchUrl(string title, JobSearchCriteria criteria);
    protected (IEnumerable<IWebElement>, int) FindElements(string title, JobSearchCriteria criteria, string jobsCssSelector)
    {
        var searchUrl = BuildSearchUrl(title, criteria);
        _logger.LogInformation("Scraping Portal with Selenium: {Url}", searchUrl);
                
        _driver.Navigate().GoToUrl(searchUrl);
                
        // Wait for job listings to load
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(_config.DefaultTimeOut));
        wait.Until(driver => driver.FindElements(By.CssSelector(jobsCssSelector)).Count > 0 || 
                             driver.FindElements(By.CssSelector(".no-results")).Count > 0);

        var jobElements = _driver.FindElements(By.CssSelector(jobsCssSelector));
        var maxJobs = criteria.MaxPerSite ?? _config.DefaultMaxPerSite;
        _logger.LogInformation($"Found  {jobElements.Count} jobs");
        
        return (jobElements.Take(maxJobs), maxJobs);
    }
    
    public void Dispose()
    {
        _driver?.Quit();
        _driver?.Dispose();
    }
}