using JobScraper.Domain.Enums;

namespace JobScraper.Domain.ValueObjects;

public record SalaryInfo
{
    public string? RawText { get; init; }
    public decimal? Min { get; init; }
    public decimal? Max { get; init; }
    public string? Currency { get; init; }
    public SalaryPeriod? Period { get; init; }
    public GrossNet? GrossNet { get; init; }

    public static SalaryInfo FromText(string text) => new() { RawText = text };
    
    public static SalaryInfo FromStructured(decimal? min, decimal? max, string currency, 
        SalaryPeriod period = SalaryPeriod.Monthly, GrossNet grossNet = Enums.GrossNet.Unspecified) =>
        new()
        {
            Min = min,
            Max = max,
            Currency = currency,
            Period = period,
            GrossNet = grossNet
        };
}