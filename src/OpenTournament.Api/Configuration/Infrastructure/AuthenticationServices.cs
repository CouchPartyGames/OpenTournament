using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace OpenTournament.Api.Configuration.Infrastructure;

public static class AuthenticationServices
{
   public static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration configuration)
   {
        services.Configure<FirebaseAuthenticationOptions>(configuration.GetSection(FirebaseAuthenticationOptions.SectionName));
        
        services
            .AddAuthentication(opts =>
            {
                opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opts.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
                opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                opts.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            /*.AddCookie("cookie", opts =>
            {
                opts.Cookie.Name = "OpenTournament.Api.Cookie";
                opts.Cookie.HttpOnly = true;
                opts.Cookie.SameSite = SameSiteMode.Strict;
                opts.Cookie.SecurePolicy = CookieSecurePolicy.Always;

            })*/
            /*
            .AddGoogle("google", opts =>
            {
                opts.ClientId = configuration["Google:ClientId"];
                opts.ClientSecret = configuration["Google:ClientSecret"];
                opts.CallbackPath = "/signin-google";
            });*/
            .AddJwtBearer(options => {
                var firebaseAuth = configuration
                    .GetSection(FirebaseAuthenticationOptions.SectionName)
                    .Get<FirebaseAuthenticationOptions>();

                options.Authority = firebaseAuth.Authority;
                options.TokenValidationParameters = new()
                {
                    ValidIssuer = firebaseAuth.Issuer,
                    ValidAudience = firebaseAuth.Audience,
                    
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true
                };
            });
      return services;
   } 
}