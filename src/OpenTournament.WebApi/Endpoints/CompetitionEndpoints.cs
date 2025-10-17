using OpenTournament.Api.Features.Competitions;

namespace OpenTournament.WebApi.Endpoints;

public static class CompetitionEndpoints
{
    
    public static RouteGroupBuilder MapCompetitionEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPost("", CreateCompetition.Endpoint)
            .WithTags("Competition")
            .WithSummary("Create Competition")
            .WithDescription("Create a Competition within an Event")
            .WithOpenApi();
        
        builder.MapGet("/{id}", GetCompetition.Endpoint)
            .WithTags("Competition")
            .WithSummary("Get Competition")
            .WithDescription("Get a Competition within an Event")
            .WithOpenApi();

        /*
        builder.MapDelete("/{id}", DeleteCompetition.Endpoint)
            .WithTags("Competition")
            .WithSummary("Get Competition")
            .WithDescription("Get a Competition within an Event")
            .WithOpenApi();
            */
        
        return builder;
    }
}