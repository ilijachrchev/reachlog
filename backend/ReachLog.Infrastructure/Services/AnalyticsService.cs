using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ReachLog.Application.DTOs.Analytics;
using ReachLog.Application.Interfaces;
using ReachLog.Infrastructure.Persistence;

namespace ReachLog.Infrastructure.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _db;

    public AnalyticsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AnalyticsDto> GetAnalyticsAsync(Guid userId)
    {
        var outreaches = await _db.Outreaches
            .Where(o => o.UserId == userId)
            .ToListAsync();

        var total = outreaches.Count;

        if (total == 0)
            return new AnalyticsDto();

        var byStatus = outreaches
            .GroupBy(o => o.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        var byChannel = outreaches
            .GroupBy(o => o.Channel)
            .ToDictionary(g => g.Key, g => g.Count());

        var opened = outreaches.Count(o => o.IsOpened);
        var replied = outreaches.Count(o =>
            o.Status == "Replied" || o.Status == "Interview" || o.Status == "Offer");

        var openRate = Math.Round((double)opened / total, 2);
        var replyRate = Math.Round((double)replied / total, 2);

        var scored = outreaches.Where(o => o.MatchScore.HasValue).ToList();
        double? averageMatchScore = scored.Count > 0
            ? Math.Round(scored.Average(o => o.MatchScore!.Value), 1)
            : null;

        var topMissingSkills = outreaches
            .Where(o => !string.IsNullOrEmpty(o.MissingSkills))
            .SelectMany(o =>
            {
                try
                {
                    return JsonSerializer.Deserialize<List<string>>(o.MissingSkills!) ?? [];
                }
                catch
                {
                    return [];
                }
            })
            .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SkillGapDto { Skill = g.Key, Count = g.Count() })
            .OrderByDescending(s => s.Count)
            .Take(10)
            .ToList();

        return new AnalyticsDto
        {
            TotalOutreaches = total,
            ByStatus = byStatus,
            ByChannel = byChannel,
            OpenRate = openRate,
            ReplyRate = replyRate,
            AverageMatchScore = averageMatchScore,
            TopMissingSkills = topMissingSkills
        };
    }
}