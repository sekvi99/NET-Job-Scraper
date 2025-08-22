using System.Globalization;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using JobScraper.Domain.Entities;
using JobScraper.Domain.Interfaces;
using JobScraper.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace JobScraper.Infrastructure.Scrapers;

public class GoogleSheetsService : IGoogleSheetsService
{
    private readonly SheetsService _service;
    private readonly IConfigurationService _config;
    private readonly ILogger<GoogleSheetsService> _logger;
    
    private static readonly string[] Headers = 
    {
        "Source", "Link", "Title", "Company", "Location", "Salary", 
        "RequiredYearsExperience", "RequiredSkills", "PostedDate", "ExpirationDate", "IngestedAt"
    };

    public GoogleSheetsService(IConfigurationService config, ILogger<GoogleSheetsService> logger)
    {
        _config = config;
        _logger = logger;
        
        _service = new SheetsService(new BaseClientService.Initializer
        {
            ApiKey = config.GoogleApiKey,
            ApplicationName = "JobScraper"
        });
    }

    public async Task AppendJobsAsync(IEnumerable<JobOffer> jobs, CancellationToken cancellationToken = default)
    {
        var jobsList = jobs.ToList();
        if (!jobsList.Any()) return;

        try
        {
            // NOTE: API Key authentication doesn't support write operations
            // You'll need to use Service Account or OAuth2 for writing
            throw new NotSupportedException("API Key authentication only supports read operations. Use Service Account authentication for write operations.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to append jobs to Google Sheets");
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetExistingLinksAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // This will only work if the spreadsheet is publicly accessible
            var range = $"{_config.GoogleSheetsSheetName}!B:B"; // Link column
            var request = _service.Spreadsheets.Values.Get(_config.GoogleSheetsSpreadsheetId, range);
            var response = await request.ExecuteAsync(cancellationToken);

            if (response.Values == null || !response.Values.Any())
                return [];

            // Skip header row
            return response.Values.Skip(1)
                .Where(row => row.Count > 0 && row[0] != null)
                .Select(row => row[0].ToString()!)
                .Where(link => !string.IsNullOrEmpty(link))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get existing links from Google Sheets");
            return [];
        }
    }

    // Remove all the private methods that involve writing since API Key doesn't support it
    private static IList<object> MapJobToRow(JobOffer job)
    {
        return new List<object>
        {
            job.Source.ToString(),
            job.Link,
            job.Title,
            job.Company ?? string.Empty,
            job.Location ?? string.Empty,
            FormatSalary(job.Salary),
            FormatYearsExperience(job.RequiredYearsExperience),
            string.Join(", ", job.RequiredSkills),
            job.PostedDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
            job.ExpirationDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
            job.IngestedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
        };
    }

    private static string FormatSalary(SalaryInfo? salary)
    {
        if (salary == null) return string.Empty;
        
        if (!string.IsNullOrEmpty(salary.RawText))
            return salary.RawText;
        
        if (salary.Min.HasValue && salary.Max.HasValue && salary.Currency != null)
            return $"{salary.Min}-{salary.Max} {salary.Currency}";
        
        return string.Empty;
    }

    private static string FormatYearsExperience(YearsExperience? experience)
    {
        if (experience == null) return string.Empty;
        
        if (!string.IsNullOrEmpty(experience.RawText))
            return experience.RawText;
        
        if (experience.Min.HasValue && experience.Max.HasValue)
            return experience.Min == experience.Max ? $"{experience.Min} years" : $"{experience.Min}-{experience.Max} years";
        
        return string.Empty;
    }
}