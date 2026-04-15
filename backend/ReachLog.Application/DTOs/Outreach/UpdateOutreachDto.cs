namespace ReachLog.Application.DTOs.Outreach;

public class UpdateOutreachDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? Role { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string? RawMessage { get; set; }
    public string? Notes { get; set; }
    public DateTime SentAt { get; set; }
    public string? ExternalUrl { get; set; }
}