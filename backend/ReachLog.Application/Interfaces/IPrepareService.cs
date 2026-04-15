using ReachLog.Application.DTOs.Prepare;

namespace ReachLog.Application.Interfaces;

public interface IPrepareService
{
    Task<PrepareResultDto> PrepareAsync(string jobDescription, Guid userId);
}
