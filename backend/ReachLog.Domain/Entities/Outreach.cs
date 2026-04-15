namespace ReachLog.Domain.Entities;

public class Outreach
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string RawMessage { get; set; } = string.Empty;
    public string Status { get; set; } = "Sent";
    public bool IsOpened { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public int? MatchScore { get; set; }
    public string? MissingSkills { get; set; }
    public string? Notes { get; set; }
    public string? ExternalUrl { get; set; }
}