using JobScraper.Domain.Entities;

namespace JobScraper.Domain.Interfaces;

public interface IGoogleSheetsService
{
    Task AppendJobsAsync(IEnumerable<JobOffer> jobs, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetExistingLinksAsync(CancellationToken cancellationToken = default);
}