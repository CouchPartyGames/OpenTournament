using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace OpenTournament.Api.Configuration.Infrastructure;

public static class FirebaseAuthenticationServices
{
   public static IServiceCollection AddFirebaseAuthentication(this IServiceCollection services, IConfiguration configuration)
   {
        services.Configure<FirebaseAuthenticationOptions>(configuration.GetSection(FirebaseAuthenticationOptions.SectionName));
        
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
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