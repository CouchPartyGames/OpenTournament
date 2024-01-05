using System.Security.Claims;
using System.Text.Encodings.Web;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace OpenTournament.Authentication;

public sealed class FirebaseAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly FirebaseApp _firebaseApp;
    
    private const string AuthHeader = "Authorization";
    private const string BearerScheme = "Bearer ";
    
    public FirebaseAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, 
        UrlEncoder encoder, ISystemClock clock, FirebaseApp firebaseApp) : base(options, logger, encoder, clock)
    {
        _firebaseApp = firebaseApp;
    }


    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authResult = VerifyAuthorization();
        authResult.Switch(
            Success => { },
            notFound => { },
            error => { });

        return AuthenticateResult.NoResult();
        return AuthenticateResult.Fail("Unable to determine scheme");

        try
        {
            string bearerToken = Context.Request.Headers["Authorization"];
            var token = GetTokenOnly(bearerToken);
            var firebaseToken = await FirebaseAuth.GetAuth(_firebaseApp).VerifyIdTokenAsync(token);
            return AuthenticateResult.Success(CreateTicket(firebaseToken.Claims));
        }
        catch (Exception e)
        {
            return AuthenticateResult.Fail(e);
        }

    }

    AuthenticationTicket CreateTicket(IReadOnlyDictionary<string, object> claims)
    {
        var claimsList = new List<Claim>()
        {
            new Claim("id", claims["id"].ToString()),
            new Claim("email", claims["email"].ToString()),
            new Claim("name", claims["name"].ToString())
        };
        
        return new AuthenticationTicket(new ClaimsPrincipal(
            new List<ClaimsIdentity>() {
                new ClaimsIdentity(claimsList, nameof(FirebaseAuthenticationHandler))
            }), JwtBearerDefaults.AuthenticationScheme);
    }

    OneOf<OneOf.Types.Success, OneOf.Types.NotFound, OneOf.Types.Error<string>> VerifyAuthorization()
    {
        if (Context.Request.Headers.ContainsKey(AuthHeader))
        {
            return new OneOf.Types.NotFound();
        }

        string bearerToken = Context.Request.Headers[AuthHeader];
        if (!bearerToken.StartsWith(BearerScheme))
        {
            return new OneOf.Types.Error<string>("Unable to determine authorization scheme.");
        }

        return new OneOf.Types.Success();
    }

    string GetTokenOnly(string bearerToken) => bearerToken.Substring(BearerScheme.Length);
    
}