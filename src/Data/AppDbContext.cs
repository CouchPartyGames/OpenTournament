using Microsoft.EntityFrameworkCore;
using OpenTournament.Data.EntityMapping;
using OpenTournament.Data.Models;

namespace OpenTournament.Data;

public class AppDbContext : DbContext
{
    public AppDbContext() { }
    
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<Tournament> Tournaments { get; set; }
    
    public DbSet<Match> Matches { get; set; }
    
    public DbSet<Participant> Participants { get; set; }
    
    public DbSet<Registration> Registrations { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TournamentConfiguration());
        modelBuilder.ApplyConfiguration(new MatchConfiguration());
        modelBuilder.ApplyConfiguration(new ParticipantConfiguration());
        modelBuilder.ApplyConfiguration(new RegistrationConfiguration());
    }
}