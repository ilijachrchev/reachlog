using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReachLog.Application.DTOs.Scraper;
using ReachLog.Application.Interfaces;
using ReachLog.Domain.Entities;
using ReachLog.Infrastructure.Persistence;

namespace ReachLog.Infrastructure.Services;

public class ScraperService : IScraperService
{
    private readonly AppDbContext _db;
    private readonly IApifyService _apifyService;
    private readonly ICvService _cvService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _claudeApiKey;
    private readonly string _claudeModel;
    private readonly ILogger<ScraperService> _logger;

    public ScraperService(
        AppDbContext db,
        IApifyService apifyService,
        ICvService cvService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ScraperService> logger)
    {
        _db = db;
        _apifyService = apifyService;
        _cvService = cvService;
        _httpClientFactory = httpClientFactory;
        _claudeApiKey = configuration["Claude:ApiKey"] ?? string.Empty;
        _claudeModel = configuration["Claude:Model"] ?? string.Empty;
        _logger = logger;
    }

    public async Task RunAsync(ScrapeRequestDto request, Guid userId)
    {
        var keywords = request.Keywords ?? ".NET Angular internship";
        var allResults = new List<ScrapedJobDto>();

        if (request.Countries?.Count > 0)
        {
            foreach (var country in request.Countries)
            {
                var results = await _apifyService.ScrapeLinkedInAsync(keywords, country, 1, userId);
                allResults.AddRange(results);
            }
        }
        else
        {
            var wave1Locations = new[] { "Ljubljana", "Koper", "Maribor" };
            foreach (var loc in wave1Locations)
            {
                var results = await _apifyService.ScrapeLinkedInAsync(keywords, loc, 1, userId);
                allResults.AddRange(results);
            }

            if (allResults.Count < 20)
            {
                var wave2Locations = new[] { "Vienna", "Milan", "Trieste", "Zagreb", "Munich", "Berlin" };
                foreach (var loc in wave2Locations)
                {
                    var results = await _apifyService.ScrapeLinkedInAsync(keywords, loc, 2, userId);
                    allResults.AddRange(results);
                }
            }

            if (allResults.Count < 50)
            {
                var results = await _apifyService.ScrapeLinkedInAsync(keywords, "remote Europe", 3, userId);
                allResults.AddRange(results);
            }

            {
                var results = await _apifyService.ScrapeLinkedInAsync(keywords, "remote", 4, userId);
                allResults.AddRange(results);
            }
        }

        var existingUrls = await _db.ScrapedJobs
            .Where(j => j.UserId == userId)
            .Select(j => j.ExternalUrl)
            .ToHashSetAsync();

        var newJobs = allResults
            .Where(j => !string.IsNullOrWhiteSpace(j.ExternalUrl) && !existingUrls.Contains(j.ExternalUrl))
            .GroupBy(j => j.ExternalUrl)
            .Select(g => g.First())
            .ToList();

        if (newJobs.Count == 0) return;

        var entities = newJobs.Select(dto => new ScrapedJob
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = dto.Title,
            Company = dto.Company,
            Location = dto.Location,
            Country = dto.Country,
            IsRemote = dto.IsRemote,
            JobBoard = dto.JobBoard,
            ExternalUrl = dto.ExternalUrl,
            Description = dto.Description,
            SalaryMin = dto.SalaryMin,
            SalaryMax = dto.SalaryMax,
            Currency = dto.Currency,
            JobType = dto.JobType,
            Wave = dto.Wave,
            PostedAt = dto.PostedAt,
            ScrapedAt = dto.ScrapedAt,
            IsImported = false
        }).ToList();

        _db.ScrapedJobs.AddRange(entities);
        await _db.SaveChangesAsync();

        var cv = await _cvService.GetAsync(userId);
        if (cv != null && !string.IsNullOrWhiteSpace(_claudeApiKey))
        {
            foreach (var entity in entities)
            {
                if (string.IsNullOrWhiteSpace(entity.Description)) continue;

                try
                {
                    var (score, missing) = await ScoreJobAsync(cv.ExtractedText, entity.Title, entity.Company, entity.Description!);
                    entity.MatchScore = score;
                    entity.MissingSkills = JsonSerializer.Serialize(missing);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Auto-scoring failed for job {JobId}.", entity.Id);
                }
            }

            await _db.SaveChangesAsync();
        }
    }

    public async Task<List<ScrapedJobDto>> GetJobsAsync(Guid userId, string? jobType, bool? remoteOnly, int? minScore)
    {
        var query = _db.ScrapedJobs.Where(j => j.UserId == userId);

        if (!string.IsNullOrWhiteSpace(jobType))
            query = query.Where(j => j.JobType == jobType);

        if (remoteOnly == true)
            query = query.Where(j => j.IsRemote);

        if (minScore.HasValue)
            query = query.Where(j => j.MatchScore >= minScore);

        var jobs = await query
            .OrderByDescending(j => j.MatchScore)
            .ThenByDescending(j => j.ScrapedAt)
            .ToListAsync();

        return jobs.Select(MapToDto).ToList();
    }

    public async Task<ScrapedJobDto> ImportJobAsync(Guid jobId, Guid userId)
    {
        var job = await _db.ScrapedJobs
            .FirstOrDefaultAsync(j => j.Id == jobId && j.UserId == userId)
            ?? throw new KeyNotFoundException("Job not found.");

        var outreach = new Outreach
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CompanyName = job.Company,
            Role = job.Title,
            ContactName = string.Empty,
            ContactEmail = string.Empty,
            Channel = job.JobBoard,
            RawMessage = job.ExternalUrl,
            Status = "Sent",
            SentAt = job.PostedAt ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.Outreaches.Add(outreach);
        job.IsImported = true;
        job.ImportedOutreachId = outreach.Id;

        await _db.SaveChangesAsync();

        return MapToDto(job);
    }

    public Task<ScraperStatusDto> GetStatusAsync(Guid userId)
    {
        return Task.FromResult(new ScraperStatusDto
        {
            IsRunning = false,
            Wave = 0,
            TotalFound = 0
        });
    }

    private async Task<(int score, List<string> missing)> ScoreJobAsync(
        string cvText, string title, string company, string description)
    {
        var prompt = "You are a recruitment assistant. Compare the candidate's CV against the job description and return a match score.\n\n" +
            "CV:\n" + cvText + "\n\n" +
            "Job Title: " + title + "\n" +
            "Company: " + company + "\n" +
            "Job Description:\n" + description + "\n\n" +
            "Return ONLY valid JSON with these exact keys:\n" +
            "- matchScore: integer 0-100 representing how well the CV matches the job\n" +
            "- missingSkills: array of strings listing skills the job requires that are missing from the CV\n\n" +
            "No markdown, no explanation, just JSON.";

        var requestBody = new
        {
            model = _claudeModel,
            max_tokens = 1024,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        var httpClient = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", _claudeApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(responseBody);
        var content = json.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString()!;

        content = content.Trim();
        if (content.StartsWith("```"))
        {
            content = content.Substring(content.IndexOf('\n') + 1);
            content = content.Substring(0, content.LastIndexOf("```")).Trim();
        }

        var result = JsonSerializer.Deserialize<ScoreResultInternal>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return (result?.MatchScore ?? 0, result?.MissingSkills ?? []);
    }

    private static ScrapedJobDto MapToDto(ScrapedJob job)
    {
        List<string> missingSkills = [];
        if (!string.IsNullOrWhiteSpace(job.MissingSkills))
        {
            try { missingSkills = JsonSerializer.Deserialize<List<string>>(job.MissingSkills) ?? []; }
            catch { }
        }

        return new ScrapedJobDto
        {
            Id = job.Id,
            UserId = job.UserId,
            Title = job.Title,
            Company = job.Company,
            Location = job.Location,
            Country = job.Country,
            IsRemote = job.IsRemote,
            JobBoard = job.JobBoard,
            ExternalUrl = job.ExternalUrl,
            Description = job.Description,
            SalaryMin = job.SalaryMin,
            SalaryMax = job.SalaryMax,
            Currency = job.Currency,
            JobType = job.JobType,
            Wave = job.Wave,
            PostedAt = job.PostedAt,
            ScrapedAt = job.ScrapedAt,
            IsImported = job.IsImported,
            ImportedOutreachId = job.ImportedOutreachId,
            MatchScore = job.MatchScore,
            MissingSkills = missingSkills
        };
    }

    private class ScoreResultInternal
    {
        public int MatchScore { get; set; }
        public List<string> MissingSkills { get; set; } = [];
    }
}
