using FluentAssertions;
using JobScraper.Application.Services;
using JobScraper.Domain.Entities;
using JobScraper.Domain.Enums;
using Xunit;

namespace JobScraper.Tests.Application;

public class DuplicateDetectorServiceTests
{
    private readonly DuplicateDetectorService _service;

    public DuplicateDetectorServiceTests()
    {
        _service = new DuplicateDetectorService();
    }

    [Fact]
    public void RemoveDuplicates_ShouldRemoveJobsWithSameLink()
    {
        // Arrange
        var jobs = new List<JobOffer>
        {
            CreateJobOffer("https://example.com/job1", "Software Engineer"),
            CreateJobOffer("https://example.com/job1", "Senior Software Engineer"), // Duplicate link
            CreateJobOffer("https://example.com/job2", "Backend Developer")
        };

        // Act
        var result = _service.RemoveDuplicates(jobs);

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainSingle(j => j.Link == "https://example.com/job1");
        result.Should().ContainSingle(j => j.Link == "https://example.com/job2");
    }

    [Fact]
    public void RemoveDuplicates_ShouldKeepJobsWithDifferentTitles()
    {
        // Arrange
        var jobs = new List<JobOffer>
        {
            CreateJobOffer("https://example.com/job1", "Software Engineer"),
            CreateJobOffer("https://example.com/job2", "Data Scientist"),
            CreateJobOffer("https://example.com/job3", "Product Manager")
        };

        // Act
        var result = _service.RemoveDuplicates(jobs);

        // Assert
        result.Should().HaveCount(3);
    }

    private static JobOffer CreateJobOffer(string link, string title, string? company = null)
    {
        return new JobOffer
        {
            Link = link,
            Title = title,
            Company = company,
            Source = JobSource.NoFluffJobs,
            RequiredSkills = []
        };
    }
}
