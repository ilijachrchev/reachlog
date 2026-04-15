using ReachLog.Application.DTOs.Scraper;

namespace ReachLog.Application.Interfaces;

public interface IApifyService
{
    Task<List<ScrapedJobDto>> ScrapeLinkedInAsync(string keywords, string location, int wave, Guid userId);
    Task<List<ScrapedJobDto>> ScrapeIndeedAsync(string keywords, string location, int wave, Guid userId);
}
