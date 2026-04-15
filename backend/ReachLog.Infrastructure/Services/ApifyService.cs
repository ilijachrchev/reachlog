using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReachLog.Application.DTOs.Scraper;
using ReachLog.Application.Interfaces;

namespace ReachLog.Infrastructure.Services;

public class ApifyService : IApifyService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiToken;
    private readonly ILogger<ApifyService> _logger;

    public ApifyService(HttpClient httpClient, IConfiguration configuration, ILogger<ApifyService> logger)
    {
        _httpClient = httpClient;
        _apiToken = configuration["Apify:ApiToken"];
        _logger = logger;
    }

    public async Task<List<ScrapedJobDto>> ScrapeLinkedInAsync(string keywords, string location, int wave)
    {
        if (string.IsNullOrWhiteSpace(_apiToken))
        {
            _logger.LogWarning("Apify API token is not configured. Skipping LinkedIn scrape for location {Location}.", location);
            return [];
        }

        var requestBody = new
        {
            searchQueries = new[] { keywords },
            location = location,
            maxResults = 25
        };

        var url = $"https://api.apify.com/v2/acts/cryptosignals~linkedin-jobs-scraper/run-sync-get-dataset-items?timeout=120";

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {_apiToken}");
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Apify API returned {StatusCode} for location {Location}.", response.StatusCode, location);
                return [];
            }

            var body = await response.Content.ReadAsStringAsync();
            var items = JsonDocument.Parse(body);

            var results = new List<ScrapedJobDto>();
            foreach (var item in items.RootElement.EnumerateArray())
            {
                var dto = MapToDto(item, wave);
                if (dto != null)
                    results.Add(dto);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Apify scrape failed for location {Location}.", location);
            return [];
        }
    }

    public async Task<List<ScrapedJobDto>> ScrapeIndeedAsync(string keywords, string location, int wave)
    {
        if (string.IsNullOrWhiteSpace(_apiToken))
        {
            _logger.LogWarning("Apify API token is not configured. Skipping Indeed scrape for location {Location}.", location);
            return [];
        }

        var requestBody = new
        {
            query = keywords,
            location = location,
            maxItems = 25,
            parseJobDetail = false
        };

        var url = $"https://api.apify.com/v2/acts/misceres~indeed-scraper/run-sync-get-dataset-items?timeout=120";

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {_apiToken}");
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Apify Indeed API returned {StatusCode} for location {Location}.", response.StatusCode, location);
                return [];
            }

            var body = await response.Content.ReadAsStringAsync();
            var items = JsonDocument.Parse(body);

            var results = new List<ScrapedJobDto>();
            foreach (var item in items.RootElement.EnumerateArray())
            {
                var dto = MapIndeedToDto(item, wave);
                if (dto != null)
                    results.Add(dto);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Apify Indeed scrape failed for location {Location}.", location);
            return [];
        }
    }

    public async Task<List<ScrapedJobDto>> ScrapeWellfoundAsync(string keywords, string location, int wave)
    {
        if (string.IsNullOrWhiteSpace(_apiToken))
        {
            _logger.LogWarning("Apify API token is not configured. Skipping Wellfound scrape for location {Location}.", location);
            return [];
        }

        var requestBody = new
        {
            query = keywords,
            location = location,
            maxItems = 25
        };

        var url = $"https://api.apify.com/v2/acts/sovereigntaylor~wellfound-scraper/run-sync-get-dataset-items?timeout=120";

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {_apiToken}");
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Apify Wellfound API returned {StatusCode} for location {Location}.", response.StatusCode, location);
                return [];
            }

            var body = await response.Content.ReadAsStringAsync();
            var items = JsonDocument.Parse(body);

            var results = new List<ScrapedJobDto>();
            foreach (var item in items.RootElement.EnumerateArray())
            {
                var dto = MapWellfoundToDto(item, wave);
                if (dto != null)
                    results.Add(dto);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Apify Wellfound scrape failed for location {Location}.", location);
            return [];
        }
    }

    private static ScrapedJobDto? MapWellfoundToDto(JsonElement item, int wave)
    {
        var title = GetString(item, "title") ?? string.Empty;
        var company = GetString(item, "company") ?? string.Empty;
        var location = GetString(item, "location") ?? string.Empty;
        var jobUrl = GetString(item, "jobUrl") ?? string.Empty;
        var description = GetString(item, "description");

        bool isRemote = false;
        if (item.TryGetProperty("remote", out var remoteProp) && remoteProp.ValueKind == JsonValueKind.True)
            isRemote = true;

        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(company))
            return null;

        return new ScrapedJobDto
        {
            Id = Guid.NewGuid(),
            Title = title,
            Company = company,
            Location = location,
            Country = ExtractCountry(location),
            IsRemote = isRemote,
            JobBoard = "Wellfound",
            ExternalUrl = jobUrl,
            Description = description,
            JobType = DetectJobType(title, description),
            Wave = wave,
            PostedAt = null,
            ScrapedAt = DateTime.UtcNow
        };
    }

    private static ScrapedJobDto? MapIndeedToDto(JsonElement item, int wave)
    {
        var title = GetString(item, "positionName") ?? string.Empty;
        var company = GetString(item, "company") ?? string.Empty;
        var location = GetString(item, "location") ?? string.Empty;
        var jobUrl = GetString(item, "url") ?? string.Empty;
        var description = GetString(item, "description");
        var postedAtStr = GetString(item, "date");

        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(company))
            return null;

        DateTime? postedAt = null;
        if (postedAtStr != null && DateTime.TryParse(postedAtStr, out var parsed))
            postedAt = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);

        return new ScrapedJobDto
        {
            Id = Guid.NewGuid(),
            Title = title,
            Company = company,
            Location = location,
            Country = ExtractCountry(location),
            IsRemote = location.Contains("remote", StringComparison.OrdinalIgnoreCase),
            JobBoard = "Indeed",
            ExternalUrl = jobUrl,
            Description = description,
            JobType = DetectJobType(title, description),
            Wave = wave,
            PostedAt = postedAt,
            ScrapedAt = DateTime.UtcNow
        };
    }

    private static ScrapedJobDto? MapToDto(JsonElement item, int wave)
    {
        var title = GetString(item, "title") ?? GetString(item, "position") ?? string.Empty;
        var company = GetString(item, "company") ?? GetString(item, "companyName") ?? string.Empty;
        var location = GetString(item, "location") ?? string.Empty;
        var jobUrl = GetString(item, "jobUrl") ?? GetString(item, "url") ?? GetString(item, "link") ?? string.Empty;
        var description = GetString(item, "description") ?? GetString(item, "descriptionText");
        var postedAtStr = GetString(item, "postedAt") ?? GetString(item, "publishedAt") ?? GetString(item, "datePosted");

        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(company))
            return null;

        DateTime? postedAt = null;
        if (postedAtStr != null && DateTime.TryParse(postedAtStr, out var parsed))
            postedAt = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);

        return new ScrapedJobDto
        {
            Id = Guid.NewGuid(),
            Title = title,
            Company = company,
            Location = location,
            Country = ExtractCountry(location),
            IsRemote = location.Contains("remote", StringComparison.OrdinalIgnoreCase),
            JobBoard = "LinkedIn",
            ExternalUrl = jobUrl,
            Description = description,
            JobType = DetectJobType(title, description),
            Wave = wave,
            PostedAt = postedAt,
            ScrapedAt = DateTime.UtcNow
        };
    }

    private static string? GetString(JsonElement element, string key)
    {
        if (element.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }

    private static string DetectJobType(string title, string? description)
    {
        var text = (title + " " + (description ?? string.Empty)).ToLowerInvariant();

        if (text.Contains("intern") || text.Contains("internship") ||
            text.Contains("praktikum") || text.Contains(" stage "))
            return "Internship";

        if (text.Contains("junior") || text.Contains("jr.") ||
            text.Contains("entry level") || text.Contains("graduate"))
            return "Junior";

        return "Other";
    }

    private static string ExtractCountry(string location)
    {
        if (string.IsNullOrWhiteSpace(location)) return string.Empty;
        var parts = location.Split(',');
        return parts.Length > 1 ? parts[^1].Trim() : location.Trim();
    }
}
