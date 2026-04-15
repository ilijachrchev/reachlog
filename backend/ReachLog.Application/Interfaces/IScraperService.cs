using ReachLog.Application.DTOs.Scraper;

namespace ReachLog.Application.Interfaces;

public interface IScraperService
{
    Task RunAsync(ScrapeRequestDto request, Guid userId);
    Task<List<ScrapedJobDto>> GetJobsAsync(Guid userId, string? jobType, bool? remoteOnly, int? minScore);
    Task<ScrapedJobDto> ImportJobAsync(Guid jobId, Guid userId);
    Task<ScraperStatusDto> GetStatusAsync(Guid userId);
}
