namespace ReachLog.Domain.Entities;

public class UserJobInteraction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid ScrapedJobId { get; set; }
    public ScrapedJob ScrapedJob { get; set; } = null!;
    public int? MatchScore { get; set; }
    public string? MissingSkills { get; set; }
    public bool IsImported { get; set; }
    public Guid? ImportedOutreachId { get; set; }
    public DateTime CreatedAt { get; set; }
}
