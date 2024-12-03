using OpenTournament;
using OpenTournament.Api;
using OpenTournament.Api.Configuration;
using OpenTournament.Api.Features;
using OpenTournament.Api.Mediator.Behaviours;
using Scalar.AspNetCore;


//var builder = WebApplication.CreateSlimBuilder(args);
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediator();
builder.Services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehaviour<,>));
//builder.Services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(ErrorLoggerHandler<,>));

/* Move to Infrastructure Layer */
builder.Services.AddHealthChecks();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

//builder.Services.AddDomainServices();
//builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddPresentationServices();


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
app.UseStatusCodePages();

app.MapGroup("registrations").MapRegistrationEndpoints();
app.MapGroup("matches").MapMatchesEndpoints();
app.MapGroup("tournaments").MapTournamentsEndpoints();
app.MapGroup("templates").MapTemplatesEndpoints();
app.MapGroup("auth").MapAuthenticationEndpoints();

app.MapHealthChecks(GlobalConstants.HealthPageUri);

app.Run();