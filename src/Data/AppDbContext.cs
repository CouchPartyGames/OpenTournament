using Microsoft.EntityFrameworkCore;
using OpenTournament.Data.EntityMapping;
using OpenTournament.Data.Models;

namespace OpenTournament.Data;

public class AppDbContext : DbContext
{
    private readonly IConfiguration _configuration;

    public AppDbContext(IConfiguration configuration) => _configuration = configuration;

    public AppDbContext(DbContextOptions<AppDbContext> options, IConfiguration configuration) : base(options)
    {
        _configuration = configuration;
    }

    public DbSet<Tournament> Tournaments { get; set; }
    
    public DbSet<Match> Matches { get; set; }
    
    public DbSet<Participant> Participants { get; set; }
    
    public DbSet<Registration> Registrations { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseNpgsql(@"Server=localhost;Port=5432;Database=tournament;User Id=opentournament;Password=somepassword")
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors();
        
        /*
        optionsBuilder
            .UseSqlite("Data Source=tourny.db")
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging();
            */
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TournamentConfiguration());
        modelBuilder.ApplyConfiguration(new MatchConfiguration());
        modelBuilder.ApplyConfiguration(new ParticipantConfiguration());
        modelBuilder.ApplyConfiguration(new RegistrationConfiguration());
    }
}