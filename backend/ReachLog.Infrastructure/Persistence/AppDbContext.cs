using Microsoft.EntityFrameworkCore;
using ReachLog.Domain.Entities;

namespace ReachLog.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Outreach> Outreaches { get; set; }
    public DbSet<UserCv> UserCvs { get; set; }
    public DbSet<ScrapedJob> ScrapedJobs { get; set; }
    public DbSet<UserJobPreference> UserJobPreferences { get; set; }
    public DbSet<UserJobInteraction> UserJobInteractions { get; set; }
    public DbSet<ScrapeRequest> ScrapeRequests { get; set; }
    public DbSet<ScraperSettings> ScraperSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
