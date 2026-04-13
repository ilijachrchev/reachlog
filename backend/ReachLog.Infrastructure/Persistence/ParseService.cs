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
        var prompt = "You are a data extraction assistant. Extract information from the job outreach message below.\n\n" +
            "Rules:\n" +
            "- companyName: the company or organization name mentioned\n" +
            "- contactName: the full name of the person who sent the message\n" +
            "- contactEmail: the email address of the sender\n" +
            "- role: the job title or internship position mentioned\n" +
            "- channel: MUST be exactly \"Email\" if it looks like an email, or \"LinkedIn\" if it mentions LinkedIn\n" +
            "- sentAt: the date in YYYY-MM-DD format, or empty string if unknown\n\n" +
            "IMPORTANT: Extract real values from the message. Do not return empty strings if the information is present.\n\n" +
            "Message:\n" + rawMessage + "\n\n" +
            "Return ONLY valid JSON with these exact keys: companyName, contactName, contactEmail, role, channel, sentAt. No markdown, no explanation.";
        
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