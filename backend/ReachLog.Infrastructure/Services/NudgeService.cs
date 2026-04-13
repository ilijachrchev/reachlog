using Microsoft.EntityFrameworkCore;
using ReachLog.Application.DTOs.Nudge;
using ReachLog.Application.Interfaces;
using ReachLog.Infrastructure.Persistence;

namespace ReachLog.Infrastructure.Services;

public class NudgeService : INudgeService
{
    private readonly AppDbContext _db;
    private const int NoReplyThresholdDays = 5;
    private const int RecentRejectionWindowDays = 14;

    public NudgeService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<NudgeDto>> GetNudgesAsync(Guid userId)
    {
        var outreaches = await _db.Outreaches
            .Where(o => o.UserId == userId)
            .ToListAsync();

        var nudges = new List<NudgeDto>();
        var now = DateTime.UtcNow;

        foreach (var outreach in outreaches)
        {
            var daysSinceSent = (int)(now - outreach.SentAt).TotalDays;

            if (outreach.Status == "Sent" && daysSinceSent >= NoReplyThresholdDays)
            {
                nudges.Add(new NudgeDto
                {
                    OutreachId = outreach.Id,
                    CompanyName = outreach.CompanyName,
                    Role = outreach.Role,
                    SentAt = outreach.SentAt,
                    DaysSinceSent = daysSinceSent,
                    NudgeType = "NoReply",
                    Message = $"No reply from {outreach.CompanyName} in {daysSinceSent} days — consider following up."
                });
            }
            else if (outreach.Status == "Rejected" && daysSinceSent <= RecentRejectionWindowDays)
            {
                nudges.Add(new NudgeDto
                {
                    OutreachId = outreach.Id,
                    CompanyName = outreach.CompanyName,
                    Role = outreach.Role,
                    SentAt = outreach.SentAt,
                    DaysSinceSent = daysSinceSent,
                    NudgeType = "RecentRejection",
                    Message = $"Rejected by {outreach.CompanyName} {daysSinceSent} days ago — a polite follow-up may still be worth it."
                });
            }
        }

        return nudges.OrderByDescending(n => n.DaysSinceSent).ToList();
    }
}