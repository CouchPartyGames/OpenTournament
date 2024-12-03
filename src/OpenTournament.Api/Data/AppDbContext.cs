using OpenTournament.Api.Data.EntityMapping;
using OpenTournament.Api.Data.Models;

namespace OpenTournament.Api.Data;

public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Tournament> Tournaments { get; init; }
    
    public DbSet<Match> Matches { get; init; }
    
    public DbSet<Participant> Participants { get; init; }
    
    public DbSet<Registration> Registrations { get; init; }
    
    public DbSet<Outbox> Outboxes { get; set; }

    /*protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseNpgsql(_dbOptions.ConnectionString, opts =>
            {
                opts.EnableRetryOnFailure();
                opts.CommandTimeout(10);    // in seconds
            })
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors();
    }*/

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TournamentConfiguration());
        modelBuilder.ApplyConfiguration(new MatchConfiguration());
        modelBuilder.ApplyConfiguration(new ParticipantConfiguration());
        modelBuilder.ApplyConfiguration(new RegistrationConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxConfiguration());
        //modelBuilder.ApplyConfiguration(new TemplateConfiguration());
    }
}