using System.Text.Json.Serialization;
using Asp.Versioning;
using Asp.Versioning.Builder;
using OpenTournament.Api;
using OpenTournament.Api.Configuration;
using OpenTournament.Api.Features;
using OpenTournament.Api.Mediator.Behaviours;
using Scalar.AspNetCore;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediator();
builder.Services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehaviour<,>));
//builder.Services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(ErrorLoggerHandler<,>));

/* Move to Infrastructure Layer */
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
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
    opts.AddPolicy(GlobalConstants.DevCorsPolicyName, policy =>
    {
        policy
            .AllowAnyOrigin()
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .AllowAnyHeader();
    });
    opts.AddPolicy(GlobalConstants.ProdCorsPolicyName, policy =>
    {
        policy
            .WithOrigins("https://api.opentournament.online")
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .AllowAnyHeader()
            .AllowCredentials();
    });
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

app.UseCors(app.Environment.IsDevelopment() ? GlobalConstants.DevCorsPolicyName : GlobalConstants.ProdCorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpLogging();
app.UseExceptionHandler(options => {});
app.UseStatusCodePages();

ApiVersionSet apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .ReportApiVersions()
    .Build();

app.MapGroup("registrations/v{apiVersion:apiVersion}")
    .MapRegistrationEndpoints()
    .WithApiVersionSet(apiVersionSet);
app.MapGroup("matches/v{apiVersion:apiVersion}")
    .MapMatchesEndpoints()
    .WithApiVersionSet(apiVersionSet);
app.MapGroup("tournaments/v{apiVersion:apiVersion}")
    .MapTournamentsEndpoints()
    .WithApiVersionSet(apiVersionSet);
app.MapGroup("templates/v{apiVersion:apiVersion}")
    .MapTemplatesEndpoints()
    .WithApiVersionSet(apiVersionSet);
app.MapGroup("authentication/v{apiVersion:apiVersion}")
    .MapAuthenticationEndpoints()
    .WithApiVersionSet(apiVersionSet);

app.MapHealthChecks(GlobalConstants.HealthPageUri);

app.Run();