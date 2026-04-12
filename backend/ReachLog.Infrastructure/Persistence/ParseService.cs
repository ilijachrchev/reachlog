using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ReachLog.Application.DTOs;
using ReachLog.Application.Interfaces;

namespace ReachLog.Infrastructure.Services;

public class ParseService : IParseService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public ParseService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Claude:ApiKey"]!;
        _model = configuration["Claude:Model"]!;
    }

    public async Task<ParseResultDto> ParseMessageAsync(string rawMessage)
    {
        var jsonStructure = """
            {
                "companyName": "company name or empty string",
                "contactName": "contact person name or empty string",
                "contactEmail": "email address or empty string",
                "role": "job role or position or empty string",
                "channel": "Email or LinkedIn",
                "sentAt": "date in YYYY-MM-DD format or today's date"
            }
            """;

        var prompt = $"Extract structured information from this job outreach message and return ONLY a JSON object with no extra text.\n\nMessage:\n{rawMessage}\n\nReturn this exact JSON structure:\n{jsonStructure}";

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
        var result = JsonSerializer.Deserialize<ParseResultDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return result ?? new ParseResultDto();
    }
}