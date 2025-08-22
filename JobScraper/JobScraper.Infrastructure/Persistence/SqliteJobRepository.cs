using System.Text.Json;
using JobScraper.Domain.Entities;
using JobScraper.Domain.Enums;
using JobScraper.Domain.Interfaces;
using JobScraper.Domain.ValueObjects;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace JobScraper.Infrastructure.Persistence;

public class SqliteJobRepository : IJobRepository, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ILogger<SqliteJobRepository> _logger;

    public SqliteJobRepository(ILogger<SqliteJobRepository> logger, string connectionString = "Data Source=jobs_cache.db")
    {
        _logger = logger;
        _connection = new SqliteConnection(connectionString);
        _connection.Open();
        InitializeDatabase();
    }

    public async Task<bool> JobExistsAsync(string link, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM Jobs WHERE Link = @link";
        using var command = new SqliteCommand(sql, _connection);
        command.Parameters.AddWithValue("@link", link);
        
        var count = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(count) > 0;
    }

    public async Task<IEnumerable<JobOffer>> GetJobsByLinksAsync(IEnumerable<string> links, CancellationToken cancellationToken = default)
    {
        var linksList = links.ToList();
        if (!linksList.Any()) return [];

        var placeholders = string.Join(",", linksList.Select((_, i) => $"@link{i}"));
        var sql = $"SELECT * FROM Jobs WHERE Link IN ({placeholders})";
        
        using var command = new SqliteCommand(sql, _connection);
        for (int i = 0; i < linksList.Count; i++)
        {
            command.Parameters.AddWithValue($"@link{i}", linksList[i]);
        }

        var jobs = new List<JobOffer>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        while (await reader.ReadAsync(cancellationToken))
        {
            jobs.Add(MapReaderToJobOffer(reader));
        }

        return jobs;
    }

    public async Task SaveJobsAsync(IEnumerable<JobOffer> jobs, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT OR REPLACE INTO Jobs 
            (Link, Title, Company, Location, Source, PostedDate, ExpirationDate, SalaryJson, RequiredSkillsJson, RequiredYearsExperienceJson, RawTextSnapshot, IngestedAt)
            VALUES (@Link, @Title, @Company, @Location, @Source, @PostedDate, @ExpirationDate, @SalaryJson, @RequiredSkillsJson, @RequiredYearsExperienceJson, @RawTextSnapshot, @IngestedAt)
            """;

        using var transaction = _connection.BeginTransaction();
        try
        {
            foreach (var job in jobs)
            {
                using var command = new SqliteCommand(sql, _connection, transaction);
                command.Parameters.AddWithValue("@Link", job.Link);
                command.Parameters.AddWithValue("@Title", job.Title);
                command.Parameters.AddWithValue("@Company", job.Company ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Location", job.Location ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Source", job.Source.ToString());
                command.Parameters.AddWithValue("@PostedDate", job.PostedDate ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ExpirationDate", job.ExpirationDate ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SalaryJson", JsonSerializer.Serialize(job.Salary));
                command.Parameters.AddWithValue("@RequiredSkillsJson", JsonSerializer.Serialize(job.RequiredSkills));
                command.Parameters.AddWithValue("@RequiredYearsExperienceJson", JsonSerializer.Serialize(job.RequiredYearsExperience));
                command.Parameters.AddWithValue("@RawTextSnapshot", job.RawTextSnapshot ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@IngestedAt", job.IngestedAt);

                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Saved {Count} jobs to local cache", jobs.Count());
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private void InitializeDatabase()
    {
        const string createTableSql = """
            CREATE TABLE IF NOT EXISTS Jobs (
                Link TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                Company TEXT,
                Location TEXT,
                Source TEXT NOT NULL,
                PostedDate TEXT,
                ExpirationDate TEXT,
                SalaryJson TEXT,
                RequiredSkillsJson TEXT,
                RequiredYearsExperienceJson TEXT,
                RawTextSnapshot TEXT,
                IngestedAt TEXT NOT NULL
            )
            """;

        using var command = new SqliteCommand(createTableSql, _connection);
        command.ExecuteNonQuery();
    }

    private static JobOffer MapReaderToJobOffer(SqliteDataReader reader) =>
     new JobOffer
        {
            Link = reader.GetString(reader.GetOrdinal("Link")),
            Title = reader.GetString(reader.GetOrdinal("Title")),
            Company = reader.IsDBNull(reader.GetOrdinal("Company")) ? null : reader.GetString(reader.GetOrdinal("Company")),
            Location = reader.IsDBNull(reader.GetOrdinal("Location")) ? null : reader.GetString(reader.GetOrdinal("Location")),
            Source = Enum.Parse<JobSource>(reader.GetString(reader.GetOrdinal("Source"))),
            PostedDate = reader.IsDBNull(reader.GetOrdinal("PostedDate")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("PostedDate"))),
            ExpirationDate = reader.IsDBNull(reader.GetOrdinal("ExpirationDate")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("ExpirationDate"))),
            Salary = JsonSerializer.Deserialize<SalaryInfo>(reader.GetString(reader.GetOrdinal("SalaryJson"))),
            RequiredSkills = JsonSerializer.Deserialize<List<string>>(reader.GetString(reader.GetOrdinal("RequiredSkillsJson"))) ?? [],
            RequiredYearsExperience = JsonSerializer.Deserialize<YearsExperience>(reader.GetString(reader.GetOrdinal("RequiredYearsExperienceJson"))),
            RawTextSnapshot = reader.IsDBNull(reader.GetOrdinal("RawTextSnapshot")) ? null : reader.GetString(reader.GetOrdinal("RawTextSnapshot")),
            IngestedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("IngestedAt")))
        };
    


    public void Dispose()
    {
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }
}
