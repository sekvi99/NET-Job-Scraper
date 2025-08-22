using System.Text.Json;
using JobScraper.Domain.DTOs;
using JobScraper.Domain.Entities;
using JobScraper.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;

namespace JobScraper.Infrastructure.Scrapers;

public class OpenAIJobNormalizer : IJobNormalizer
{
    private readonly OpenAIClient _client;
    private readonly ILogger<OpenAIJobNormalizer> _logger;
    private const string SystemPrompt = "You are a strict information extractor. Output only valid JSON matching the provided JSON Schema. If a field is missing in the source, use `null` or an empty array as appropriate. Do not invent data.";

    private const string JsonSchema = """
    {
      "type": "object",
      "required": ["Title","Link","Salary","RequiredYearsExperience","RequiredSkills","ExpirationDate","Company","Location","Source","PostedDate"],
      "properties": {
        "Title": {"type":"string"},
        "Link": {"type":"string","format":"uri"},
        "Salary": {
          "oneOf": [
            {"type":"string"},
            {"type":"object","properties":{
              "min":{"type":"number"},
              "max":{"type":"number"},
              "currency":{"type":"string"},
              "period":{"type":"string","enum":["monthly","yearly","hourly"]},
              "grossNet":{"type":"string","enum":["gross","net","unspecified"]}
            }, "required":["currency"]}
          ]
        },
        "RequiredYearsExperience": {"oneOf":[{"type":"number"},{"type":"string"}]},
        "RequiredSkills": {"type":"array","items":{"type":"string"}},
        "ExpirationDate": {"oneOf":[{"type":"string","format":"date"},{"type":"null"}]},
        "Company": {"oneOf":[{"type":"string"},{"type":"null"}]},
        "Location": {"oneOf":[{"type":"string"},{"type":"null"}]},
        "Source": {"type":"string"},
        "PostedDate": {"oneOf":[{"type":"string","format":"date"},{"type":"null"}]}
      }
    }
    """;

    public OpenAIJobNormalizer(IConfigurationService config, ILogger<OpenAIJobNormalizer> logger)
    {
        _client = new OpenAIClient(config.OpenAiApiKey);
        _logger = logger;
    }

    public async Task<NormalizedJobData?> NormalizeJobAsync(RawJobOffer rawJob, CancellationToken cancellationToken = default)
    {
        try
        {
            var prompt = BuildPrompt(rawJob);
            _logger.LogDebug("Normalizing job with OpenAI: {Link}", rawJob.Link);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(SystemPrompt),
                new UserChatMessage(prompt)
            };

            var chatRequest = new ChatCompletionOptions
            {
                Temperature = 0.0f,
            };

            var response = await _client.GetChatClient("gpt-4o-mini").CompleteChatAsync(messages, chatRequest, cancellationToken);
            var jsonContent = response.Value.Content[0].Text;

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                _logger.LogWarning("Empty response from OpenAI for job: {Link}", rawJob.Link);
                return null;
            }

            var normalizedData = JsonSerializer.Deserialize<NormalizedJobData>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogDebug("Successfully normalized job: {Link}", rawJob.Link);
            return normalizedData;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse OpenAI JSON response for job: {Link}", rawJob.Link);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to normalize job with OpenAI: {Link}", rawJob.Link);
            return null;
        }
    }

    private static string BuildPrompt(RawJobOffer rawJob)
    {
        return $"""
        SourceSite: {rawJob.Source}
        URL: {rawJob.Link}

        RawFields:
        Title: {rawJob.ScrapedTitle}
        Company: {rawJob.ScrapedCompany}
        Location: {rawJob.ScrapedLocation}
        SalaryText: {rawJob.ScrapedSalaryText}
        ExpirationText: {rawJob.ScrapedExpirationText}
        PostedText: {rawJob.ScrapedPostedText}

        Description:
        {rawJob.CleanedDescription}

        JSON Schema:
        {JsonSchema}

        Rules:
        - Parse salary ranges and currency when possible; otherwise return the original salary text as a string.
        - Infer years of experience from phrases like "2+ years", "mid", etc. If unclear, null.
        - Extract skills as an array of canonical tech names (e.g., ".NET", "C#", "Angular", "Azure", "SQL").
        - Dates in ISO 8601 (YYYY-MM-DD).
        - Return JSON only.
        """;
    }
}
