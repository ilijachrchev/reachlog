namespace ReachLog.Domain.Entities;

public class ScrapeRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime RequestedAt { get; set; }
    public bool IsHandled { get; set; }
    public DateTime? HandledAt { get; set; }
}
