using Microsoft.EntityFrameworkCore;

namespace OpenTournament.Common;

public sealed class AppDbContext : DbContext
{
    public AppDbContext() { }
    
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source=tourny.db");

    private DbSet<Tournament> Tournaments { get; set; }
}