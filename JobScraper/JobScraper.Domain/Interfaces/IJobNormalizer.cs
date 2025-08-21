using JobScraper.Domain.DTOs;
using JobScraper.Domain.Entities;

namespace JobScraper.Domain.Interfaces;

public interface IJobNormalizer
{
    Task<NormalizedJobData?> NormalizeJobAsync(RawJobOffer rawJob, CancellationToken cancellationToken = default);
}