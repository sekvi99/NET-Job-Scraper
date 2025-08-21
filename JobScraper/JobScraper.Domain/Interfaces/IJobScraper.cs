using JobScraper.Domain.DTOs;
using JobScraper.Domain.Entities;
using JobScraper.Domain.Enums;

namespace JobScraper.Domain.Interfaces;

public interface IJobScraper
{
    JobSource Source { get; }
    Task<IEnumerable<RawJobOffer>> ScrapeJobsAsync(JobSearchCriteria criteria, CancellationToken cancellationToken = default);
}