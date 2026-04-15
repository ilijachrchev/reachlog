namespace ReachLog.Application.DTOs.Scraper;

public class ScraperStatusDto
{
    public bool IsRunning { get; set; }
    public int Wave { get; set; }
    public int TotalFound { get; set; }
    public string? CurrentBoard { get; set; }
    public string? Message { get; set; }
}
