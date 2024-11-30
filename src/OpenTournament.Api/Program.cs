using Microsoft.AspNetCore.HttpLogging;
using OpenTournament;
using OpenTournament.Common.Exceptions;
using OpenTournament.Mediator.Behaviours;
using Microsoft.AspNetCore.Http.Features;
using OpenTournament.Configuration;
using OpenTournament.Features;
using Scalar.AspNetCore;


//var builder = WebApplication.CreateSlimBuilder(args);
var builder = WebApplication.CreateBuilder(args);

/* Move to Infrastructure Layer */
builder.Services.AddHttpLogging((options) =>
{
    options.CombineLogs = true;
    options.LoggingFields = HttpLoggingFields.All;
});

builder.Services.AddProblemDetails(opts =>
{
    opts.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
        context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
        
        var activity = context.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity;
        context.ProblemDetails.Extensions.TryAdd("traceId", activity?.Id);
    };
});

builder.Services.AddMediator();
builder.Services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehaviour<,>));
//builder.Services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(ErrorLoggerHandler<,>));


/* Move to Infrastructure Layer */
builder.Services.AddHealthChecks();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddInfrastructureServices(builder.Configuration);



var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseHttpLogging();
app.UseExceptionHandler(options => {});

app.MapGroup("registrations").MapRegistrationEndpoints();
app.MapGroup("matches").MapMatchesEndpoints();
app.MapGroup("tournaments").MapTournamentsEndpoints();
app.MapGroup("templates").MapTemplatesEndpoints();
app.MapGroup("auth").MapAuthenticationEndpoints();

app.MapHealthChecks(GlobalConstants.HealthPageUri);

app.Run();