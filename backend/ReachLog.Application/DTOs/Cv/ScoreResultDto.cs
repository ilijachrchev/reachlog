namespace ReachLog.Application.DTOs.Cv;

public class ScoreResultDto
{
    public int MatchScore { get; set; }
    public List<string> MissingSkills { get; set; } = [];
}