namespace JobScraper.Infrastructure.Extensions;

public static class StringExtensions
{
    public static string Truncate(this string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        
        return text[..(maxLength - 3)] + "...";
    }
}
