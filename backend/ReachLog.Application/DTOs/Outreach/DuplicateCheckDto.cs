namespace ReachLog.Application.DTOs.Outreach;

public class DuplicateCheckDto
{
    public bool IsDuplicate { get; set; }
    public List<DuplicateOutreachDto> ExistingOutreaches { get; set; } = new();
}

public class DuplicateOutreachDto
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}