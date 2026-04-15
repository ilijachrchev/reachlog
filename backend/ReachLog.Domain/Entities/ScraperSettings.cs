namespace ReachLog.Domain.Entities;

public class ScraperSettings
{
    public Guid Id { get; set; }
    public DateTime? LastScrapedAt { get; set; }
    public int TotalJobsInFeed { get; set; }
}
