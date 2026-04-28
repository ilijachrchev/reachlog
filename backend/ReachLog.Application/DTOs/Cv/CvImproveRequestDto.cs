namespace ReachLog.Application.DTOs.Cv;

public record CvImproveRequestDto(
    string? JobDescription,
    Guid? OutreachId
);
