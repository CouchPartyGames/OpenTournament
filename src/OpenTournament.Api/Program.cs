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
builder.Services.AddHealthChecks();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(opts =>
{
    opts.AddPolicy(GlobalConstants.DevCorsPolicyName, policy => policy.AllowAnyOrigin());
    opts.AddPolicy(GlobalConstants.ProdCorsPolicyName, policy =>
    {
        policy
            .WithOrigins("https://api.opentournament.online")
            .WithMethods("GET", "POST", "PUT", "DELETE");
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
    app.MapScalarApiReference();
}

app.UseCors(app.Environment.IsDevelopment() ? GlobalConstants.DevCorsPolicyName : GlobalConstants.ProdCorsPolicyName);
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