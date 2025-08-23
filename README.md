# Job Scraper - AI-Powered Job Aggregation Tool

A comprehensive .NET 9 console application that scrapes job offers from multiple Polish job sites, normalizes the data using OpenAI GPT-4o-mini, and exports results to Sqlite database.

## 🚀 Features

- **Multi-site scraping**: NoFluffJobs, Pracuj.pl, and JustJoin.it
- **AI-powered normalization**: Uses OpenAI GPT-4o-mini to extract structured data
- **Clean Architecture**: Proper separation of concerns with DI container
- **SQLite caching**: Local persistence to avoid re-requests
- **Robust error handling**: Retry policies, rate limiting, and graceful failures
- **Progress reporting**: Real-time console feedback
- **Flexible filtering**: By titles, locations, seniority, and date ranges

## 📋 Prerequisites

- .NET 8 SDK
- OpenAI API key
- Internet connection for scraping

## 🛠 Setup

### 1. Clone and Build

```bash
git clone <repository-url>
cd JobScraper
dotnet restore
dotnet build
```

### 2. Configuration Files

Create the following configuration files:

#### `appsettings.json`
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  },
  "JobScraper": {
    "DefaultMaxPerSite": 50,
    "RateLimitDelayMs": 1000,
    "MaxRetries": 3
  }
}
```

#### `.env.example` (copy to `.env` and fill in values)
```bash
# OpenAI Configuration
OPENAI_API_KEY=your_openai_api_key_here

# Sqlite Configuration
SQLITE_CONNECTION_STRING=your_sqlite_connection_string
```

### 3. OpenAI Setup

1. Get API key from [OpenAI Platform](https://platform.openai.com/api-keys)
2. Set in environment variable `OPENAI_API_KEY`

## 🚀 Usage

### Basic Usage

```bash
# Navigate to CLI project
cd src/JobScraper.Presentation.Cli

# Run with basic parameters
dotnet run -- --titles "software engineer .net,angular developer"

# With location filtering
dotnet run -- --titles ".net developer" --locations "Poland,Remote"

# With seniority and date filtering
dotnet run -- --titles "backend .net" --seniority "mid,senior" --dateFrom 2025-08-01

# Limit results per site
dotnet run -- --titles "full stack developer" --max 25
```

### Command Line Options

| Option | Alias | Description | Required |
|--------|-------|-------------|----------|
| `--titles` | `-t` | Job titles (comma-separated) | ✅ |
| `--locations` | `-l` | Locations (comma-separated) | ❌ |
| `--seniority` | `-s` | Seniority levels: junior, mid, senior, lead | ❌ |
| `--dateFrom` | `-d` | Filter jobs from date (YYYY-MM-DD) | ❌ |
| `--max` | `-m` | Max jobs per site | ❌ |

### Example Commands

```bash
# Search for .NET developers in Warsaw
dotnet run -- --titles ".net developer,c# developer" --locations "Warsaw" --max 30

# Senior positions only
dotnet run -- --titles "software architect,tech lead" --seniority "senior,lead"

# Recent postings only
dotnet run -- --titles "react developer" --dateFrom 2025-08-15

# Multiple titles and locations
dotnet run -- --titles "frontend developer,angular developer,react developer" --locations "Poland,Remote,Krakow"
```

## 📊 Output

The application creates new entries in sqlite database

## 🏗 Architecture

The project follows Clean Architecture principles:

```
JobScraper/
├── src/
│   ├── JobScraper.Domain/           # Entities, Value Objects, Interfaces
│   ├── JobScraper.Application/      # Use Cases, Services, DTOs
│   ├── JobScraper.Infrastructure/   # External services, Persistence, HTTP clients
│   └── JobScraper.Presentation.Cli/ # Console app, Commands, Progress reporting
└── tests/
    └── JobScraper.Tests/            # Unit and integration tests
```

### Key Components

- **Scrapers**: Site-specific implementations for job extraction
- **AI Normalizer**: OpenAI GPT-4o-mini integration for data structuring
- **Duplicate Detector**: Link-based and fuzzy matching deduplication
- **Progress Reporter**: Real-time console feedback
- **SQLite Repository**: Local caching for performance

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "DuplicateDetectorServiceTests"
```

## 🔧 Development

### Adding New Job Sites

1. Create new scraper class inheriting from `BaseJobScraper`
2. Implement site-specific parsing logic
3. Register in `DependencyInjection.cs`
4. Add corresponding tests

### Customizing AI Prompts

Modify the prompt in `OpenAIJobNormalizer.cs` to adjust extraction behavior:
- Add new fields to extract
- Change skill extraction logic
- Modify salary parsing rules

### Rate Limiting

Configure delays and retries in `appsettings.json`:
```json
{
  "JobScraper": {
    "RateLimitDelayMs": 2000,  // 2 second delay between requests
    "MaxRetries": 5            // 5 retry attempts
  }
}
```

## ⚠️ Limitations & Considerations

### Site Coverage
- **NoFluffJobs**: Full support with job details
- **Pracuj.pl**: Basic listing support (details page scraping can be added)
- **JustJoin.it**: Basic listing support (may need updates due to SPA nature)

### Rate Limits
- Respects `robots.txt` where applicable
- Implements polite delays between requests
- Uses exponential backoff for retries

### Legal & Ethical
- Only scrapes publicly available job listings
- Respects site terms of service
- Does not store personal/sensitive information
- Implements reasonable rate limiting

### AI Processing
- OpenAI API costs apply (typically $0.001-0.01 per job)
- Requires internet connection
- May occasionally fail to parse complex job descriptions

## 🐛 Troubleshooting

### Common Issues

**"OpenAI API Key not configured"**
- Set `OPENAI_API_KEY` environment variable
- Or add to `appsettings.json` under `JobScraper:OpenAiApiKey`

**"No jobs found"**
- Try broader search terms
- Check if sites are accessible
- Verify network connectivity

**Rate limiting errors**
- Increase `RateLimitDelayMs` in configuration
- Reduce `MaxPerSite` to process fewer jobs
- Check site-specific rate limits

### Debug Mode

```bash
# Run in development mode for verbose logging
DOTNET_ENVIRONMENT=Development dotnet run -- --titles "developer"

# Check log files
tail -f logs/jobscraper-*.log
```

## 📝 License

[Add your license here]

## 🤝 Contributing

1. Fork the repository
2. Create feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open Pull Request

## 📞 Support

- Create an issue for bug reports
- Check existing issues for known problems
- Review logs in `logs/` directory for detailed error information