using OpenQA.Selenium;

namespace JobScraper.Infrastructure.Extensions;

public static class WebElementExtensions
{
    public static string SafeGetText(this IWebElement parent, string selector)
    {
        try
        {
            var element = parent.FindElement(By.CssSelector(selector));
            return element?.Text?.Trim() ?? string.Empty;
        }
        catch (NoSuchElementException)
        {
            return string.Empty;
        }
    }
    
    public static string SafeGetTextMultiple(this IWebElement parent, params string[] selectors)
    {
        foreach (var selector in selectors)
        {
            try
            {
                var element = parent.FindElement(By.CssSelector(selector));
                var text = element?.Text?.Trim();
                if (!string.IsNullOrEmpty(text))
                    return text;
            }
            catch (NoSuchElementException)
            {
                continue;
            }
        }
        return string.Empty;
    }
}