using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace OpenTournament.Core.Features.Templates;

public static class UpdateTemplate
{
    public record UpdateTemplateCommand();


    public static async Task<Results<NoContent, NotFound>> Endpoint(string id, CancellationToken token)
    {
        return TypedResults.NoContent();
    }
    
}