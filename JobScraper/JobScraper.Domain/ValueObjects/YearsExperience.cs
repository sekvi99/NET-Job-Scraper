namespace JobScraper.Domain.ValueObjects;

public record YearsExperience
{
    public string? RawText { get; init; }
    public int? Min { get; init; }
    public int? Max { get; init; }

    public static YearsExperience FromText(string text) => new() { RawText = text };
    public static YearsExperience FromNumber(int years) => new() { Min = years, Max = years };
    public static YearsExperience FromRange(int min, int max) => new() { Min = min, Max = max };
}