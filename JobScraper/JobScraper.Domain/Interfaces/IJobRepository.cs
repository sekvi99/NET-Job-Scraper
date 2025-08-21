using JobScraper.Domain.Entities;

namespace JobScraper.Domain.Interfaces;

public interface IJobRepository
{
    Task SaveJobsAsync(IEnumerable<JobOffer> jobs, CancellationToken cancellationToken = default);
    Task<bool> JobExistsAsync(string link, CancellationToken cancellationToken = default);
    Task<IEnumerable<JobOffer>> GetJobsByLinksAsync(IEnumerable<string> links, CancellationToken cancellationToken = default);
}