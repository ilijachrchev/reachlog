using ReachLog.Application.DTOs.Cv;

namespace ReachLog.Application.Interfaces;

public interface ICvService
{
    Task<CvDto> UploadAsync(Stream fileStream, string fileName, string contentType, Guid userId);
    Task<CvDto?> GetAsync(Guid userId);
    Task<(byte[] bytes, string contentType, string fileName)?> GetFileAsync(Guid userId);
    Task<List<CvBlockDto>> GetCvBlocksAsync(Guid userId);
    Task<List<CvSuggestResponseDto>> GetSuggestionsAsync(Guid userId, CvSuggestRequestDto request);
    Task<byte[]> ExportCvAsDocxAsync(CvExportRequestDto request);
    Task<byte[]> ExportCvAsPdfAsync(CvExportRequestDto request);
}
