using System.Text;
using System.Text.Json;
using ReachLog.Application.DTOs.Cv;
using ReachLog.Application.Interfaces;
using ReachLog.Domain.Entities;
using ReachLog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Core;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ReachLog.Infrastructure.Services;

public class CvService : ICvService
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _claudeApiKey;
    private readonly string _claudeModel;

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

    public async Task<List<CvBlockDto>> GetCvBlocksAsync(Guid userId)
    {
        var cv = await _db.UserCvs.FirstOrDefaultAsync(c => c.UserId == userId);
        if (cv == null) return new List<CvBlockDto>();

        var requestBody = new
        {
            model = _claudeModel,
            max_tokens = 4096,
            system = "You are a CV parser. Parse the following CV text into structured blocks. Return ONLY a JSON array, no markdown, no explanation. Each block must have: id (short unique string like \"exp-1\", \"edu-1\", \"skills-1\"), type (one of: header, summary, experience, education, skills, projects, other), title (short label like \"Software Engineer at IAESTE\" or \"Education\" or \"Skills\"), content (the full text of that block as it appears in the CV).",
            messages = new[]
            {
                new { role = "user", content = cv.ExtractedText }
            }
        };

        var content = await CallClaudeAsync(requestBody);
        content = StripJsonFences(content);

        return JsonSerializer.Deserialize<List<CvBlockDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<CvBlockDto>();
    }

    public async Task<List<CvSuggestResponseDto>> GetSuggestionsAsync(Guid userId, CvSuggestRequestDto request)
    {
        var userContent = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(request.JobDescription))
            userContent.AppendLine("Job Description:\n" + request.JobDescription + "\n");
        userContent.AppendLine("CV Blocks:\n" + JsonSerializer.Serialize(request.Blocks));
        userContent.AppendLine("\nReturn ONLY a JSON array of { blockId, suggestedContent } objects — one entry per block, rewriting each block's content to better match the job description (or improve it generally if no job description). No markdown, no explanation.");

        var requestBody = new
        {
            model = _claudeModel,
            max_tokens = 4096,
            messages = new[]
            {
                new { role = "user", content = userContent.ToString() }
            }
        };

        var content = await CallClaudeAsync(requestBody);
        content = StripJsonFences(content);

        return JsonSerializer.Deserialize<List<CvSuggestResponseDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<CvSuggestResponseDto>();
    }

    public Task<byte[]> ExportCvAsDocxAsync(CvExportRequestDto request)
    {
        var ms = new MemoryStream();

        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            for (int i = 0; i < request.Blocks.Count; i++)
            {
                var block = request.Blocks[i];

                var titlePara = body.AppendChild(new Paragraph());
                var titleRun = titlePara.AppendChild(new Run());
                titleRun.AppendChild(new RunProperties(new Bold()));
                titleRun.AppendChild(new Text(block.Title));

                var contentPara = body.AppendChild(new Paragraph());
                var contentRun = contentPara.AppendChild(new Run());
                contentRun.AppendChild(new Text(block.Content) { Space = SpaceProcessingModeValues.Preserve });

                if (i < request.Blocks.Count - 1)
                    body.AppendChild(new Paragraph());
            }

            mainPart.Document.Save();
        }

        return Task.FromResult(ms.ToArray());
    }

    public Task<byte[]> ExportCvAsPdfAsync(CvExportRequestDto request)
    {
        var pdfBuilder = new PdfDocumentBuilder();
        var normalFont = pdfBuilder.AddStandard14Font(Standard14Font.Helvetica);
        var boldFont = pdfBuilder.AddStandard14Font(Standard14Font.HelveticaBold);

        const double marginLeft = 50;
        const double marginBottom = 50;
        const double pageHeight = 842;
        const double normalSize = 11;
        const double titleSize = 13;
        const double lineHeight = 16;
        const double titleLineHeight = 22;
        const double sectionGap = 18;
        const int maxCharsPerLine = 85;

        var page = pdfBuilder.AddPage(595, 842);
        double y = pageHeight - 70;

        foreach (var block in request.Blocks)
        {
            if (y < marginBottom + 80)
            {
                page = pdfBuilder.AddPage(595, 842);
                y = pageHeight - 70;
            }

            page.AddText(block.Title, titleSize, new PdfPoint(marginLeft, y), boldFont);
            y -= titleLineHeight;

            var lines = SplitIntoLines(block.Content, maxCharsPerLine);
            foreach (var line in lines)
            {
                if (y < marginBottom + 30)
                {
                    page = pdfBuilder.AddPage(595, 842);
                    y = pageHeight - 70;
                }
                if (!string.IsNullOrEmpty(line))
                    page.AddText(line, normalSize, new PdfPoint(marginLeft, y), normalFont);
                y -= lineHeight;
            }

            y -= sectionGap;
        }

        return Task.FromResult(pdfBuilder.Build());
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
        // response.EnsureSuccessStatusCode();

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

    private static List<string> SplitIntoLines(string text, int maxChars)
    {
        var result = new List<string>();
        foreach (var paragraph in text.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(paragraph))
            {
                result.Add(string.Empty);
                continue;
            }
            var words = paragraph.Split(' ');
            var current = new StringBuilder();
            foreach (var word in words)
            {
                if (current.Length + word.Length + 1 > maxChars && current.Length > 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                if (current.Length > 0) current.Append(' ');
                current.Append(word);
            }
            if (current.Length > 0) result.Add(current.ToString());
        }
        return result;
    }

    private static string ExtractFromPdf(Stream stream)
    {
        using var pdf = PdfDocument.Open(stream);
        var text = string.Join(" ", pdf.GetPages().Select(p => p.Text));
        return text;
    }

    private static string ExtractFromDocx(Stream stream)
    {
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return string.Empty;

        var text = string.Join(" ", body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>()
            .Select(t => t.Text));
        return text;
    }
}
