namespace LLOIS.Data;

using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using LLOIS.Models;

public class AppDbContext : DbContext
{
    public AppDbContext() { }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Ordinance> Ordinances => Set<Ordinance>();
    public DbSet<OrdinanceVersion> OrdinanceVersions => Set<OrdinanceVersion>();
    public DbSet<User> Users => Set<User>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<CommitteeReport> CommitteeReports => Set<CommitteeReport>();
    public DbSet<CommitteeReportAttachment> CommitteeReportAttachments => Set<CommitteeReportAttachment>();
    public DbSet<Resolution> Resolutions => Set<Resolution>();
    public DbSet<ResolutionClause> ResolutionClauses => Set<ResolutionClause>();
    public DbSet<Minutes> Minutes => Set<Minutes>();
    public DbSet<Feedback> Feedback => Set<Feedback>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
            Env.Load();

            var connectionString =
                $"Host={Environment.GetEnvironmentVariable("SUPABASE_HOST")};" +
                $"Port={Environment.GetEnvironmentVariable("SUPABASE_PORT")};" +
                $"Database={Environment.GetEnvironmentVariable("SUPABASE_DB")};" +
                $"Username={Environment.GetEnvironmentVariable("SUPABASE_USER")};" +
                $"Password={Environment.GetEnvironmentVariable("SUPABASE_PASSWORD")};" +
                "SSL Mode=Require;Trust Server Certificate=true";

            options.UseNpgsql(connectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CommitteeReport>()
            .HasMany(r => r.Attachments)
            .WithOne()
            .HasForeignKey(a => a.CommitteeReportId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Resolution>()
            .HasMany(r => r.Clauses)
            .WithOne()
            .HasForeignKey(c => c.ResolutionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Resolution>()
            .Ignore(r => r.WhereasClauses);

        modelBuilder.Entity<Resolution>()
            .Ignore(r => r.ResolvedClauses);
    }
}