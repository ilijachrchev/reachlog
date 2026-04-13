using ReachLog.Application.DTOs.Outreach;

namespace ReachLog.Application.Interfaces;

public interface IOutreachService
{
    Task<List<OutreachDto>> GetAllAsync(Guid userId);
    Task<OutreachDto> GetByIdAsync(Guid id, Guid userId);
    Task<OutreachDto> CreateAsync(CreateOutreachDto request, Guid userId);
    Task<OutreachDto> UpdateAsync(Guid id, UpdateOutreachDto request, Guid userId);
    Task<OutreachDto> UpdateStatusAsync(Guid id, UpdateStatusDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
    Task<DuplicateCheckDto> CheckDuplicateAsync(string companyName, Guid userId);
}