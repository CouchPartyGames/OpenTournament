namespace OpenTournament.Features.Templates;

public static class UpdateTemplate
{
    public record UpdateTemplateCommand();

    public static void MapEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/templates/{id}", (string id, CancellationToken token) =>
            {
                return Endpoint(id, token);
            })
            .WithTags("Template")
            .WithSummary("Update Template")
            .WithDescription("Update a Tournament Template")
            .WithOpenApi();
    }

    public static async Task<Results<NoContent, NotFound>> Endpoint(string id, CancellationToken token)
    {
        return TypedResults.NoContent();
    }
    
}