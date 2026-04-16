namespace ReachLog.Application.DTOs.Cv;

public class CvSuggestRequestDto
{
    public List<CvBlockDto> Blocks { get; set; } = new();
    public string? JobDescription { get; set; }
}
