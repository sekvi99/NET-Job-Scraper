namespace JobScraper.Infrastructure.Options;

public class GoogleSheetsOptions
{
    public string? SpreadsheetId { get; set; }
    public string? SheetName { get; set; }
    public string? CredentialsPath { get; set; }
}