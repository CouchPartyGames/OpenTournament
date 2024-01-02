using Microsoft.EntityFrameworkCore;
using OpenTournament.Common.Models;

namespace OpenTournament.Common;

public sealed class AppDbContext : DbContext
{
    public AppDbContext() { }
    
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source=tourny.db");

    public DbSet<Tournament> Tournaments { get; set; }
    
    public DbSet<Match> Matches { get; set; }
    
    public DbSet<Participant> Participants { get; set; }
    
    public DbSet<Registration> Registrations { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
            // Tournaments
        modelBuilder.Entity<Tournament>().HasKey(t => t.Id);
        
        modelBuilder.Entity<Tournament>()
            .Property(t => t.Id)
            .HasConversion(v => v.Value,
                v => new TournamentId(v));
        
        modelBuilder.Entity<Tournament>()
            .Property(b => b.Name)
            .IsRequired();

        modelBuilder.Entity<Tournament>()
            .Property(t => t.EliminationMode)
            .HasConversion<int>();
        
        modelBuilder.Entity<Tournament>()
            .Property(t => t.DrawSize)
            .HasConversion<int>();
        
        modelBuilder.Entity<Tournament>()
            .Property(t => t.RegistrationMode)
            .HasConversion<int>();
        
        
            // Matches
        modelBuilder.Entity<Match>().HasKey(m => m.Id);
        
        modelBuilder.Entity<Match>()
            .Property(m => m.Id)
            .HasConversion(v => v.Value,
                v => new MatchId(v));

            // Participants
        modelBuilder.Entity<Participant>().HasKey(m => m.Id);
        
        modelBuilder.Entity<Participant>()
            .Property(p => p.Id)
            .HasConversion(v => v.Value,
                v => new ParticipantId(v));
        
            // Registration
        modelBuilder.Entity<Registration>()
            .HasKey(r => r.TournamentId);
        
        modelBuilder.Entity<Registration>()
            .Property(p => p.TournamentId)
            .HasConversion(v => v.Value,
                v => new TournamentId(v));
        
        modelBuilder.Entity<Registration>()
            .Property(p => p.ParticipantId)
            .HasConversion(v => v.Value,
                v => new ParticipantId(v));
    }
}