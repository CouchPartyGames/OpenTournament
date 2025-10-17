using Asp.Versioning;
using Asp.Versioning.Builder;
using OpenTournament.WebApi.Endpoints;
using OpenTournament.WebApi.Middleware.Exceptions;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseKestrel(opts => opts.AddServerHeader = false);
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
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

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.UseAuthentication();
app.UseAuthorization();
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
app.MapGroup("competitions/v{apiVersion:apiVersion}")
    .MapCompetitionEndpoints()
    .WithApiVersionSet(apiVersionSet);
app.MapGroup("events/v{apiVersion:apiVersion}")
    .MapEventEndpoints()
    .WithApiVersionSet(apiVersionSet);
app.MapGroup("matches/v{apiVersion:apiVersion}")
    .MapMatchEndpoints()
    .WithApiVersionSet(apiVersionSet);
app.MapGroup("registrations/v{apiVersion:apiVersion}")
    .MapRegistrationEndpoints()
    .WithApiVersionSet(apiVersionSet);
app.MapGroup("tournaments/v{apiVersion:apiVersion}")
    .MapTournamentEndpoints()
    .WithApiVersionSet(apiVersionSet);
    

app.MapHealthChecks("/health");

app.Run();