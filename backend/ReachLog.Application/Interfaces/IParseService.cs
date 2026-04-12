using ReachLog.Application.DTOs;

namespace ReachLog.Application.Interfaces;

public interface IParseService
{
    Task<ParseResultDto> ParseMessageAsync(string rawMessage);
}