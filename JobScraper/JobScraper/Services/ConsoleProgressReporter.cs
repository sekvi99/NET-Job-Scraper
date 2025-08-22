namespace JobScraper.Services;

using JobScraper.Domain.Interfaces;
using JobScraper.Domain.DTOs;

public class ConsoleProgressReporter : IProgressReporter
{
    private readonly object _lock = new();
    private string? _lastActivity;

    public void ReportProgress(ScrapingProgress progress)
    {
        lock (_lock)
        {
            // Only update if activity changed to reduce console spam
            if (progress.CurrentActivity != _lastActivity)
            {
                Console.WriteLine($"🔄 {progress.CurrentSource}: {progress.CurrentActivity}");
                _lastActivity = progress.CurrentActivity;
            }

            if (progress.TotalFound > 0 && progress.Processed > 0)
            {
                var percentage = (double)progress.Processed / progress.TotalFound * 100;
                Console.Write($"\r   Progress: {progress.Processed}/{progress.TotalFound} ({percentage:F1}%) ");
                
                if (progress.Failed > 0)
                {
                    Console.Write($"[{progress.Failed} failed] ");
                }
            }
        }
    }
}
