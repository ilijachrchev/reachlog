using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ReachLog.Application.DTOs.Cv;
using ReachLog.Application.Interfaces;
using ReachLog.Infrastructure.Persistence;

namespace ReachLog.Infrastructure.Services;

public class ScoreService : IScoreService
{
    private readonly AppDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public ScoreService(AppDbContext db, HttpClient httpClient, IConfiguration configuration)
    {
        _db = db;
        _httpClient = httpClient;
        _apiKey = configuration["Claude:ApiKey"]!;
        _model = configuration["Claude:Model"]!;
    }

    public async Task<ScoreResultDto> ScoreAsync(Guid outreachId, Guid userId)
    {
        var outreach = await _db.Outreaches
            .FirstOrDefaultAsync(o => o.Id == outreachId && o.UserId == userId)
            ?? throw new InvalidOperationException("Outreach not found.");

        var cv = await _db.UserCvs
            .FirstOrDefaultAsync(c => c.UserId == userId)
            ?? throw new InvalidOperationException("No CV found. Please upload your CV first.");

        var prompt = "You are a recruitment assistant. Compare the candidate's CV against the job outreach message and return a match score.\n\n" +
            "CV:\n" + cv.ExtractedText + "\n\n" +
            "Job Outreach Message:\n" + outreach.RawMessage + "\n\n" +
            "Role: " + outreach.Role + "\n" +
            "Company: " + outreach.CompanyName + "\n\n" +
            "Return ONLY valid JSON with these exact keys:\n" +
            "- matchScore: integer 0-100 representing how well the CV matches the job\n" +
            "- missingSkills: array of strings listing skills the job requires that are missing from the CV\n\n" +
            "No markdown, no explanation, just JSON.";

        var requestBody = new
        {
            model = _model,
            max_tokens = 1024,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.SendAsync(request);
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

        var result = JsonSerializer.Deserialize<ScoreResultDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result ??= new ScoreResultDto();

        outreach.MatchScore = result.MatchScore;
        outreach.MissingSkills = JsonSerializer.Serialize(result.MissingSkills);
        await _db.SaveChangesAsync();

        return result;
    }
}