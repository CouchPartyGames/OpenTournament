using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenTournament.Data.EntityMapping;
using OpenTournament.Data.Models;
using OpenTournament.Options;

namespace OpenTournament.Data;

public class AppDbContext : DbContext
{
    private readonly DatabaseOptions _dbOptions;

    public AppDbContext(IOptions<DatabaseOptions> dbOptions) => _dbOptions = dbOptions.Value;

    public AppDbContext(DbContextOptions<AppDbContext> options, IOptions<DatabaseOptions> dbOptions) : base(options)
    {
        _dbOptions = dbOptions.Value;
    }

    public DbSet<Tournament> Tournaments { get; set; }
    
    public DbSet<Match> Matches { get; set; }
    
    public DbSet<Participant> Participants { get; set; }
    
    public DbSet<Registration> Registrations { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseNpgsql(_dbOptions.ConnectionString)
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