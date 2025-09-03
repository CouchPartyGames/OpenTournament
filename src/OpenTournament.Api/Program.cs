using System.Text.Json.Serialization;
using Asp.Versioning;
using Asp.Versioning.Builder;
using OpenTournament.Api;
using OpenTournament.Api.Configuration;
using OpenTournament.Api.Configuration.Infrastructure;
using OpenTournament.Api.Features;
using OpenTournament.Api.Mediator.Behaviours;
using Scalar.AspNetCore;


var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseKestrel(opts => opts.AddServerHeader = false);
builder.Services.AddMediator();
builder.Services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehaviour<,>));
//builder.Services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(ErrorLoggerHandler<,>));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

/* Move to Infrastructure Layer */
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddOpenApi();
builder.Services.AddApiVersioning(opts =>
{
    opts.DefaultApiVersion = new ApiVersion(1, 0);
    opts.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer(opts =>
{
    opts.GroupNameFormat = "'v'V";
    opts.SubstituteApiVersionInUrl = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(opts =>
{
    opts.AddPolicy(CorsExtensions.DevCorsPolicyName, CorsExtensions.GetDevCorsPolicy());
    opts.AddPolicy(CorsExtensions.ProdCorsPolicyName, CorsExtensions.GetProdCorsPolicy());
});

//builder.Services.AddDomainServices();
//builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddPresentationServices();


var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opts =>
    {
        opts.Theme = ScalarTheme.BluePlanet;
        opts.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
        opts.ShowSidebar = true;
    });
}

app.UseCors(app.Environment.IsDevelopment() ? CorsExtensions.DevCorsPolicyName : CorsExtensions.ProdCorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpLogging();
app.UseExceptionHandler(_ => {});
app.UseStatusCodePages();

ApiVersionSet apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .ReportApiVersions()
    .Build();

app.MapGroup("authentication/v{apiVersion:apiVersion}")
    .MapAuthenticationEndpoints()
    .WithApiVersionSet(apiVersionSet);
app.MapGroup("competitions/v{apiVersion:apiVersion}")
    .MapCompetitionsEndpoints()
    .WithApiVersionSet(apiVersionSet);
app.MapGroup("events/v{apiVersion:apiVersion}")
    .MapEventsEndpoints()
    .WithApiVersionSet(apiVersionSet);
app.MapGroup("matches/v{apiVersion:apiVersion}")
    .MapMatchesEndpoints()
    .WithApiVersionSet(apiVersionSet);
app.MapGroup("registrations/v{apiVersion:apiVersion}")
    .MapRegistrationEndpoints()
    .WithApiVersionSet(apiVersionSet);
app.MapGroup("tournaments/v{apiVersion:apiVersion}")
    .MapTournamentsEndpoints()
    .WithApiVersionSet(apiVersionSet);

app.MapHealthChecks(GlobalConstants.HealthPageUri);

app.Run();