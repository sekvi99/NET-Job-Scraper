using JobScraper.Domain.Entities;

namespace JobScraper.Domain.Interfaces;

public interface IDuplicateDetector
{
    IEnumerable<JobOffer> RemoveDuplicates(IEnumerable<JobOffer> jobs);
}