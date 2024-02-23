namespace OpenTournament.Authentication;

public static class FirebaseAuthenticationExtensions
{
    public static string GetUserId(this IHttpContextAccessor httpContextAccessor)
    {
        return httpContextAccessor.HttpContext.User
            .Claims
            .FirstOrDefault(c => c.Type == "user_id")?.Value;
    }

    public static string GetEmail(this IHttpContextAccessor httpContextAccessor)
    {
        return httpContextAccessor.HttpContext.User
            .Claims
            .FirstOrDefault(c => c.Type == "email")?.Value;
    }
    
    public static string GetEmailVerified(this IHttpContextAccessor httpContextAccessor)
    {
        return httpContextAccessor.HttpContext.User
            .Claims
            .FirstOrDefault(c => c.Type == "email_verified")?.Value;
    }
}