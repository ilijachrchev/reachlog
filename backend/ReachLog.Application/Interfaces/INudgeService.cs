using ReachLog.Application.DTOs.Nudge;

namespace ReachLog.Application.Interfaces;

public interface INudgeService
{
    Task<List<NudgeDto>> GetNudgesAsync(Guid userId);
}