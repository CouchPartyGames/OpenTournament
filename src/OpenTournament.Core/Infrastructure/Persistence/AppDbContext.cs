using Microsoft.EntityFrameworkCore;
using OpenTournament.Api.Data.EntityMapping;
using OpenTournament.Core.Domain.Entities;
using OpenTournament.Core.Infrastructure.Persistence.Configurations;

namespace OpenTournament.Core.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Competition> Competitions { get; init; }
    
    public DbSet<Event> Events { get; init; }
    
    public DbSet<Game> Games { get; init; }
    
    public DbSet<Match> Matches { get; init; }
    
    public DbSet<Participant> Participants { get; init; }
    
    public DbSet<Platform> Platforms { get; init; }
    
    public DbSet<Platform> Pools { get; init; }
    
    public DbSet<Registration> Registrations { get; init; }
    
    public DbSet<Stage> Stages { get; init; }
    
    public DbSet<Tournament> Tournaments { get; init; }
    
    public DbSet<TournamentMatches> TournamentMatches { get; init; }
    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CompetitionConfiguration());
        modelBuilder.ApplyConfiguration(new EventConfiguration());
        modelBuilder.ApplyConfiguration(new GameConfiguration());
        modelBuilder.ApplyConfiguration(new MatchConfiguration());
        modelBuilder.ApplyConfiguration(new ParticipantConfiguration());
        modelBuilder.ApplyConfiguration(new PlatformConfiguration());
        modelBuilder.ApplyConfiguration(new PoolConfiguration());
        modelBuilder.ApplyConfiguration(new RegistrationConfiguration());
        modelBuilder.ApplyConfiguration(new StageConfiguration());
        modelBuilder.ApplyConfiguration(new TournamentConfiguration());
        modelBuilder.ApplyConfiguration(new TournamentMatchesConfiguration());
    }
}