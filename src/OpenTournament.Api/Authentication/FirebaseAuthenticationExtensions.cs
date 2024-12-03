namespace OpenTournament.Api.Authentication;

public static class FirebaseAuthenticationExtensions
{
    public static string GetUserId(this HttpContext httpContext)
    {
        return httpContext.User
            .Claims
            .FirstOrDefault(c => c.Type == "user_id")?.Value;
    }

    public static string GetEmail(this HttpContext httpContext)
    {
        return httpContext.User
            .Claims
            .FirstOrDefault(c => c.Type == "email")?.Value;
    }
    
    public static string GetEmailVerified(this HttpContext httpContext)
    {
        return httpContext.User
            .Claims
            .FirstOrDefault(c => c.Type == "email_verified")?.Value;
    }
}