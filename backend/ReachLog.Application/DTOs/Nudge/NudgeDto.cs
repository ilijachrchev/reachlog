namespace ReachLog.Application.DTOs.Nudge;

public class NudgeDto
{
    public Guid OutreachId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public int DaysSinceSent { get; set; }
    public string NudgeType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}