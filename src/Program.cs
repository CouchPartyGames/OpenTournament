using Features.Matches;
using Features.Tournaments;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpLogging;
using OpenTournament;
using OpenTournament.Common.Exceptions;
using OpenTournament.Features.Matches;
using OpenTournament.Features.Tournaments;
using OpenTournament.Features.Tournaments.Create;
using OpenTournament.Features.Templates;
using OpenTournament.Services;
using OpenTournament.Jobs;
using OpenTournament.Identity;
using OpenTournament.Identity.Authorization;
using OpenTournament.Mediator.Behaviours;
using OpenTournament.Observability;
using OpenTournament.Options;
using Quartz;


//var builder = WebApplication.CreateSlimBuilder(args);
var builder = WebApplication.CreateBuilder(args);

    // Observability
builder.Services.AddObservability(builder.Configuration);
builder.Services.AddHttpLogging((options) =>
{
    options.CombineLogs = true;
    options.LoggingFields = HttpLoggingFields.All;
});


builder.Services.AddProblemDetails();
builder.Services.Configure<FirebaseAuthenticationOptions>(
    builder.Configuration.GetSection(FirebaseAuthenticationOptions.SectionName));
    //.ValidateDataAnnotations().ValidateOnStart();

DatabaseOptions dbOptions = new();
builder.Configuration.GetSection(DatabaseOptions.SectionName).Bind(dbOptions);

builder.Services.AddDbContext<AppDbContext>(opts =>
{
    var connectionString = dbOptions.ConnectionString;
    opts.UseNpgsql(connectionString, pgOpts =>
        {
            pgOpts.EnableRetryOnFailure(4);
            pgOpts.CommandTimeout(15);
        })
        .EnableSensitiveDataLogging()
        .EnableSensitiveDataLogging();
}, ServiceLifetime.Singleton);
builder.Services.AddMediator();
builder.Services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehaviour<,>));
//builder.Services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(ErrorLoggerHandler<,>));
builder.Services.AddHealthChecks();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddTournamentLayouts();
/*builder.Services.AddQuartz(opts =>
{
    var jobKey = JobKey.Create(nameof(OutboxBackgroundJob));
    opts.AddJob<OutboxBackgroundJob>(jobKey)
        .AddTrigger(trigger =>
        {
            trigger.ForJob(jobKey).WithSimpleSchedule(schedule =>
            {
                schedule.WithIntervalInSeconds(1).RepeatForever();
            });
        });
    
    jobKey = JobKey.Create(nameof(StartTournamentJob));
    opts.AddJob<StartTournamentJob>(jobKey)
        .AddTrigger(trigger =>
        {
            trigger.ForJob(jobKey).WithSimpleSchedule(schedule =>
            {
                schedule.WithIntervalInSeconds(30).RepeatForever();
            });
        });
});
builder.Services.AddQuartzHostedService(opts =>
{
    opts.WaitForJobsToComplete = true;
}); */
builder.Services.AddSingleton<IAuthorizationHandler, MatchEditHandler>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    var firebaseAuth = builder.Configuration
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
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(IdentityData.ParticipantPolicyName, policyBuilder =>
    {
        policyBuilder.RequireAuthenticatedUser();
    });
    
    options.AddPolicy(IdentityData.ServerPolicyName, policyBuilder =>
    {
        policyBuilder.RequireClaim(IdentityData.ServerClaimName, "server");
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseHttpLogging();
app.UseExceptionHandler(options => {});
app.MapHealthChecks(GlobalConsts.HealthPageUri);

CreateTournamentEndpoint.MapEndpoint(app);
GetTournament.MapEndpoint(app);
UpdateTournament.MapEndpoint(app);
DeleteTournament.MapEndpoint(app);
StartTournament.MapEndpoint(app);

LeaveRegistration.MapEndpoint(app);
JoinRegistration.MapEndpoint(app);
ListRegistration.MapEndpoint(app);

GetMatch.MapEndpoint(app);
UpdateMatch.MapEndpoint(app);
CompleteMatch.MapEndpoint(app);


CreateTemplate.MapEndpoint(app);
DeleteTemplate.MapEndpoint(app);
UpdateTemplate.MapEndpoint(app);
//ListTemplate.MapEndpoint(app);

//Login.MapEndpoint(app);

app.Run();