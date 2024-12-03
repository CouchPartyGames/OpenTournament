using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenTournament.Api;
using OpenTournament.Api.Data;
using OpenTournament.Tests.Integration.Helpers;


namespace OpenTournament.Tests.Integration;

public class TournamentApiFactory : WebApplicationFactory<IApiAssemblyMarker>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
        .WithDatabase("tournament")
        .Build();

    public string ConnectionString => _postgreSqlContainer.GetConnectionString();
    public string ContainerId => _postgreSqlContainer.Id;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        
        builder.ConfigureTestServices(services =>
        {
            services.RemoveDbContext<AppDbContext>();
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(_postgreSqlContainer.GetConnectionString());
            }, ServiceLifetime.Singleton);
            services.EnsureDbCreated<AppDbContext>();
        });
    }
    
    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
    }

    public new async Task DisposeAsync() 
        => await _postgreSqlContainer.DisposeAsync().AsTask();
}