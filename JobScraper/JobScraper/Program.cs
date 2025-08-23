using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using JobScraper.Infrastructure;
using JobScraper.Application.UseCases;
using JobScraper.Domain.Interfaces;
using JobScraper.Domain.DTOs;
using JobScraper.Domain.Enums;
using JobScraper.Services;
using DotNetEnv;

// Load .env file
Env.Load();

// Build configuration early for Serilog
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/jobscraper-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var host = CreateHostBuilder(args, configuration).Build();
    var rootCommand = CreateRootCommand(host.Services);
    return await rootCommand.InvokeAsync(args);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration) =>
    Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureServices((hostContext, services) =>
        {
            services.AddSingleton(configuration);
            services.AddInfrastructure(configuration);
            services.AddScoped<ScrapeJobsUseCase>();
            services.AddScoped<IProgressReporter, ConsoleProgressReporter>();
        });

static RootCommand CreateRootCommand(IServiceProvider services)
{
    var titlesOption = new Option<string[]>(
        aliases: ["--titles", "-t"],
        description: "Job titles to search for (comma-separated)",
        parseArgument: result => result.Tokens.Select(t => t.Value).ToArray())
    { IsRequired = true };

    var locationsOption = new Option<string[]>(
        aliases: ["--locations", "-l"],
        description: "Locations to search in (comma-separated, optional)")
    { IsRequired = false };

    var seniorityOption = new Option<string[]>(
        aliases: ["--seniority", "-s"],
        description: "Seniority levels: junior, mid, senior, lead (comma-separated, optional)")
    { IsRequired = false };

    var dateFromOption = new Option<DateTime?>(
        aliases: ["--dateFrom", "-d"],
        description: "Filter jobs posted from this date (YYYY-MM-DD, optional)")
    { IsRequired = false };

    var maxOption = new Option<int?>(
        aliases: ["--max", "-m"],
        description: "Maximum number of jobs per site (optional, default from config)")
    { IsRequired = false };

    var rootCommand = new RootCommand("Job Scraper - Scrape jobs from multiple sites and normalize with AI")
    {
        titlesOption,
        locationsOption,
        seniorityOption,
        dateFromOption,
        maxOption
    };

    rootCommand.SetHandler(async (titles, locations, seniority, dateFrom, max) =>
    {
        using var scope = services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ScrapeJobsUseCase>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            var criteria = new JobSearchCriteria
            {
                Titles = ParseCommaSeparated(titles),
                Locations = locations?.Length > 0 ? ParseCommaSeparated(locations) : null,
                Seniorities = seniority?.Length > 0 ? ParseSeniorities(seniority) : null,
                DateFrom = dateFrom,
                MaxPerSite = max
            };

            logger.LogInformation("Starting job scraping with criteria: {@Criteria}", criteria);
            Console.WriteLine("🚀 Starting job scraping...");
            Console.WriteLine($"📝 Titles: {string.Join(", ", criteria.Titles)}");
            if (criteria.Locations != null) Console.WriteLine($"📍 Locations: {string.Join(", ", criteria.Locations)}");
            if (criteria.Seniorities != null) Console.WriteLine($"👔 Seniority: {string.Join(", ", criteria.Seniorities)}");
            if (criteria.DateFrom != null) Console.WriteLine($"📅 From date: {criteria.DateFrom:yyyy-MM-dd}");
            Console.WriteLine();

            var result = await useCase.ExecuteAsync(criteria, CancellationToken.None);

            // Print results
            Console.WriteLine("\n🎉 Scraping completed!");
            Console.WriteLine($"📊 Results Summary:");
            Console.WriteLine($"   • Total found: {result.TotalFound}");
            Console.WriteLine($"   • Processed: {result.Processed}");
            Console.WriteLine($"   • Failed: {result.Failed}");
            Console.WriteLine($"   • Skipped (duplicates): {result.Skipped}");
            Console.WriteLine($"   • Saved to sheets: {result.SavedToSheets}");

            if (result.ProcessedBySite.Any())
            {
                Console.WriteLine($"\n📈 By Site:");
                foreach (var (site, count) in result.ProcessedBySite)
                {
                    Console.WriteLine($"   • {site}: {count} jobs");
                }
            }

            if (result.FailedSites.Any())
            {
                Console.WriteLine($"\n❌ Failed Sites: {string.Join(", ", result.FailedSites)}");
            }

            if (result.SheetsSaveFailed)
            {
                Console.WriteLine("⚠️  Warning: Failed to save to Google Sheets");
            }

            logger.LogInformation("Job scraping completed successfully: {@Result}", result);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\n🛑 Operation cancelled by user");
            logger.LogInformation("Job scraping cancelled by user");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n💥 Error: {ex.Message}");
            logger.LogError(ex, "Job scraping failed");
            throw;
        }
    }, titlesOption, locationsOption, seniorityOption, dateFromOption, maxOption);

    return rootCommand;
}

static List<string> ParseCommaSeparated(string[] input)
{
    return input.SelectMany(s => s.Split(',', StringSplitOptions.RemoveEmptyEntries))
               .Select(s => s.Trim())
               .Where(s => !string.IsNullOrEmpty(s))
               .ToList();
}

static List<Seniority> ParseSeniorities(string[] input)
{
    var seniorities = new List<Seniority>();
    var seniorityStrings = ParseCommaSeparated(input);

    foreach (var seniorityString in seniorityStrings)
    {
        if (Enum.TryParse<Seniority>(seniorityString, true, out var seniority))
        {
            seniorities.Add(seniority);
        }
        else
        {
            Console.WriteLine($"⚠️  Warning: Unknown seniority level '{seniorityString}'. Valid values: junior, mid, senior, lead");
        }
    }

    return seniorities;
}