namespace ReachLog.Domain.Entities;

public class UserCv
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ExtractedText { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public byte[]? FileBytes { get; set; }
    public string? ContentType { get; set; }

    public User User { get; set; } = null!;
}