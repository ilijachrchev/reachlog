namespace ReachLog.Application.DTOs.Outreach;

public class OutreachDto
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsOpened { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? MatchScore { get; set; }
    public List<string> MissingSkills { get; set; } = [];
}