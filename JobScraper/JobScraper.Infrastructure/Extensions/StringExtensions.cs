using System.Text.RegularExpressions;

namespace JobScraper.Infrastructure.Extensions;

public static class StringExtensions
{
    public static string Truncate(this string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        
        return text[..(maxLength - 3)] + "...";
    }
    
    public static string RegexReplace(this string input, string pattern, string replacement) =>
     Regex.Replace(input, pattern, replacement);

    public static string CleanText(this string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        
        return text.Trim()
            .Replace("\n", " ")
            .Replace("\r", "")
            .Replace("\t", " ")
            .RegexReplace(@"\s+", " ");
    }
}
