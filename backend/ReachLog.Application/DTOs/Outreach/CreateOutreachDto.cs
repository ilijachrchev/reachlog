namespace ReachLog.Application.DTOs.Outreach;

public class CreateOutreachDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string RawMessage { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}