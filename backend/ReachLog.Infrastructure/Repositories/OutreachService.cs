using Microsoft.EntityFrameworkCore;
using ReachLog.Application.DTOs.Outreach;
using ReachLog.Application.Interfaces;
using ReachLog.Domain.Entities;
using ReachLog.Infrastructure.Persistence;

namespace ReachLog.Infrastructure.Repositories;

public class OutreachService : IOutreachService
{
    private readonly AppDbContext _context;

    public OutreachService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<OutreachDto>> GetAllAsync(Guid userId)
    {
        return await _context.Outreaches
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => MapToDto(o))
            .ToListAsync();
    }

    public async Task<OutreachDto> GetByIdAsync(Guid id, Guid userId)
    {
        var outreach = await _context.Outreaches
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

        if (outreach == null)
            throw new KeyNotFoundException("Outreach not found.");

        return MapToDto(outreach);
    }

    public async Task<OutreachDto> CreateAsync(CreateOutreachDto request, Guid userId)
    {
        var outreach = new Outreach
        {
            Id = Guid.NewGuid(),
            CompanyName = request.CompanyName,
            ContactName = request.ContactName,
            ContactEmail = request.ContactEmail,
            Role = request.Role,
            Channel = request.Channel,
            RawMessage = request.RawMessage,
            SentAt = request.SentAt,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Outreaches.Add(outreach);
        await _context.SaveChangesAsync();

        return MapToDto(outreach);
    }

    public async Task<OutreachDto> UpdateStatusAsync(Guid id, UpdateStatusDto request, Guid userId)
    {
        var outreach = await _context.Outreaches
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

        if (outreach == null)
            throw new KeyNotFoundException("Outreach not found.");

        outreach.Status = request.Status;
        await _context.SaveChangesAsync();

        return MapToDto(outreach);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var outreach = await _context.Outreaches
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

        if (outreach == null)
            throw new KeyNotFoundException("Outreach not found.");

        _context.Outreaches.Remove(outreach);
        await _context.SaveChangesAsync();
    }

    private static OutreachDto MapToDto(Outreach outreach) => new()
    {
        Id = outreach.Id,
        CompanyName = outreach.CompanyName,
        ContactName = outreach.ContactName,
        ContactEmail = outreach.ContactEmail,
        Role = outreach.Role,
        Channel = outreach.Channel,
        Status = outreach.Status,
        IsOpened = outreach.IsOpened,
        SentAt = outreach.SentAt,
        CreatedAt = outreach.CreatedAt
    };
}