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
using OpenTournament.Features.Authentication;
using OpenTournament.Services;
using OpenTournament.Jobs;
using OpenTournament.Identity;
using OpenTournament.Identity.Authorization;
using OpenTournament.Mediator.Behaviours;
using OpenTournament.Observability;
using OpenTournament.Options;
using OpenTournament.Jobs;
using Quartz;
using MassTransit;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;


//var builder = WebApplication.CreateSlimBuilder(args);
var builder = WebApplication.CreateBuilder(args);

    // Observability
/* Move to Infrastructure Layer */
builder.Services.AddObservability(builder.Configuration);
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

/* Move to Infrastructure Layer */
builder.Services.Configure<FirebaseAuthenticationOptions>(
    builder.Configuration.GetSection(FirebaseAuthenticationOptions.SectionName));
    //.ValidateDataAnnotations().ValidateOnStart();

/* Move to Infrastructure Layer */
DatabaseOptions dbOptions = new();
builder.Configuration.GetSection(DatabaseOptions.SectionName).Bind(dbOptions);

/* Move to Infrastructure Layer */
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

/* Move to Infrastructure Layer */
builder.Services.AddHealthChecks();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddTournamentLayouts();

/* Move to Infrastructure Layer */
builder.Services.AddMassTransit(opts => {
    opts.SetKebabCaseEndpointNameFormatter();

    opts.AddConsumer<TournamentStartedConsumer>();
    opts.AddConsumer<MatchCompletedConsumer>();

    //opts.UsingInMemory();
    opts.UsingRabbitMq((context, cfg) => {
        cfg.Host("localhost", h => {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ConfigureEndpoints(context);
    });
});
/*builder.Services.AddQuartz(opts =>
{
    var jobKey = JobKey.Create(nameof(StartTournamentJob));
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


/* Move to Infrastructure Layer */
builder.Services.AddSingleton<IAuthorizationHandler, MatchEditHandler>();
/* Move to Infrastructure Layer */
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
/* Move to Infrastructure Layer */
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

Login.MapEndpoint(app);

app.Run();
