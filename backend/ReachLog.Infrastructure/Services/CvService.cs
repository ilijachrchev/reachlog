using ReachLog.Application.DTOs.Cv;
using ReachLog.Application.Interfaces;
using ReachLog.Domain.Entities;
using ReachLog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ReachLog.Infrastructure.Services;

public class CvService : ICvService
{
    private readonly AppDbContext _db;

    public CvService(AppDbContext db)
    {
        _db = db;
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
