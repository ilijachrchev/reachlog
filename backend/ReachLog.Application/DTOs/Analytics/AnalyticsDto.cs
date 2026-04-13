namespace ReachLog.Application.DTOs.Analytics;

public class AnalyticsDto
{
    public int TotalOutreaches { get; set; }
    public Dictionary<string, int> ByStatus { get; set; } = new();
    public Dictionary<string, int> ByChannel { get; set; } = new();
    public double OpenRate { get; set; }
    public double ReplyRate { get; set; }
    public double? AverageMatchScore { get; set; }
    public List<SkillGapDto> TopMissingSkills { get; set; } = new();
}

public class SkillGapDto
{
    public string Skill { get; set; } = string.Empty;
    public int Count { get; set; }
}