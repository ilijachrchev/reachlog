namespace ReachLog.Application.DTOs.Scraper;

public class ScrapedJobDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public bool IsRemote { get; set; }
    public string JobBoard { get; set; } = string.Empty;
    public string ExternalUrl { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? SalaryMin { get; set; }
    public int? SalaryMax { get; set; }
    public string? Currency { get; set; }
    public string? JobType { get; set; }
    public int Wave { get; set; }
    public DateTime? PostedAt { get; set; }
    public DateTime ScrapedAt { get; set; }
    public bool IsImported { get; set; }
    public Guid? ImportedOutreachId { get; set; }
    public int? MatchScore { get; set; }
    public List<string> MissingSkills { get; set; } = [];
}
