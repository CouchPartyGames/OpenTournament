using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace OpenTournament.Core.Infrastructure.Persistence;

public static class PostgresServices
{
    public static IServiceCollection AddPostgres(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        PostgresOptions dbOptions = new();
        configuration.GetSection(PostgresOptions.SectionName).Bind(dbOptions);
        
        services.AddDbContext<AppDbContext>(opts =>
        {
            var dataSource = new NpgsqlDataSourceBuilder(dbOptions.ConnectionString)
                .EnableDynamicJson()
                .Build();
            
            opts.UseNpgsql(dataSource, pgOpts =>
                {
                    //pgOpts.EnableRetryOnFailure();
                    //pgOpts.CommandTimeout(15);
                    //pgOpts.ExecutionStrategy();
                })
                .EnableSensitiveDataLogging();
        }, ServiceLifetime.Singleton);
        
        return services;
    }
}