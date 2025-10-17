using OpenTournament.Api.Features.Events;

namespace OpenTournament.WebApi.Endpoints;

public static class EventEndpoints
{
    public static RouteGroupBuilder MapEventEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPost("", CreateEvent.Endpoint)
            .WithTags("Event")
            .WithSummary("Create Event")
            .WithDescription("Create an Event")
            .WithOpenApi();

        builder.MapGet("/{id}", GetEvent.Endpoint)
            .WithTags("Event")
            .WithSummary("Get Event")
            .WithDescription("Get an Event")
            .WithOpenApi();
        
        builder.MapGet("", GetFilteredEvents.Endpoint)
            .WithTags("Event")
            .WithSummary("Get Event")
            .WithDescription("Get an Filtered Events")
            .WithOpenApi();
        /*
        builder.MapDelete("/{id}", DeleteEvent.Endpoint)
            .WithTags("Event")
            .WithSummary("Delete Event")
            .WithDescription("Delete an Event")
            .WithOpenApi();
            */
        
        return builder;
    }
    
}