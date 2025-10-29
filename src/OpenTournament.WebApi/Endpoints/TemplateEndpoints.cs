using OpenTournament.Core.Features.Templates.Create;
using OpenTournament.Core.Features.Templates.Delete;
using OpenTournament.Core.Features.Templates.Update;

namespace OpenTournament.WebApi.Endpoints;

public static class TemplateEndpoints
{
    
    public static RouteGroupBuilder MapTemplateEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPost("", CreateTemplate.Endpoint)
            .WithTags("Template")
            .WithSummary("Create Template")
            .WithDescription("Create a Tournament Template");


        builder.MapDelete("/{id}", DeleteTemplate.Endpoint)
            .WithTags("Template")
            .WithSummary("Delete Template")
            .WithDescription("Delete an available Template");

        builder.MapPut("/templates/{id}", UpdateTemplate.Endpoint)
            .WithTags("Template")
            .WithSummary("Update Template")
            .WithDescription("Update a Tournament Template");
        
        return builder;
    }
}