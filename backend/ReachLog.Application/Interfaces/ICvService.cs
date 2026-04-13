using ReachLog.Application.DTOs.Cv;

namespace ReachLog.Application.Interfaces;

public interface ICvService
{
    Task<CvDto> UploadAsync(Stream fileStream, string fileName, string contentType, Guid userId);
    Task<CvDto?> GetAsync(Guid userId);
    Task<(byte[] bytes, string contentType, string fileName)?> GetFileAsync(Guid userId);
}