namespace ReachLog.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    //one to many relationship between User and Outreach
    public ICollection<Outreach> Outreaches { get; set; } = new List<Outreach>(); 
}