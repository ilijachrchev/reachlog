namespace ReachLog.Domain.Entities;

public class UserJobPreference
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string Keywords { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
