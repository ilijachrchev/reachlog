using ReachLog.Application.DTOs.Scraper;

namespace ReachLog.Application.Interfaces;

public interface IScraperService
{
    Task RunAsync(Guid adminUserId);
    Task<List<ScrapedJobDto>> GetJobsAsync(Guid userId, string? jobType, bool? remoteOnly);
    Task<ScrapedJobDto> ImportJobAsync(Guid jobId, Guid userId);
    Task<ScraperInfoDto> GetInfoAsync();
    Task<UserJobPreferenceDto?> GetPreferenceAsync(Guid userId);
    Task<UserJobPreferenceDto> SavePreferenceAsync(UserJobPreferenceDto dto, Guid userId);
    Task RequestScrapeAsync(Guid userId);
    Task<List<ScrapeRequestDto>> GetPendingRequestsAsync();
}
