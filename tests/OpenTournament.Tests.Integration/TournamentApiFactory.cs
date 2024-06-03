using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTournament.Data;
using OpenTournament.Tests.Integration.Helpers;


namespace OpenTournament.Tests.Integration;

public class TournamentApiFactory : WebApplicationFactory<IApiMarker>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
        .WithDatabase("tournament")
        .Build();

    public string ConnectionString => _postgreSqlContainer.GetConnectionString();
    public string ContainerId => _postgreSqlContainer.Id;

    //public TournamentApiFactory(ITestOutputHelper outputHelper) => _outputHelper = outputHelper;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            //services.RemoveAll(typeof(AppDbContext));
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(_postgreSqlContainer.GetConnectionString());
            }, ServiceLifetime.Singleton);

            services
                .AddAuthentication(MyAuthenticationOptions.DefaultScheme)
                .AddScheme<MyAuthenticationOptions, TestAuthenticationHandler>(MyAuthenticationOptions.DefaultScheme, options =>
                {
                    
                });
        });
    }
    
    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
        
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();

    }

    public new async Task DisposeAsync()
    {
        await _postgreSqlContainer.StopAsync();
    }
}