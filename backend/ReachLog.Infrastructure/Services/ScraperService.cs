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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _claudeApiKey;
    private readonly string _claudeModel;
    private readonly ILogger<ScraperService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ScraperService(
        AppDbContext db,
        IApifyService apifyService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ScraperService> logger)
    {
        _db = db;
        _apifyService = apifyService;
        _httpClientFactory = httpClientFactory;
        _claudeApiKey = configuration["Claude:ApiKey"] ?? string.Empty;
        _claudeModel = configuration["Claude:Model"] ?? string.Empty;
        _logger = logger;
    }

    public async Task RunAsync(Guid adminUserId)
    {
        var preferences = await _db.UserJobPreferences.ToListAsync();

        var keywords = ".NET Angular internship";
        if (preferences.Count > 0)
        {
            var tokens = preferences
                .Select(p => p.Keywords)
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .SelectMany(k => k.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (tokens.Count > 0)
                keywords = string.Join(" ", tokens);
        }

        var wave1Cities = preferences.Count > 0
            ? preferences.Select(p => p.City).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList()
            : ["Ljubljana", "Koper", "Maribor"];

        var allResults = new List<ScrapedJobDto>();

        foreach (var city in wave1Cities)
        {
            allResults.AddRange(await _apifyService.ScrapeLinkedInAsync(keywords, city, 1));
            allResults.AddRange(await _apifyService.ScrapeIndeedAsync(keywords, city, 1));
        }

        if (allResults.Count < 20)
        {
            string[] wave2Locations = ["Vienna", "Milan", "Trieste", "Zagreb", "Munich", "Berlin"];
            foreach (var loc in wave2Locations)
            {
                allResults.AddRange(await _apifyService.ScrapeLinkedInAsync(keywords, loc, 2));
                allResults.AddRange(await _apifyService.ScrapeIndeedAsync(keywords, loc, 2));
            }
        }

        if (allResults.Count < 50)
        {
            allResults.AddRange(await _apifyService.ScrapeLinkedInAsync(keywords, "remote Europe", 3));
            allResults.AddRange(await _apifyService.ScrapeIndeedAsync(keywords, "remote Europe", 3));
        }

        allResults.AddRange(await _apifyService.ScrapeLinkedInAsync(keywords, "remote", 4));
        allResults.AddRange(await _apifyService.ScrapeIndeedAsync(keywords, "remote", 4));

        var existingUrls = await _db.ScrapedJobs
            .Select(j => j.ExternalUrl)
            .ToHashSetAsync();

        var newJobs = allResults
            .Where(j => !string.IsNullOrWhiteSpace(j.ExternalUrl) && !existingUrls.Contains(j.ExternalUrl))
            .GroupBy(j => j.ExternalUrl)
            .Select(g => g.First())
            .ToList();

        if (newJobs.Count > 0)
        {
            var entities = newJobs.Select(dto => new ScrapedJob
            {
                Id = Guid.NewGuid(),
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
                ScrapedAt = dto.ScrapedAt
            }).ToList();

            _db.ScrapedJobs.AddRange(entities);
            await _db.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(_claudeApiKey))
            {
                var userCvs = await _db.UserCvs.ToListAsync();
                foreach (var userCv in userCvs)
                {
                    foreach (var entity in entities)
                    {
                        if (string.IsNullOrWhiteSpace(entity.Description)) continue;

                        var interaction = await _db.UserJobInteractions
                            .FirstOrDefaultAsync(i => i.UserId == userCv.UserId && i.ScrapedJobId == entity.Id);

                        if (interaction == null)
                        {
                            interaction = new UserJobInteraction
                            {
                                Id = Guid.NewGuid(),
                                UserId = userCv.UserId,
                                ScrapedJobId = entity.Id,
                                CreatedAt = DateTime.UtcNow
                            };
                            _db.UserJobInteractions.Add(interaction);
                        }

                        try
                        {
                            var (score, missing) = await ScoreJobAsync(userCv.ExtractedText, entity.Title, entity.Company, entity.Description!);
                            interaction.MatchScore = score;
                            interaction.MissingSkills = JsonSerializer.Serialize(missing);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Auto-scoring failed for job {JobId}.", entity.Id);
                        }
                    }
                    await _db.SaveChangesAsync();
                }
            }
        }

        var settings = await _db.ScraperSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new ScraperSettings { Id = Guid.NewGuid() };
            _db.ScraperSettings.Add(settings);
        }
        settings.LastScrapedAt = DateTime.UtcNow;
        settings.TotalJobsInFeed = await _db.ScrapedJobs.CountAsync();

        var pendingRequests = await _db.ScrapeRequests.Where(r => !r.IsHandled).ToListAsync();
        foreach (var req in pendingRequests)
        {
            req.IsHandled = true;
            req.HandledAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<ScrapedJobDto>> GetJobsAsync(Guid userId, string? jobType, bool? remoteOnly)
    {
        var jobs = await _db.ScrapedJobs
            .Where(j =>
                (string.IsNullOrWhiteSpace(jobType) || j.JobType == jobType) &&
                (remoteOnly != true || j.IsRemote))
            .ToListAsync();

        var interactions = await _db.UserJobInteractions
            .Where(i => i.UserId == userId)
            .ToDictionaryAsync(i => i.ScrapedJobId);

        return jobs
            .OrderBy(j => j.Wave)
            .ThenByDescending(j => interactions.TryGetValue(j.Id, out var i) ? i.MatchScore : null)
            .Select(j => MapToDto(j, interactions.TryGetValue(j.Id, out var i) ? i : null))
            .ToList();
    }

    public async Task<ScrapedJobDto> ImportJobAsync(Guid jobId, Guid userId)
    {
        var job = await _db.ScrapedJobs
            .FirstOrDefaultAsync(j => j.Id == jobId)
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
            RawMessage = job.Description ?? string.Empty,
            ExternalUrl = job.ExternalUrl,
            Status = "Interested",
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.Outreaches.Add(outreach);

        var interaction = await _db.UserJobInteractions
            .FirstOrDefaultAsync(i => i.UserId == userId && i.ScrapedJobId == jobId);

        if (interaction == null)
        {
            interaction = new UserJobInteraction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ScrapedJobId = jobId,
                IsImported = true,
                ImportedOutreachId = outreach.Id,
                CreatedAt = DateTime.UtcNow
            };
            _db.UserJobInteractions.Add(interaction);
        }
        else
        {
            interaction.IsImported = true;
            interaction.ImportedOutreachId = outreach.Id;
        }

        await _db.SaveChangesAsync();

        return MapToDto(job, interaction);
    }

    public async Task<ScraperInfoDto> GetInfoAsync()
    {
        var settings = await _db.ScraperSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new ScraperSettings { Id = Guid.NewGuid() };
            _db.ScraperSettings.Add(settings);
            await _db.SaveChangesAsync();
        }

        var pendingRequests = await _db.ScrapeRequests.CountAsync(r => !r.IsHandled);

        return new ScraperInfoDto
        {
            LastScrapedAt = settings.LastScrapedAt,
            TotalJobsInFeed = settings.TotalJobsInFeed,
            PendingRequests = pendingRequests
        };
    }

    public async Task<UserJobPreferenceDto?> GetPreferenceAsync(Guid userId)
    {
        var pref = await _db.UserJobPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
        if (pref == null) return null;

        return new UserJobPreferenceDto
        {
            Country = pref.Country,
            City = pref.City,
            JobType = pref.JobType,
            Keywords = pref.Keywords
        };
    }

    public async Task<UserJobPreferenceDto> SavePreferenceAsync(UserJobPreferenceDto dto, Guid userId)
    {
        var pref = await _db.UserJobPreferences.FirstOrDefaultAsync(p => p.UserId == userId);

        if (pref == null)
        {
            pref = new UserJobPreference
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            _db.UserJobPreferences.Add(pref);
        }

        pref.Country = dto.Country;
        pref.City = dto.City;
        pref.JobType = dto.JobType;
        pref.Keywords = dto.Keywords;
        pref.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return new UserJobPreferenceDto
        {
            Country = pref.Country,
            City = pref.City,
            JobType = pref.JobType,
            Keywords = pref.Keywords
        };
    }

    public async Task RequestScrapeAsync(Guid userId)
    {
        var request = new ScrapeRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RequestedAt = DateTime.UtcNow,
            IsHandled = false
        };

        _db.ScrapeRequests.Add(request);
        await _db.SaveChangesAsync();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        _logger.LogWarning("Scrape requested by user {Email} at {RequestedAt}. Check admin panel to handle.",
            user?.Email ?? userId.ToString(), request.RequestedAt);
    }

    public async Task<List<ScrapeRequestDto>> GetPendingRequestsAsync()
    {
        return await _db.ScrapeRequests
            .Where(r => !r.IsHandled)
            .Include(r => r.User)
            .Select(r => new ScrapeRequestDto
            {
                Id = r.Id,
                UserEmail = r.User.Email,
                RequestedAt = r.RequestedAt,
                IsHandled = r.IsHandled
            })
            .ToListAsync();
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
            content = content[(content.IndexOf('\n') + 1)..];
            content = content[..content.LastIndexOf("```")].Trim();
        }

        var result = JsonSerializer.Deserialize<ScoreResultInternal>(content, _jsonOptions);

        return (result?.MatchScore ?? 0, result?.MissingSkills ?? []);
    }

    private static ScrapedJobDto MapToDto(ScrapedJob job, UserJobInteraction? interaction)
    {
        List<string> missingSkills = [];
        if (!string.IsNullOrWhiteSpace(interaction?.MissingSkills))
        {
            try { missingSkills = JsonSerializer.Deserialize<List<string>>(interaction.MissingSkills) ?? []; }
            catch { }
        }

        return new ScrapedJobDto
        {
            Id = job.Id,
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
            IsImported = interaction?.IsImported ?? false,
            ImportedOutreachId = interaction?.ImportedOutreachId,
            MatchScore = interaction?.MatchScore,
            MissingSkills = missingSkills
        };
    }

    private class ScoreResultInternal
    {
        public int MatchScore { get; set; }
        public List<string> MissingSkills { get; set; } = [];
    }
}
