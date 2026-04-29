using System.Text;
using System.Text.Json;
using ReachLog.Application.DTOs.Cv;
using ReachLog.Application.Interfaces;
using ReachLog.Domain.Entities;
using ReachLog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UglyToad.PdfPig;
using QuestPDF.Fluent;
using ReachLog.Infrastructure.Documents;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig.Content;

namespace ReachLog.Infrastructure.Services;

public class CvService : ICvService
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _claudeApiKey;
    private readonly string _claudeModel;

    private const string ImprovePrompt = """
You are a CV improvement assistant. Analyze the CV and return targeted, specific suggestions — NOT a full rewrite.

HARD RULES — NEVER suggest changes to:
- Contact information (name, email, phone, links, location)
- Education entries (school names, degrees, dates, GPAs)
- Job titles, company names, or employment dates
- Certification names, award names, or dates
- Any factual data (numbers that already appear, dates, proper nouns)

ONLY suggest a change when there is a GENUINE, MEANINGFUL improvement:
- Weak action verb → stronger one (e.g., "Helped with" → "Led")
- Vague claim → quantifiable one (only if context strongly implies a number)
- Missing keyword from the job description that the candidate clearly demonstrates
- Unclear or wordy phrasing → crisp phrasing
- Generic statement → specific achievement

Return FEWER, BETTER suggestions. Target 3–8 suggestions total for the whole CV.
If a bullet is already strong, skip it. Do not suggest near-identical rewrites.
Never fabricate skills, technologies, metrics, or experiences not present in the CV.

For each suggestion:
- "originalText" MUST be an EXACT substring of the CV, character-for-character, including punctuation. Copy it directly from the CV text.
- "suggestedText" should modify only what needs changing.
- "reason" must be ONE short sentence explaining the concrete improvement.
- "type" must be one of: "impact", "keyword", "clarity", "quantify".
- "section" should be the CV section name the suggestion falls under (e.g., "Experience", "Projects", "Skills", "Summary").

Output ONLY a JSON array. No preamble. No markdown fences. No trailing text.

Schema:
[
  {
    "section": "string",
    "type": "impact" | "keyword" | "clarity" | "quantify",
    "originalText": "string",
    "suggestedText": "string",
    "reason": "string"
  }
]

CV:
{CV_TEXT}

Job context (may be empty):
{JOB_CONTEXT}
""";

    public CvService(AppDbContext db, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _claudeApiKey = configuration["Claude:ApiKey"] ?? string.Empty;
        _claudeModel = configuration["Claude:Model"] ?? string.Empty;
    }

    public async Task<CvDto> UploadAsync(Stream fileStream, string fileName, string contentType, Guid userId)
    {
        using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms);
        var fileBytes = ms.ToArray();
        ms.Position = 0;

        var extractedText = contentType switch
        {
            "application/pdf" => ExtractFromPdf(ms),
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ExtractFromDocx(ms),
            _ => throw new InvalidOperationException("Unsupported file type. Please upload a PDF or Word document.")
        };

        var existing = await _db.UserCvs.FirstOrDefaultAsync(c => c.UserId == userId);

        if (existing != null)
        {
            existing.ExtractedText = extractedText;
            existing.FileName = fileName;
            existing.UploadedAt = DateTime.UtcNow;
            existing.FileBytes = fileBytes;
            existing.ContentType = contentType;
        }
        else
        {
            var cv = new UserCv
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ExtractedText = extractedText,
                FileName = fileName,
                UploadedAt = DateTime.UtcNow,
                FileBytes = fileBytes,
                ContentType = contentType
            };
            _db.UserCvs.Add(cv);
        }

        await _db.SaveChangesAsync();

        var saved = await _db.UserCvs.FirstAsync(c => c.UserId == userId);
        return new CvDto
        {
            Id = saved.Id,
            FileName = saved.FileName,
            UploadedAt = saved.UploadedAt,
            ExtractedText = saved.ExtractedText,
            ContentType = saved.ContentType
        };
    }

    public async Task<CvDto?> GetAsync(Guid userId)
    {
        var cv = await _db.UserCvs.FirstOrDefaultAsync(c => c.UserId == userId);
        if (cv == null) return null;

        return new CvDto
        {
            Id = cv.Id,
            FileName = cv.FileName,
            UploadedAt = cv.UploadedAt,
            ExtractedText = cv.ExtractedText,
            ContentType = cv.ContentType
        };
    }

    public async Task<(byte[] bytes, string contentType, string fileName)?> GetFileAsync(Guid userId)
    {
        var cv = await _db.UserCvs.FirstOrDefaultAsync(c => c.UserId == userId);
        if (cv == null || cv.FileBytes == null || cv.ContentType == null) return null;
        return (cv.FileBytes, cv.ContentType, cv.FileName);
    }

    public async Task<List<CvSuggestionDto>> ImproveCvAsync(Guid userId, CvImproveRequestDto request)
    {
        var cv = await _db.UserCvs.FirstOrDefaultAsync(c => c.UserId == userId);
        if (cv == null) throw new InvalidOperationException("No CV found for this user.");

        var cvText = cv.ExtractedText ?? string.Empty;

        var jobContext = string.Empty;
        if (request.OutreachId.HasValue)
        {
            var outreach = await _db.Outreaches.FirstOrDefaultAsync(o => o.Id == request.OutreachId && o.UserId == userId);
            if (outreach != null)
            {
                jobContext = $"Role: {outreach.Role} at {outreach.CompanyName}";
                if (!string.IsNullOrWhiteSpace(outreach.RawMessage))
                    jobContext += $"\n{outreach.RawMessage}";
            }
        }
        else if (!string.IsNullOrWhiteSpace(request.JobDescription))
        {
            jobContext = request.JobDescription;
        }

        var prompt = ImprovePrompt
            .Replace("{CV_TEXT}", cvText)
            .Replace("{JOB_CONTEXT}", jobContext);

        var requestBody = new
        {
            model = _claudeModel,
            max_tokens = 4096,
            messages = new[] { new { role = "user", content = prompt } }
        };

        var content = await CallClaudeAsync(requestBody);
        content = StripJsonFences(content);

        var rawSuggestions = JsonSerializer.Deserialize<List<RawSuggestion>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? new List<RawSuggestion>();

        var suggestions = new List<CvSuggestionDto>();
        var dropped = 0;

        var normalizedCv = NormalizeWhitespace(cvText);

        foreach (var raw in rawSuggestions)
        {
            if (string.IsNullOrEmpty(raw.OriginalText))
            {
                dropped++;
                continue;
            }

            var normalizedOriginal = NormalizeWhitespace(raw.OriginalText);
            var matchIndex = normalizedCv.IndexOf(normalizedOriginal, StringComparison.Ordinal);

            if (matchIndex < 0)
            {
                dropped++;
                continue;
            }

            var actualOriginal = FindOriginalInCv(cvText, normalizedCv, matchIndex, normalizedOriginal.Length);
            if (actualOriginal == null)
            {
                dropped++;
                continue;
            }

            suggestions.Add(new CvSuggestionDto(
                Guid.NewGuid().ToString(),
                raw.Section ?? string.Empty,
                raw.Type ?? string.Empty,
                actualOriginal,
                raw.SuggestedText ?? string.Empty,
                raw.Reason ?? string.Empty
            ));
        }

        if (dropped > 0)
            Console.WriteLine($"[CvService] ImproveCvAsync: dropped {dropped} suggestion(s) with unmatched originalText.");

        return suggestions;
    }

    public Task<byte[]> ExportCvAsDocxAsync(CvExportRequestDto request)
    {
        var parsed = CvParser.Parse(NormalizeLigatures(request.FullText));
        return Task.FromResult(CvDocxRenderer.Render(parsed));
    }

    public Task<byte[]> ExportCvAsPdfAsync(CvExportRequestDto request)
    {
        var parsed = CvParser.Parse(NormalizeLigatures(request.FullText));
        var document = new CvQuestDocument(parsed);
        return Task.FromResult(document.GeneratePdf());
    }

    private async Task<string> CallClaudeAsync(object requestBody)
    {
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
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"Claude API: {(int)response.StatusCode}: {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(responseBody);
        return json.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString()!;
    }

    private static string StripJsonFences(string content)
    {
        content = content.Trim();
        if (content.StartsWith("```"))
        {
            content = content.Substring(content.IndexOf('\n') + 1);
            content = content.Substring(0, content.LastIndexOf("```")).Trim();
        }
        return content;
    }

    private static string NormalizeWhitespace(string text)
    {
        return System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
    }

    private static string? FindOriginalInCv(string originalCv, string normalizedCv, int normalizedIndex, int normalizedLength)
    {
        var origIndex = 0;
        var normIndex = 0;
        int? startInOriginal = null;

        while (origIndex < originalCv.Length && normIndex < normalizedCv.Length)
        {
            if (normIndex == normalizedIndex && startInOriginal == null)
                startInOriginal = origIndex;

            if (normIndex == normalizedIndex + normalizedLength)
                return originalCv.Substring(startInOriginal!.Value, origIndex - startInOriginal.Value).TrimEnd();

            var origChar = originalCv[origIndex];
            var normChar = normalizedCv[normIndex];

            if (char.IsWhiteSpace(origChar))
            {
                origIndex++;
                while (origIndex < originalCv.Length && char.IsWhiteSpace(originalCv[origIndex])) origIndex++;
                if (normChar == ' ') normIndex++;
            }
            else if (origChar == normChar)
            {
                origIndex++;
                normIndex++;
            }
            else
            {
                return null;
            }
        }

        if (startInOriginal != null && normIndex >= normalizedIndex + normalizedLength)
            return originalCv.Substring(startInOriginal.Value, origIndex - startInOriginal.Value).TrimEnd();

        return null;
    }

    private static string ExtractFromPdf(Stream stream)
    {
        using var pdf = PdfDocument.Open(stream);
        var sb = new StringBuilder();

        foreach (var page in pdf.GetPages())
        {
            var words = page.GetWords().ToList();
            if (words.Count == 0)
            {
                sb.AppendLine();
                continue;
            }

            var lines = new List<List<UglyToad.PdfPig.Content.Word>>();
            var currentLine = new List<UglyToad.PdfPig.Content.Word> { words[0] };

            for (int i = 1; i < words.Count; i++)
            {
                var prev = words[i - 1];
                var curr = words[i];

                var prevCenterY = (prev.BoundingBox.Top + prev.BoundingBox.Bottom) / 2;
                var currCenterY = (curr.BoundingBox.Top + curr.BoundingBox.Bottom) / 2;
                var avgHeight = (prev.BoundingBox.Height + curr.BoundingBox.Height) / 2;

                if (Math.Abs(prevCenterY - currCenterY) > avgHeight * 0.5)
                {
                    lines.Add(currentLine);
                    currentLine = new List<UglyToad.PdfPig.Content.Word>();
                }

                currentLine.Add(curr);
            }
            if (currentLine.Count > 0) lines.Add(currentLine);

            lines = lines
                .OrderByDescending(line => line.Average(w => (w.BoundingBox.Top + w.BoundingBox.Bottom) / 2))
                .ToList();

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].OrderBy(w => w.BoundingBox.Left).ToList();
                sb.AppendLine(string.Join(" ", line.Select(w => w.Text)));

                if (i < lines.Count - 1)
                {
                    var thisCenterY = lines[i].Average(w => (w.BoundingBox.Top + w.BoundingBox.Bottom) / 2);
                    var nextCenterY = lines[i + 1].Average(w => (w.BoundingBox.Top + w.BoundingBox.Bottom) / 2);
                    var gap = thisCenterY - nextCenterY;
                    var avgHeight = lines[i].Average(w => w.BoundingBox.Height);
                    if (gap > avgHeight * 1.8) sb.AppendLine();
                }
            }

            sb.AppendLine();
        }

        return sb.ToString().Trim();
    }

    private static string NormalizeLigatures(string text)
    {
        return text
            .Replace("\uFB00", "ff")
            .Replace("\uFB01", "fi")
            .Replace("\uFB02", "fl")
            .Replace("\uFB03", "ffi")
            .Replace("\uFB04", "ffl")
            .Replace("\uFB05", "ft")
            .Replace("\uFB06", "st")
            .Replace("\u2013", "-")
            .Replace("\u2014", "-")
            .Replace("\u2018", "'")
            .Replace("\u2019", "'")
            .Replace("\u201C", "\"")
            .Replace("\u201D", "\"")
            .Replace("\u2026", "...")
            .Replace("\u00A0", " ");
    }

    private static string ExtractFromDocx(Stream stream)
    {
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return string.Empty;

        var paragraphs = body.Descendants<Paragraph>()
            .Select(p => string.Join("", p.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text)));
        return string.Join("\n", paragraphs);
    }

    private record RawSuggestion(string? Section, string? Type, string? OriginalText, string? SuggestedText, string? Reason);
}

