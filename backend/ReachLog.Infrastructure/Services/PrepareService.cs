using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ReachLog.Application.DTOs.Prepare;
using ReachLog.Application.Interfaces;

namespace ReachLog.Infrastructure.Services;

public class PrepareService : IPrepareService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICvService _cvService;
    private readonly string _apiKey;
    private readonly string _model;

    public PrepareService(IHttpClientFactory httpClientFactory, ICvService cvService, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _cvService = cvService;
        _apiKey = configuration["Claude:ApiKey"]!;
        _model = configuration["Claude:Model"]!;
    }

    public async Task<PrepareResultDto> PrepareAsync(string jobDescription, Guid userId)
    {
        var cv = await _cvService.GetAsync(userId)
            ?? throw new InvalidOperationException("No CV uploaded. Please upload your CV first.");

        var systemPrompt = "You are a career coach helping a job seeker tailor their application materials.\n" +
            "Given the user's CV text and a job description, return ONLY valid JSON with no explanation, no markdown, no code blocks.\n" +
            "Schema: { \"cvSummary\": \"2-3 sentence professional summary tailored to the role\", \"coverLetter\": \"full cover letter, 3-4 paragraphs, professional tone, no placeholder text, addressed to Hiring Manager\" }";

        var userMessage = $"CV:\n{cv.ExtractedText}\n\nJob Description:\n{jobDescription}";

        var requestBody = new
        {
            model = _model,
            max_tokens = 2048,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = userMessage }
            }
        };

        var httpClient = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", _apiKey);
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

        var result = JsonSerializer.Deserialize<PrepareResultDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return result ?? new PrepareResultDto();
    }
}
