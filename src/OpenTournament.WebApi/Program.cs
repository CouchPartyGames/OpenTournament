using Asp.Versioning;
using Asp.Versioning.Builder;
using Microsoft.AspNetCore.Http.Features;
using OpenTournament.Core.Infrastructure.Authentication;
using OpenTournament.Core.Infrastructure.Authorization;
using OpenTournament.Core.Infrastructure.Persistence;
using OpenTournament.WebApi.Endpoints;
using OpenTournament.WebApi.Middleware.Exceptions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseKestrel(opts => opts.AddServerHeader = false);
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
//builder.Services.AddAuthenticationServices();
//builder.Services.AddAuthorizationServices();
builder.Services.AddApiVersioning(opts =>
{
    opts.DefaultApiVersion = new ApiVersion(1);
    opts.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer(opts =>
{
    opts.GroupNameFormat = "'v'V";
    opts.SubstituteApiVersionInUrl = true;
});
builder.Services.AddEndpointsApiExplorer();
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
builder.Services.AddHealthChecks();
builder.Services.AddPostgres(builder.Configuration);

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}


//app.UseAuthentication();
//app.UseAuthorization();
//app.UseCors();
//app.UseHttpLogging();
app.UseExceptionHandler(_ => {});
app.UseStatusCodePages();

ApiVersionSet apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .ReportApiVersions()
    .Build();



app.MapGroup("authentication/v{apiVersion:apiVersion}")
    .MapAuthenticationEndpoints()
    .WithApiVersionSet(apiVersionSet);

/*
app.MapGroup("competitions/v{apiVersion:apiVersion}")
    .MapCompetitionEndpoints()
    .WithApiVersionSet(apiVersionSet);
*/
app.MapGroup("events/v{apiVersion:apiVersion}")
    .WithTags("events")
    .MapEventEndpoints()
    .WithApiVersionSet(apiVersionSet);

app.MapGroup("matches/v{apiVersion:apiVersion}")
    .WithTags("matches")
    .MapMatchEndpoints()
    .WithApiVersionSet(apiVersionSet);

app.MapGroup("registrations/v{apiVersion:apiVersion}")
    .WithTags("registrations")
    .MapRegistrationEndpoints()
    .WithApiVersionSet(apiVersionSet);

app.MapGroup("tournaments/v{apiVersion:apiVersion}")
    .WithTags("tournaments")
    .MapTournamentEndpoints()
    .WithApiVersionSet(apiVersionSet);

app.MapHealthChecks("/health");

app.Run();