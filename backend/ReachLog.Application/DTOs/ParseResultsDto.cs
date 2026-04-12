namespace ReachLog.Application.DTOs;

public class ParseResultDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string SentAt { get; set; } = string.Empty;
}