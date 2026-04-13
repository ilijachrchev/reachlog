using ReachLog.Application.DTOs.Cv;

namespace ReachLog.Application.Interfaces;

public interface IScoreService
{
    Task<ScoreResultDto> ScoreAsync(Guid outreachId, Guid userId);
}