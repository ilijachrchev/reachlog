namespace ReachLog.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Outreach> Outreaches { get; set; } = new List<Outreach>();
    public UserJobPreference? JobPreference { get; set; }
    public ICollection<UserJobInteraction> JobInteractions { get; set; } = new List<UserJobInteraction>();
    public ICollection<ScrapeRequest> ScrapeRequests { get; set; } = new List<ScrapeRequest>();
}
