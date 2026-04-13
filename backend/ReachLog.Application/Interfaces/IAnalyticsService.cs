using ReachLog.Application.DTOs.Analytics;

namespace ReachLog.Application.Interfaces;

public interface IAnalyticsService
{
    Task<AnalyticsDto> GetAnalyticsAsync(Guid userId);
}