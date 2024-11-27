namespace OpenTournament.Features.Templates;

public static class UpdateTemplate
{
    public record UpdateTemplateCommand();


    public static async Task<Results<NoContent, NotFound>> Endpoint(string id, CancellationToken token)
    {
        return TypedResults.NoContent();
    }
    
}