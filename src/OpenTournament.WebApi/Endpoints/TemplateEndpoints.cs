using OpenTournament.Api.Features.Templates;

namespace OpenTournament.WebApi.Endpoints;

public static class TemplateEndpoints
{
    
    public static RouteGroupBuilder MapTemplateEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPost("", CreateTemplate.Endpoint)
            .WithTags("Template")
            .WithSummary("Create Template")
            .WithDescription("Create a Tournament Template")
            .WithOpenApi();
        
        
        builder.MapDelete("/{id}", DeleteTemplate.Endpoint)
            .WithTags("Template")
            .WithSummary("Delete Template")
            .WithDescription("Delete an available Template")
            .WithOpenApi();
        
        builder.MapPut("/templates/{id}", UpdateTemplate.Endpoint)
            .WithTags("Template")
            .WithSummary("Update Template")
            .WithDescription("Update a Tournament Template")
            .WithOpenApi();
        
        return builder;
    }
}