namespace ReachLog.Application.DTOs.Scraper;

public class ScraperInfoDto
{
    public DateTime? LastScrapedAt { get; set; }
    public int TotalJobsInFeed { get; set; }
    public int PendingRequests { get; set; }
}
