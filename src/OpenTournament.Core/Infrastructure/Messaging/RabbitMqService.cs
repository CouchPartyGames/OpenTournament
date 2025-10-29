using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OpenTournament.Core.Infrastructure;

public static class RabbitMqService
{

    public static IServiceCollection AddRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        
        services.AddMassTransit(opts => {
            opts.SetKebabCaseEndpointNameFormatter();

            //opts.AddConsumer<TournamentStartedConsumer>();
            //opts.AddConsumer<MatchCompletedConsumer>();

            //opts.UsingInMemory();
            opts.UsingRabbitMq((context, cfg) => {
                cfg.Host("localhost", h => {
                    h.Username("guest");
                    h.Password("guest");
                });

                cfg.ConfigureEndpoints(context);
            });
        });
        
        return services;
    }
}