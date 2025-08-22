using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Extensions.Logging;
using JobScraper.Domain.Entities;
using JobScraper.Domain.Interfaces;
using JobScraper.Domain.DTOs;
using JobScraper.Domain.Enums;

namespace JobScraper.Infrastructure.Scrapers;

public abstract class BaseJobScraper : IJobScraper
{
    protected readonly HttpClient _httpClient;
    protected readonly ILogger _logger;
    protected readonly IConfigurationService _config;

    protected BaseJobScraper(HttpClient httpClient, ILogger logger, IConfigurationService config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config;
        
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
    }

    public abstract JobSource Source { get; }

    public abstract Task<IEnumerable<RawJobOffer>> ScrapeJobsAsync(JobSearchCriteria criteria, CancellationToken cancellationToken = default);

    protected async Task<IDocument> GetDocumentAsync(string url, CancellationToken cancellationToken = default)
    {
        await Task.Delay(_config.RateLimitDelayMs, cancellationToken);
        
        var html = await _httpClient.GetStringAsync(url, cancellationToken);
        var config = AngleSharp.Configuration.Default;
        var context = BrowsingContext.New(config);
        return await context.OpenAsync(req => req.Content(html), cancellationToken);
    }

    protected static string CleanText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        return System.Text.RegularExpressions.Regex.Replace(text.Trim(), @"\s+", " ");
    }

    protected static string? ExtractTextSafely(IElement? element)
    {
        return element?.TextContent?.Trim();
    }

    protected static string? ExtractAttributeSafely(IElement? element, string attribute)
    {
        return element?.GetAttribute(attribute)?.Trim();
    }
}
