using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OpenTournament.Tests.Integration.Helpers;

public class MyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "MyTestAuthenticationScheme";
    
    public string TokenHeaderName { get; set; } = "MyToken";
}


public class TestAuthenticationHandler : AuthenticationHandler<MyAuthenticationOptions>
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>()
        {
            new Claim("FirstName", "Jete")
        };
        
        var identity = new ClaimsIdentity(claims, MyAuthenticationOptions.DefaultScheme );
        var principal = new ClaimsPrincipal(identity);
        
        var ticket = new AuthenticationTicket(principal, MyAuthenticationOptions.DefaultScheme);
        var result = AuthenticateResult.Success(ticket);
        return Task.FromResult(result);
    }

    public TestAuthenticationHandler(IOptionsMonitor<MyAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
    {
    }
}