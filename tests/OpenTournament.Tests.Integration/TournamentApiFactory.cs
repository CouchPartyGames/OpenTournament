using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenTournament.Common;


namespace OpenTournament.Tests.Integration;

public class TournamentApiFactory : WebApplicationFactory<IApiMarker>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
        .WithDatabase("tournament")
        .WithUsername("tourny")
        .WithPassword("tourny").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            var descriptor = services
                .SingleOrDefault(s => s.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
            {
                //var connectionString = $"file:{Guid.NewGuid()}?mode=memory?cache=shared";
                //options.UseSqlite(connectionString);
                options.UseNpgsql(_postgreSqlContainer.GetConnectionString());
            });
            services.AddSingleton<AppDbContext>();
        });
    }
    
    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgreSqlContainer.StopAsync();
    }
}