using Microsoft.AspNetCore.Http.HttpResults;
using OpenTournament.Core.Features.Events.Create;
using OpenTournament.Core.Infrastructure.Persistence;

namespace OpenTournament.WebApi.Endpoints;

public static class EventEndpoints
{
    public static IEndpointRouteBuilder MapEventEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPost("", async Task<Results<Created, BadRequest>> (CreateEventCommand command, 
                AppDbContext dbContext, 
                CancellationToken token) =>
            {
                var result = await CreateEventHandler.HandleAsync(command, dbContext, token);
                return result switch
                {
                    { IsError: false } => TypedResults.Created(),
                    _ => TypedResults.BadRequest()
                };
            })
            .WithTags("Event")
            .WithSummary("Create Event")
            .WithDescription("Create an Event");

        builder.MapGet("/{id}", GetEvent.Endpoint)
            .WithTags("Event")
            .WithSummary("Get Event")
            .WithDescription("Get an Event");

        builder.MapGet("", GetFilteredEvents.Endpoint)
            .WithTags("Event")
            .WithSummary("Get Event")
            .WithDescription("Get an Filtered Events");
        
        /*
        builder.MapDelete("/{id}", DeleteEvent.Endpoint)
            .WithTags("Event")
            .WithSummary("Delete Event")
            .WithDescription("Delete an Event");
            */
        
        return builder;
    }
    
}