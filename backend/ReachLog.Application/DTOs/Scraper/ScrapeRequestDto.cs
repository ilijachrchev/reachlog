namespace ReachLog.Application.DTOs.Scraper;

public class ScrapeRequestDto
{
    public Guid Id { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public bool IsHandled { get; set; }
}
