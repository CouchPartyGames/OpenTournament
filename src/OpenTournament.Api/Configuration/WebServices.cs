using Microsoft.AspNetCore.Http.Features;

namespace OpenTournament.Api.Configuration;

public static class WebServices
{
    public static IServiceCollection AddWebServices(this IServiceCollection services)
    {
        services.AddProblemDetails(opts =>
        {
            opts.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
                context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
                
                var activity = context.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity;
                context.ProblemDetails.Extensions.TryAdd("traceId", activity?.Id);
            };
        });
        
        services.AddHealthChecks();
        return services;
    }
}