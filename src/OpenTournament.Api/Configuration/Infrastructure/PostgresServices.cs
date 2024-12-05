using OpenTournament.Api.Data;

namespace OpenTournament.Api.Configuration.Infrastructure;

public static class PostgresServices
{
    public static IServiceCollection AddPostgres(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        PostgresOptions dbOptions = new();
        configuration.GetSection(PostgresOptions.SectionName).Bind(dbOptions);
        
        services.AddDbContext<AppDbContext>(opts =>
        {
            var connectionString = dbOptions.ConnectionString;
            opts.UseNpgsql(connectionString, pgOpts =>
                {
                    //pgOpts.EnableRetryOnFailure(4);
                    pgOpts.CommandTimeout(15);
                    //pgOpts.ExecutionStrategy();
                })
                .EnableSensitiveDataLogging()
                .EnableSensitiveDataLogging();
        }, ServiceLifetime.Singleton);
        
        return services;
    }
}