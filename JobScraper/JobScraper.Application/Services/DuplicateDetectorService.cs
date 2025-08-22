using JobScraper.Domain.Entities;
using JobScraper.Domain.Interfaces;

namespace JobScraper.Application.Services;

public class DuplicateDetectorService : IDuplicateDetector
{
    public IEnumerable<JobOffer> RemoveDuplicates(IEnumerable<JobOffer> jobs)
    {
        var seen = new HashSet<string>();
        var uniqueJobs = new List<JobOffer>();

        foreach (var job in jobs)
        {
            // Primary deduplication by link
            if (seen.Add(job.Link))
            {
                uniqueJobs.Add(job);
            }
        }

        // Optional: Add fuzzy matching for title + company
        return RemoveFuzzyDuplicates(uniqueJobs);
    }

    private static IEnumerable<JobOffer> RemoveFuzzyDuplicates(IEnumerable<JobOffer> jobs)
    {
        var jobsList = jobs.ToList();
        var duplicateIndices = new HashSet<int>();

        for (int i = 0; i < jobsList.Count; i++)
        {
            if (duplicateIndices.Contains(i)) continue;

            for (int j = i + 1; j < jobsList.Count; j++)
            {
                if (duplicateIndices.Contains(j)) continue;

                if (AreJobsSimilar(jobsList[i], jobsList[j]))
                {
                    duplicateIndices.Add(j);
                }
            }
        }

        return jobsList.Where((_, index) => !duplicateIndices.Contains(index));
    }

    private static bool AreJobsSimilar(JobOffer job1, JobOffer job2)
    {
        // Simple similarity check
        var titleSimilarity = CalculateJaccardSimilarity(job1.Title, job2.Title);
        var companySimilarity = job1.Company != null && job2.Company != null 
            ? CalculateJaccardSimilarity(job1.Company, job2.Company) 
            : 0.0;

        return titleSimilarity > 0.8 && (companySimilarity > 0.8 || string.IsNullOrEmpty(job1.Company) || string.IsNullOrEmpty(job2.Company));
    }

    private static double CalculateJaccardSimilarity(string text1, string text2)
    {
        var words1 = text1.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var words2 = text2.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        
        var intersection = words1.Intersect(words2).Count();
        var union = words1.Union(words2).Count();
        
        return union == 0 ? 0.0 : (double)intersection / union;
    }
}
