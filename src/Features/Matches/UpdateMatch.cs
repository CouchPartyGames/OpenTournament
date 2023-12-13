namespace Features.Matches;

public static class UpdateMatch
{
    private sealed record UpdateMatchCommand(Guid Id);


    public static void MapEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPut("matches/{id}", Endpoint);

    public static async Task<Results<NoContent, NotFound>> Endpoint(string id,
        IMediator mediator,
        CancellationToken token)
    {
        if (!Guid.TryParse(id, out Guid guid))
        {
            return TypedResults.NotFound();
        }
        
        var command = new UpdateMatchCommand(guid);
        var result = await mediator.Send(command, token);

        return TypedResults.NoContent();
    }
}