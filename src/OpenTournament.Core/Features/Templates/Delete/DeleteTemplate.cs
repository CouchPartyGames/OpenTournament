using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace OpenTournament.Core.Features.Templates.Delete;

public static class DeleteTemplate
{
    public record DeleteTemplateCommand();

    public static void MapEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/templates/{id}", (string id, CancellationToken token) =>
            {
                return Endpoint(id, token);
            })
            .WithTags("Template")
            .WithSummary("Delete Template")
            .WithDescription("Delete an available Template");
    }

    public static async Task<Results<NoContent, NotFound>> Endpoint(string id, CancellationToken token)
    {
        return TypedResults.NoContent();
    }
}