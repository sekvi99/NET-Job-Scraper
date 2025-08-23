using JobScraper.Domain.DTOs;
using JobScraper.Domain.Entities;
using JobScraper.Domain.Enums;

namespace JobScraper.Tests.TestData;

public static class MockJobData
{
    public static RawJobOffer CreateRawJobOffer(
        string link = "https://example.com/job1",
        string title = "Software Engineer",
        string? company = "TechCorp",
        JobSource source = JobSource.NoFluffJobs)
    {
        return new RawJobOffer
        {
            Link = link,
            ScrapedTitle = title,
            ScrapedCompany = company,
            ScrapedLocation = "Warsaw, Poland",
            ScrapedSalaryText = "5000-7000 PLN",
            CleanedDescription = "We are looking for a talented software engineer...",
            Source = source
        };
    }

    public static JobOffer CreateJobOffer(
        string link = "https://example.com/job1",
        string title = "Software Engineer",
        string? company = "TechCorp",
        JobSource source = JobSource.NoFluffJobs)
    {
        return new JobOffer
        {
            Link = link,
            Title = title,
            Company = company,
            Source = source,
            RequiredSkills = [".NET", "C#", "SQL"],
            Location = "Warsaw, Poland"
        };
    }

    public static NormalizedJobData CreateNormalizedJobData(
        string link = "https://example.com/job1",
        string title = "Software Engineer",
        string source = "NoFluffJobs")
    {
        return new NormalizedJobData
        {
            Link = link,
            Title = title,
            Source = source,
            RequiredSkills = [".NET", "C#", "SQL"],
            Company = "TechCorp",
            Location = "Warsaw, Poland",
            Salary = "5000-7000 PLN",
            RequiredYearsExperience = 3,
            PostedDate = DateTime.Today.AddDays(-1),
            ExpirationDate = DateTime.Today.AddDays(30)
        };
    }

    public static JobSearchCriteria CreateJobSearchCriteria(
        string[] titles = null,
        string[]? locations = null,
        int? maxPerSite = null)
    {
        return new JobSearchCriteria
        {
            Titles = (titles ?? ["Software Engineer", ".NET Developer"]).ToList(),
            Locations = locations?.ToList(),
            MaxPerSite = maxPerSite ?? 50
        };
    }
}
