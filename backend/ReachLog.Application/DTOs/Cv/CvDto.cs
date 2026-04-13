namespace ReachLog.Application.DTOs.Cv;

public class CvDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string? ExtractedText { get; set; }
    public string? ContentType { get; set; }
    public int CharacterCount => ExtractedText?.Length ?? 0;
}