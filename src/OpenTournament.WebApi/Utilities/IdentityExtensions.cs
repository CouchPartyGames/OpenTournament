namespace OpenTournament.WebApi.Utilities;

public static class IdentityExtensions
{
    public static string GetUserId(this HttpContext httpContext)
    {
        return httpContext.User
            .Claims
            .FirstOrDefault(c => c.Type == "user_id")?.Value;
    }
}