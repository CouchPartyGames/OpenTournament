using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace OpenTournament.Core.Features.Templates;

public static class CreateTemplate
{
    public record CreateTemplateCommand();

    
    public static void MapEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPost("templates", (CreateTemplateCommand cmd, CancellationToken token) =>
            {
                return Endpoint(cmd, token);
            })
            .WithTags("Template")
            .WithSummary("Create Template")
            .WithDescription("Create a Tournament Template")
            .WithOpenApi();

    public static async Task<Results<Created, NotFound>> Endpoint(CreateTemplateCommand cmd, CancellationToken token)
    {
        return TypedResults.Created();
    }
}